using System;
using System.IO;
using System.Xml.Serialization;

/// <summary>
/// Tracks real-world prison sentences for Bolingbroke Penitentiary.
///
/// Sentence scaling:
///   1st arrest  = BASE_SENTENCE_MINUTES         (default 15 min)
///   2nd arrest  = BASE_SENTENCE_MINUTES + INCREMENT_MINUTES * 1
///   3rd arrest  = BASE_SENTENCE_MINUTES + INCREMENT_MINUTES * 2
///   ...etc.
///
/// Persists across game sessions via XML at SAVE_FILE path.
/// </summary>
public class PrisonSentenceTracker
{
    // -------------------------------------------------------------------------
    // Configuration — tweak these to taste
    // -------------------------------------------------------------------------
    private const int BASE_SENTENCE_MINUTES = 15;   // First-offence sentence
    private const int INCREMENT_MINUTES     = 5;    // Added for every additional arrest
    private const int MAX_SENTENCE_MINUTES  = 60;   // Hard cap so it never gets absurd
    private const string DEFAULT_SAVE_FILE  = "Plugins\\LosSantosRED\\PrisonSentences.xml";
    // -------------------------------------------------------------------------

    private readonly string _savePath;
    private readonly Action<string> _log;
    private PrisonSentenceData _data;

    /// <summary>
    /// Production constructor — uses the default save path and Rage logging.
    /// </summary>
    public PrisonSentenceTracker()
        : this(DEFAULT_SAVE_FILE, msg => Rage.Game.LogTrivial(msg)) { }

    /// <summary>
    /// Testable constructor — caller supplies the save path and a log sink.
    /// Pass a temp file path and a no-op action to avoid any GTA/Rage dependency.
    /// </summary>
    public PrisonSentenceTracker(string savePath, Action<string> log)
    {
        _savePath = savePath;
        _log      = log;
        Load();
    }

    // ------------------------------------------------------------------
    // Public read-only state
    // ------------------------------------------------------------------

    public int  ArrestCount       => _data.ArrestCount;
    public int  SentenceMinutes   => _data.SentenceMinutes;

    /// <summary>True when the player still has real-world time left to serve.</summary>
    public bool IsServingSentence =>
        _data.IsServingSentence &&
        DateTime.Now < _data.SentenceStartTime.AddMinutes(_data.SentenceMinutes);

    /// <summary>How much real-world time is left. Zero when sentence is complete.</summary>
    public TimeSpan RemainingTime =>
        IsServingSentence
            ? _data.SentenceStartTime.AddMinutes(_data.SentenceMinutes) - DateTime.Now
            : TimeSpan.Zero;

    // ------------------------------------------------------------------
    // Sentence lifecycle
    // ------------------------------------------------------------------

    /// <summary>
    /// Call this when the player is booked and sent to Bolingbroke.
    /// Increments the arrest counter, calculates sentence length, and starts the timer.
    /// </summary>
    public void OnArrested()
    {
        _data.ArrestCount++;
        int rawSentence = BASE_SENTENCE_MINUTES + (_data.ArrestCount - 1) * INCREMENT_MINUTES;
        _data.SentenceMinutes   = Math.Min(rawSentence, MAX_SENTENCE_MINUTES);
        _data.SentenceStartTime = DateTime.Now;
        _data.IsServingSentence = true;
        Save();
    }

    /// <summary>
    /// Check whether the sentence has been served.
    /// Returns true (and clears the flag) once real-world time has elapsed.
    /// </summary>
    public bool TryRelease()
    {
        if (!_data.IsServingSentence)
            return true;

        if (DateTime.Now >= _data.SentenceStartTime.AddMinutes(_data.SentenceMinutes))
        {
            _data.IsServingSentence = false;
            Save();
            return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // UI helpers — call these inside your game-loop to show the timer
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns a formatted GTA notification string showing remaining sentence time.
    /// Use with Game.DisplayHelp() or Game.DisplayNotification() each loop tick.
    /// </summary>
    public string GetCountdownText()
    {
        TimeSpan remaining = RemainingTime;
        string timeStr     = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
        string offenceStr  = OrdinalSuffix(_data.ArrestCount) + " offense";
        string sentenceStr = $"{_data.SentenceMinutes} min sentence";

        return
            $"~r~Bolingbroke Penitentiary~s~~n~" +
            $"~y~{offenceStr}~s~ · {sentenceStr}~n~" +
            $"Time remaining: ~r~{timeStr}";
    }

    // ------------------------------------------------------------------
    // Persistence
    // ------------------------------------------------------------------

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var serializer = new XmlSerializer(typeof(PrisonSentenceData));
            using (var writer = new StreamWriter(_savePath))
                serializer.Serialize(writer, _data);
        }
        catch (Exception ex)
        {
            _log($"[PrisonSentenceTracker] Save failed: {ex.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                var serializer = new XmlSerializer(typeof(PrisonSentenceData));
                using (var reader = new StreamReader(_savePath))
                    _data = (PrisonSentenceData)serializer.Deserialize(reader);
            }
            else
            {
                _data = new PrisonSentenceData();
            }
        }
        catch
        {
            _data = new PrisonSentenceData();
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string OrdinalSuffix(int n)
    {
        switch (n % 100)
        {
            case 11: case 12: case 13: return $"{n}th";
        }
        switch (n % 10)
        {
            case 1:  return $"{n}st";
            case 2:  return $"{n}nd";
            case 3:  return $"{n}rd";
            default: return $"{n}th";
        }
    }
}
