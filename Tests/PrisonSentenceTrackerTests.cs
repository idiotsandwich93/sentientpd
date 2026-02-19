// PrisonSentenceTrackerTests.cs
// xUnit test project — no GTA/Rage dependency required.
//
// To run:
//   1. Create a new "xUnit Test Project (.NET Framework 4.8)" in your solution
//   2. Add references to PrisonSentenceData.cs and PrisonSentenceTracker.cs
//      (Add as linked files so you share the same source)
//   3. dotnet test  (or run via Visual Studio Test Explorer)

using System;
using System.IO;
using System.Threading;
using Xunit;

public class PrisonSentenceTrackerTests : IDisposable
{
    // Each test gets its own temp file so they never interfere
    private readonly string _tempFile;
    private readonly Action<string> _noop = _ => { };

    public PrisonSentenceTrackerTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"sentientpd_test_{Guid.NewGuid()}.xml");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    private PrisonSentenceTracker Fresh() =>
        new PrisonSentenceTracker(_tempFile, _noop);

    // -----------------------------------------------------------------------
    // Sentence length calculation
    // -----------------------------------------------------------------------

    [Fact]
    public void FirstArrest_Is15Minutes()
    {
        var tracker = Fresh();
        tracker.OnArrested();
        Assert.Equal(15, tracker.SentenceMinutes);
    }

    [Fact]
    public void SecondArrest_Is20Minutes()
    {
        var tracker = Fresh();
        tracker.OnArrested(); // 1st — finishes sentence for test purposes
        // Manually clear the serving flag by reloading with a modified file:
        // Easier: just call OnArrested twice on a fresh tracker
        tracker = Fresh();
        tracker.OnArrested();  // +0 increments → 15 min
        tracker.OnArrested();  // +1 increment  → 20 min
        Assert.Equal(20, tracker.SentenceMinutes);
    }

    [Theory]
    [InlineData(1,  15)]
    [InlineData(2,  20)]
    [InlineData(3,  25)]
    [InlineData(5,  35)]
    [InlineData(10, 60)]  // hits the 60-min cap
    [InlineData(20, 60)]  // still capped
    public void SentenceScales_AndCapsAt60(int arrests, int expectedMinutes)
    {
        var tracker = Fresh();
        for (int i = 0; i < arrests; i++)
            tracker.OnArrested();
        Assert.Equal(expectedMinutes, tracker.SentenceMinutes);
    }

    [Fact]
    public void ArrestCount_IncrementsEachCall()
    {
        var tracker = Fresh();
        tracker.OnArrested();
        tracker.OnArrested();
        tracker.OnArrested();
        Assert.Equal(3, tracker.ArrestCount);
    }

    // -----------------------------------------------------------------------
    // TryRelease / IsServingSentence
    // -----------------------------------------------------------------------

    [Fact]
    public void TryRelease_ReturnsFalse_WhileTimeRemaining()
    {
        var tracker = Fresh();
        tracker.OnArrested();
        // Sentence is 15 minutes — nowhere near elapsed yet
        Assert.False(tracker.TryRelease());
        Assert.True(tracker.IsServingSentence);
    }

    [Fact]
    public void TryRelease_ReturnsTrue_WhenNotServingSentence()
    {
        var tracker = Fresh(); // fresh with no sentence
        Assert.True(tracker.TryRelease());
    }

    [Fact]
    public void RemainingTime_IsZero_WhenNotServing()
    {
        var tracker = Fresh();
        Assert.Equal(TimeSpan.Zero, tracker.RemainingTime);
    }

    [Fact]
    public void RemainingTime_IsPositive_WhileServing()
    {
        var tracker = Fresh();
        tracker.OnArrested();
        Assert.True(tracker.RemainingTime > TimeSpan.Zero);
    }

    // -----------------------------------------------------------------------
    // Persistence (save → load round-trip)
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveFile_IsCreated_AfterArrest()
    {
        Fresh().OnArrested();
        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void ArrestCount_PersistsAcrossInstances()
    {
        var t1 = Fresh();
        t1.OnArrested();
        t1.OnArrested();

        // Simulate game restart — new instance reads same file
        var t2 = new PrisonSentenceTracker(_tempFile, _noop);
        Assert.Equal(2, t2.ArrestCount);
    }

    [Fact]
    public void IsServingSentence_PersistsAcrossInstances()
    {
        var t1 = Fresh();
        t1.OnArrested();
        Assert.True(t1.IsServingSentence);

        var t2 = new PrisonSentenceTracker(_tempFile, _noop);
        Assert.True(t2.IsServingSentence);
    }

    [Fact]
    public void SentenceMinutes_PersistsAcrossInstances()
    {
        var t1 = Fresh();
        t1.OnArrested();
        t1.OnArrested(); // 20 min

        var t2 = new PrisonSentenceTracker(_tempFile, _noop);
        Assert.Equal(20, t2.SentenceMinutes);
    }

    [Fact]
    public void CorruptSaveFile_FallsBackToFreshState()
    {
        File.WriteAllText(_tempFile, "<<not valid xml>>");
        var tracker = new PrisonSentenceTracker(_tempFile, _noop);
        Assert.Equal(0, tracker.ArrestCount);
        Assert.False(tracker.IsServingSentence);
    }

    // -----------------------------------------------------------------------
    // GetCountdownText
    // -----------------------------------------------------------------------

    [Fact]
    public void GetCountdownText_ContainsOrdinalAndTime()
    {
        var tracker = Fresh();
        tracker.OnArrested();
        string text = tracker.GetCountdownText();
        Assert.Contains("1st", text);
        Assert.Contains("15 min", text);
        // Timer should be close to 15:00 — just check the format NN:NN exists
        Assert.Matches(@"\d{2}:\d{2}", text);
    }

    [Fact]
    public void GetCountdownText_Uses2nd3rd4thOrdinals()
    {
        var t = Fresh();
        t.OnArrested(); t.OnArrested(); // 2nd
        Assert.Contains("2nd", t.GetCountdownText());

        t = Fresh();
        t.OnArrested(); t.OnArrested(); t.OnArrested(); // 3rd
        Assert.Contains("3rd", t.GetCountdownText());
    }
}
