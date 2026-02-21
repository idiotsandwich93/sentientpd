using Rage;
using Rage.Native;
using System;

/// <summary>
/// Drives the player through Bolingbroke's two security gates in a prison bus.
/// Ported from marhex/prison-mod (ScriptHookVDotNet) to RAGE Plugin Hook / LSR.
///
/// Invoke Start() on a GameFiber — it blocks until transport is complete or times out.
/// </summary>
public class PrisonGateTransportActivity
{
    // ------------------------------------------------------------------
    // Coordinates (ported 1-to-1 from prison-mod)
    // ------------------------------------------------------------------
    private static readonly Vector3 BusSpawnPos     = new Vector3(1971.267f, 2635.439f, 46.17389f);
    private static readonly float   BusSpawnHeading  = 120.792f;

    // Gate 1 — outer perimeter (bus stops here while gate opens)
    private static readonly Vector3 Gate1DriveTarget = new Vector3(1855.855f, 2606.756f, 45.9304f);
    private static readonly Vector3 Gate1PropPos     = new Vector3(1845.0f,   2605.0f,   45.0f);

    // Gate 2 — inner perimeter
    private static readonly Vector3 Gate2DriveTarget = new Vector3(1831.152f, 2606.738f, 45.83254f);
    private static readonly Vector3 Gate2PropPos     = new Vector3(1819.27f,  2608.53f,  44.61f);

    // Where the bus stops inside the yard and the player gets out
    private static readonly Vector3 DropOffPos       = new Vector3(1754.018f, 2604.271f, 45.82404f);

    // Leg timeouts (ms) — long enough for normal gameplay, short enough not to hang forever
    private const int LEG_TIMEOUT_MS = 90_000;  // 90 s per leg
    private const int GATE_WAIT_MS   = 4_000;   // pause at each gate

    private const int DRIVING_STYLE_NORMAL = 786603; // GTA's "Normal" driving flags
    private const int PASSENGER_SEAT = -2;           // front passenger

    // ------------------------------------------------------------------
    private Vehicle _bus;
    private Ped     _driver;

    public bool IsActive { get; private set; }

    /// <summary>
    /// Blocks the calling GameFiber until transport is complete.
    /// Returns true on success, false if it had to abort (timeout / entity missing).
    /// </summary>
    public bool Start()
    {
        IsActive = true;
        bool success = false;
        try
        {
            success = RunTransport();
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"[PrisonGateTransport] Error: {ex.Message}", 0);
        }
        finally
        {
            Cleanup();
            IsActive = false;
        }
        return success;
    }

    private bool RunTransport()
    {
        // Spawn the prison transport bus
        _bus = new Vehicle("PBUS", BusSpawnPos, BusSpawnHeading) { IsPersistent = true };
        GameFiber.Sleep(200);
        if (!_bus.Exists()) return false;

        // Spawn a prison guard as driver
        _driver = new Ped("prisguard01smm", BusSpawnPos.Around(2f), BusSpawnHeading) { IsPersistent = true };
        GameFiber.Sleep(200);
        if (!_driver.Exists()) return false;

        NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(_driver, -1533126372); // police group
        NativeFunction.Natives.SET_PED_AS_COP(_driver, true);
        NativeFunction.Natives.SET_ENTITY_INVINCIBLE(_driver, true);

        // Warp driver and player into vehicle (no animation — player is already faded in)
        NativeFunction.Natives.SET_PED_INTO_VEHICLE(_driver, _bus, -1);           // driver seat
        NativeFunction.Natives.SET_PED_INTO_VEHICLE(Game.LocalPlayer.Character, _bus, PASSENGER_SEAT);
        GameFiber.Sleep(500);

        // ---- Leg 1: drive to Gate 1 ----
        SetGate(Gate1PropPos, open: true);
        DriveTo(Gate1DriveTarget);
        bool leg1Ok = WaitUntilNear(Gate1DriveTarget, 8f, LEG_TIMEOUT_MS);
        GameFiber.Sleep(GATE_WAIT_MS);

        // ---- Leg 2: drive to Gate 2 ----
        SetGate(Gate1PropPos, open: false); // close outer gate behind us
        SetGate(Gate2PropPos, open: true);
        DriveTo(Gate2DriveTarget);
        bool leg2Ok = WaitUntilNear(Gate2DriveTarget, 8f, LEG_TIMEOUT_MS);
        GameFiber.Sleep(GATE_WAIT_MS);

        // ---- Leg 3: drive to drop-off inside yard ----
        SetGate(Gate2PropPos, open: false);
        DriveTo(DropOffPos);
        bool leg3Ok = WaitUntilNear(DropOffPos, 8f, LEG_TIMEOUT_MS);
        GameFiber.Sleep(1500);

        // Player exits the vehicle
        NativeFunction.Natives.TASK_LEAVE_VEHICLE(Game.LocalPlayer.Character, _bus, 0);
        GameFiber.WaitWhile(() => Game.LocalPlayer.Character.IsInVehicle(_bus), 8000);

        return leg1Ok && leg2Ok && leg3Ok;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void DriveTo(Vector3 target)
    {
        if (!GuardExists()) return;
        NativeFunction.Natives.TASK_VEHICLE_DRIVE_TO_COORD(
            _driver, _bus,
            target.X, target.Y, target.Z,
            10f,                      // speed
            0,                        // unknown
            (uint)_bus.Model.Hash,    // vehicle model hash
            DRIVING_STYLE_NORMAL,
            3f,                       // stop radius
            1);                       // flag
    }

    private bool WaitUntilNear(Vector3 target, float radius, int timeoutMs)
    {
        uint start = Game.GameTime;
        while (Game.LocalPlayer.Character.DistanceTo(target) > radius)
        {
            if (!GuardExists() || Game.GameTime - start > (uint)timeoutMs)
            {
                EntryPoint.WriteToConsole("[PrisonGateTransport] Leg timed out or guard missing.", 0);
                return false;
            }
            GameFiber.Yield();
        }
        return true;
    }

    /// <summary>
    /// Opens (open=true) or closes (open=false) the nearest instance of
    /// prop_gate_prison_01 at the given world position.
    /// Native: 0x9B12F9A24FABEDB0 — same as SHVDN Hash._0x9B12F9A24FABEDB0
    /// </summary>
    private static void SetGate(Vector3 pos, bool open)
    {
        uint propHash = NativeFunction.Natives.GET_HASH_KEY<uint>("prop_gate_prison_01");
        int  locked   = open ? 0 : 1;  // 0 = open, 1 = closed (same as prison-mod)
        NativeFunction.Natives.x9B12F9A24FABEDB0<bool>(
            propHash, pos.X, pos.Y, pos.Z,
            locked, 0f, 50f, 0f);
    }

    private bool GuardExists() => _driver != null && _driver.Exists() && !_driver.IsDead
                                && _bus    != null && _bus.Exists();

    private void Cleanup()
    {
        try
        {
            if (_driver != null && _driver.Exists()) _driver.Dismiss();
            if (_bus    != null && _bus.Exists())
            {
                _bus.Dismiss();
            }
        }
        catch { /* non-fatal */ }
    }
}
