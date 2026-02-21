using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;

/// <summary>
/// Starts a prison riot at Bolingbroke by arming prisoners and guards against
/// each other and sounding the alarm. Ported from marhex/prison-mod (SHVDN).
///
/// Call Start() to set everything up (non-blocking).
/// Call Cleanup() when the player's sentence expires to restore normal state.
/// </summary>
public class PrisonRiotActivity
{
    // ------------------------------------------------------------------
    // Relationship group hashes — same values as prison-mod
    // ------------------------------------------------------------------
    private static readonly uint PRISONER_GROUP = unchecked((uint)2124571506);
    private static readonly uint GUARD_GROUP    = unchecked((uint)-183807561);

    private const int RELATIONSHIP_HATE       = 5;  // GTA native value for Hate
    private const int RELATIONSHIP_PEDESTRIAN = 3;  // Neutral/Pedestrian baseline

    // Weapon hashes
    private const uint WEAPON_KNIFE    = 0x99B507EA;
    private const uint WEAPON_STUNGUN  = 0x3656C8C1;

    // Spawn points inside the Bolingbroke yard (where player is dropped after transport)
    private static readonly (Vector3 pos, float heading)[] PrisonerSpawns =
    {
        (new Vector3(1640f, 2519f, 45.56f), 200f),
        (new Vector3(1625f, 2508f, 45.56f), 140f),
        (new Vector3(1658f, 2531f, 45.56f), 260f),
        (new Vector3(1748f, 2590f, 45.82f), 180f),
    };

    private static readonly (Vector3 pos, float heading)[] GuardSpawns =
    {
        (new Vector3(1682f, 2551f, 45.56f), 320f),
        (new Vector3(1698f, 2540f, 45.56f),  20f),
        (new Vector3(1714f, 2568f, 45.56f), 270f),
    };

    // ------------------------------------------------------------------
    private readonly List<Ped> _spawnedPrisoners = new List<Ped>();
    private readonly List<Ped> _spawnedGuards    = new List<Ped>();

    public bool IsActive { get; private set; }

