namespace SRGL;

using System.Collections.Immutable;
using SRGL.Common;

public class Chart
{
    // ======== metadata ========
    public string FormatVersion { get; init; }
    
    public string Title { get; init; }
    public string Composer { get; init; }
    public string Illustrator { get; init; }
    public string Charter { get; init; }

    public int DifficultyCategory { get; init; }
    public float DifficultyLevel { get; init; }

    public string Description { get; init; }

    // ======== chart data ========
    public string ImagePath { get; init; }
    public string AudioPath { get; init; }

    private long _ppqn;
    public int LaneCount { get; init; }
    public int TotalCombo { get; init; }
    public long OffsetUsec { get; init; }

    public ImmutableArray<Tempo> _tempos { get; init; }
    public ImmutableArray<TimeSignature> _timeSignatures { get; init; }
    public ImmutableArray<SvChange> _svChanges { get; init; }

    public ImmutableArray<NoteData> _notes { get; init; }
    public ImmutableArray<NoteData> _barlines { get; init; }
    // ======== end ========

    public Chart(RawChart rawChart)
    {
        // ======== metadata ========
        FormatVersion = rawChart.FormatVersion;
        Title = rawChart.Title;
        Composer = rawChart.Composer;
        Illustrator = rawChart.Illustrator;
        Charter = rawChart.Charter;
        
        DifficultyCategory = rawChart.DifficultyCategory;
        DifficultyLevel = rawChart.DifficultyLevel;
        
        Description = rawChart.Description;
        
        // ======== preprocess raw chart ========
        ImagePath = rawChart.ImagePath;
        AudioPath = rawChart.AudioPath;

        _ppqn = rawChart.PPQN;
        LaneCount = rawChart.LaneCount;
        OffsetUsec = rawChart.OffsetUsec;
        
        _tempos = Preprocessor.PreprocessTempos(rawChart.Tempos, rawChart.PPQN).ToImmutableArray();
        _timeSignatures = Preprocessor.PreprocessTimeSignatures(rawChart.TimeSignatures, rawChart.PPQN).ToImmutableArray();
        _svChanges = Preprocessor.PreprocessSvChanges(rawChart.SvChanges, rawChart.PPQN, _tempos).ToImmutableArray();

        _notes = Preprocessor.PreprocessNotes(rawChart.Notes, rawChart.PPQN, _tempos, _timeSignatures, _svChanges).ToImmutableArray();
        _barlines = Preprocessor.GenerateBarlines(rawChart.EndOfTrack, _ppqn, _timeSignatures, _tempos, _svChanges).ToImmutableArray();
        // ======== end ========

        // calculate total combo
        TotalCombo = 0;
        foreach(NoteData data in _notes)
        {
            // ignore dummy notes
            if((data.LogicData.Options & NoteOptions.Dummy) != 0) { continue; }

            TotalCombo++; // start time
            TotalCombo += data.LogicData.MiddleTimesUsec.Length; // middle times
            if((data.LogicData.Options & NoteOptions.CheckRelease) != 0) { TotalCombo++; } // end time
        }
    }
}