# sentientpd — Bolingbroke Real-Time Prison Sentences

An add-on for [Los Santos RED](https://github.com/thatoneguy650/Los-Santos-RED) that enforces **real-world wait times** when a player is sent to Bolingbroke Penitentiary, with a full gate transport sequence and prison outfit — ported from [marhex/prison-mod](https://github.com/marhex/prison-mod) (originally ScriptHookVDotNet) into LSR's RAGE Plugin Hook framework.

> **Note:** Because SHVDN and RAGE Plugin Hook cannot run simultaneously, the prison-mod features are ported directly into LSR rather than loaded alongside it.

---

## Features

### Real-world prison sentences
| Arrest # | Sentence length |
|----------|----------------|
| 1st      | 15 minutes      |
| 2nd      | 20 minutes      |
| 3rd      | 25 minutes      |
| …        | +5 min each     |
| (cap)    | 60 minutes max  |

* Timer uses **real-world clock time** — closing the game does **not** reset it.
* Sentence data persists to `Plugins\LosSantosRED\PrisonSentences.xml`.
* On release: default clothing is restored and a notification shows the arrest count.

### Gate transport (ported from marhex/prison-mod)
When the player surrenders, instead of a direct teleport inside:
1. Screen fades in at the **prison transport bus spawn** east of the outer gate.
2. A prison guard warps into the bus and drives the player through **Gate 1** (outer) then **Gate 2** (inner), opening each gate as the bus approaches and closing it behind.
3. Player exits the bus inside the prison yard.
4. If the transport times out (>90 s per leg), a direct teleport fallback is used.

### Prison outfit (ported from marhex/prison-mod)
After the transport drops the player inside, the character is automatically changed into a prison jumpsuit:
* **Michael** — orange top/bottoms (component 3: drawable 12, component 4: drawable 11)
* **Franklin** — orange outfit (component 3: drawable 1, component 4: drawable 1)
* **Trevor** — orange outfit (component 3: drawable 5, component 4: drawable 5)
* FreeMode/custom models — unchanged (LSR handles clothing separately)

Outfit is removed automatically when the sentence is served.

---

## Files

| File | Purpose |
|------|---------|
| `PrisonSentenceData.cs` | Serialisable data model (arrest count, sentence start time, minutes, serving flag) |
| `PrisonSentenceTracker.cs` | Core logic — `OnArrested()`, `TryRelease()`, `GetCountdownText()`, save/load |
| `PrisonGateTransportActivity.cs` | Drives the player through the two Bolingbroke security gates in a prison bus |
| `BolingbrookeOutfit.cs` | Applies/removes the prison jumpsuit based on player model |

---

## Integration into LSR

Add all four `.cs` files to the **Los Santos RED** Visual Studio project (same solution), then apply the following three changes to `lsr/Player/Respawning/Respawning.cs`:

### 1 — Add the tracker field (after the `HasIllegalItems` field)

```csharp
private PrisonSentenceTracker _prisonSentenceTracker;
```

### 2 — Initialise in `Setup()`

```csharp
public void Setup()
{
    _prisonSentenceTracker = new PrisonSentenceTracker();
    // ... rest of Setup ...
```

### 3 — Replace the tail of `SurrenderToPolice()` (from the `EntryPoint.WriteToConsole` PRE line to the end of the method)

```csharp
// Start real-time sentence before fading back in
_prisonSentenceTracker.OnArrested();

EntryPoint.WriteToConsole($"PRE 1: {Time.CurrentDateTime} {BailDuration}");
Time.SetDateTime(BailPostingTime);
GameFiber.Sleep(1000);
Player.HumanState.SetRandom();
FadeIn();

// Lock the player in place and display a countdown until the sentence is served
Rage.Game.LocalPlayer.Character.IsPositionFrozen = true;
Rage.Game.LocalPlayer.Character.BlockPermanentEvents = true;
uint lastDisplayTick = 0;
while (!_prisonSentenceTracker.TryRelease())
{
    if (Rage.Game.GameTime - lastDisplayTick >= 1000 || lastDisplayTick == 0)
    {
        Rage.Game.DisplayHelp(_prisonSentenceTracker.GetCountdownText(), false);
        lastDisplayTick = Rage.Game.GameTime;
    }
    GameFiber.Yield();
}
Rage.Game.LocalPlayer.Character.IsPositionFrozen = false;
Rage.Game.LocalPlayer.Character.BlockPermanentEvents = false;
Rage.Game.DisplayNotification(
    "CHAR_CALL911", "CHAR_CALL911",
    "Bolingbroke Penitentiary", "~g~Released",
    $"Sentence served. You are free to go.~n~Arrest #{_prisonSentenceTracker.ArrestCount} on your record.");

if (Settings.SettingsManager.RespawnSettings.DeductBailFee)
    GenerateTotalBailFee();
DisplayBailNotification(respawnableLocation.Name);
ShowImpoundDisplay();
GameTimeLastSurrenderedToPolice = Game.GameTime;
EntryPoint.WriteToConsole($"POST 1: {Time.CurrentDateTime} {BailDuration}");
```

---

## Tuning constants

Edit the top of `PrisonSentenceTracker.cs`:

```csharp
private const int BASE_SENTENCE_MINUTES = 15;  // First offence
private const int INCREMENT_MINUTES     = 5;   // Added per additional arrest
private const int MAX_SENTENCE_MINUTES  = 60;  // Hard cap
```