    // ------------------------------------------------------------------
    /// <summary>
    /// Sets up the riot immediately and returns — NPC AI handles the fight.
    /// </summary>
    public void Start()
    {
        try
        {
            // Set hate both ways between prisoners and guards
            SetRiotRelationships();

            // Spawn + arm prisoners
            foreach (var (pos, heading) in PrisonerSpawns)
            {
                Ped p = SpawnPrisoner(pos, heading);
                if (p != null) _spawnedPrisoners.Add(p);
            }

            // Spawn + arm guards
            foreach (var (pos, heading) in GuardSpawns)
            {
                Ped g = SpawnGuard(pos, heading);
                if (g != null) _spawnedGuards.Add(g);
            }

            // Arm any pre-existing Bolingbroke peds that are already in the right groups
            ArmNearbyGroupPeds();

            // Give player a knife so they can participate
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(
                Game.LocalPlayer.Character, WEAPON_KNIFE, 1, false, true);

            // Sound the prison alarm
            NativeFunction.Natives.START_ALARM("PRISON_ALARMS", true);

            // Suppress external wanted level so only prison peds are involved
            NativeFunction.Natives.SET_MAX_WANTED_LEVEL(0);

            Game.DisplayNotification(
                "CHAR_CALL911", "CHAR_CALL911",
                "~r~Bolingbroke Penitentiary", "~r~RIOT IN PROGRESS",
                "A riot has broken out. Survive until your sentence expires.");

            IsActive = true;
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"[PrisonRiotActivity] Start error: {ex.Message}", 0);
        }
    }

    // ------------------------------------------------------------------
    /// <summary>
    /// Restores neutral relationships, stops the alarm, and dismisses spawned peds.
    /// Call this when the sentence expires regardless of whether a riot was triggered.
    /// </summary>
    public void Cleanup()
    {
        try
        {
            if (IsActive)
            {
                NativeFunction.Natives.STOP_ALARM("PRISON_ALARMS", true);
                NativeFunction.Natives.SET_MAX_WANTED_LEVEL(5);
                ResetRelationships();
            }

            foreach (var p in _spawnedPrisoners)
                if (p != null && p.Exists()) p.Dismiss();

            foreach (var g in _spawnedGuards)
                if (g != null && g.Exists()) g.Dismiss();

            _spawnedPrisoners.Clear();
            _spawnedGuards.Clear();
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"[PrisonRiotActivity] Cleanup error: {ex.Message}", 0);
        }
        finally
        {
            IsActive = false;
        }
    }

    // ------------------------------------------------------------------
    // Spawning helpers
    // ------------------------------------------------------------------

    private static Ped SpawnPrisoner(Vector3 pos, float heading)
    {
        try
        {
            var ped = new Ped(new Model("s_m_y_prisoner_01"), pos, heading)
                { IsPersistent = true, KeepTasks = true };

            NativeFunction.Natives.SET_PED_RELATIONSHIP_GROUP_HASH(ped, PRISONER_GROUP);
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(ped, WEAPON_KNIFE, 1, false, true);
            NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, true);
            NativeFunction.Natives.SET_ENTITY_INVINCIBLE(ped, false);

            return ped;
        }
        catch
        {
            return null;
        }
    }

    private static Ped SpawnGuard(Vector3 pos, float heading)
    {
        try
        {
            var ped = new Ped(new Model("s_m_m_prisguard_01"), pos, heading)
                { IsPersistent = true, KeepTasks = true };

            NativeFunction.Natives.SET_PED_RELATIONSHIP_GROUP_HASH(ped, GUARD_GROUP);
            NativeFunction.Natives.SET_PED_AS_COP(ped, true);
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(ped, WEAPON_STUNGUN, 200, false, true);
            NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, true);
            NativeFunction.Natives.SET_ENTITY_INVINCIBLE(ped, false);

            return ped;
        }
        catch
        {
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Arm any Bolingbroke peds that spawned naturally in the area
    // ------------------------------------------------------------------

    private static void ArmNearbyGroupPeds()
    {
        try
        {
            Ped[] nearby = World.GetAllPeds();
            foreach (Ped p in nearby)
            {
                if (!p.Exists() || p.IsDead) continue;
                uint group = NativeFunction.Natives.GET_PED_RELATIONSHIP_GROUP_HASH<uint>(p);

                if (group == PRISONER_GROUP)
                {
                    NativeFunction.Natives.GIVE_WEAPON_TO_PED(p, WEAPON_KNIFE, 1, false, true);
                    NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(p, true);
                }
                else if (group == GUARD_GROUP)
                {
                    NativeFunction.Natives.GIVE_WEAPON_TO_PED(p, WEAPON_STUNGUN, 200, false, true);
                    NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(p, true);
                }
            }
        }
        catch { /* non-fatal */ }
    }

    // ------------------------------------------------------------------
    // Relationship management
    // ------------------------------------------------------------------

    private static void SetRiotRelationships()
    {
        // Clear any existing relationship first, then set to Hate in both directions
        NativeFunction.Natives.CLEAR_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, PRISONER_GROUP, GUARD_GROUP);
        NativeFunction.Natives.CLEAR_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, GUARD_GROUP, PRISONER_GROUP);
        NativeFunction.Natives.SET_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, PRISONER_GROUP, GUARD_GROUP);
        NativeFunction.Natives.SET_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, GUARD_GROUP, PRISONER_GROUP);
    }

    private static void ResetRelationships()
    {
        NativeFunction.Natives.CLEAR_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, PRISONER_GROUP, GUARD_GROUP);
        NativeFunction.Natives.CLEAR_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_HATE, GUARD_GROUP, PRISONER_GROUP);
        NativeFunction.Natives.SET_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_PEDESTRIAN, PRISONER_GROUP, GUARD_GROUP);
        NativeFunction.Natives.SET_RELATIONSHIP_BETWEEN_GROUPS(RELATIONSHIP_PEDESTRIAN, GUARD_GROUP, PRISONER_GROUP);
    }
}
