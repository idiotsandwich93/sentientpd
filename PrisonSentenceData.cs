using System;

/// <summary>
/// Serializable data for persisting prison sentence state across sessions.
/// Saved to Plugins\LosSantosRED\PrisonSentences.xml
/// </summary>
[Serializable]
public class PrisonSentenceData
{
    /// <summary>Total lifetime arrests sent to Bolingbroke (used to scale sentence length).</summary>
    public int ArrestCount { get; set; } = 0;

    /// <summary>Real-world DateTime when the current sentence began.</summary>
    public DateTime SentenceStartTime { get; set; } = DateTime.MinValue;

    /// <summary>Length of the current sentence in real-world minutes.</summary>
    public int SentenceMinutes { get; set; } = 0;

    /// <summary>True while the player has not yet served the current sentence.</summary>
    public bool IsServingSentence { get; set; } = false;
}
