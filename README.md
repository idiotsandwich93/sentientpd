# sentientpd — Bolingbroke Real-Time Prison Sentences

An add-on for [Los Santos RED](https://github.com/thatoneguy650/Los-Santos-RED) that enforces **real-world wait times** when a player is sent to Bolingbroke Penitentiary.

---

## How it works

| Arrest # | Sentence length |
|----------|----------------|
| 1st      | 15 minutes      |
| 2nd      | 20 minutes      |
| 3rd      | 25 minutes      |
| …        | +5 min each     |
| (cap)    | 60 minutes max  |

* The timer uses **real-world clock time**, so closing and reopening the game does **not** reset it.
* While serving, the player is **frozen in place** at Bolingbroke and sees a live countdown.
* On release a notification confirms the sentence is served and shows the arrest count.
* All data persists to `Plugins\LosSantosRED\PrisonSentences.xml`.

---

## Files

| File | Purpose |
|------|---------|
| `PrisonSentenceData.cs` | Serialisable data model (arrest count, sentence start time, minutes, serving flag) |
| `PrisonSentenceTracker.cs` | Core logic — `OnArrested()`, `TryRelease()`, `GetCountdownText()`, save/load |

---

## Integration into LSR

Add both `.cs` files to the **Los Santos RED** Visual Studio project (same solution), then apply the following three changes to `lsr/Player/Respawning/Respawning.cs`:

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
