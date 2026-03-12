namespace SRGL;

using System.Collections.Immutable;
using SRGL.Common;

public class Chart
{
    // ======== metadata ========

    // ======== chart data ========
    public string ImagePath { get; private set; }
    public string AudioPath { get; private set; }

    private long _ppqn;
    public int LaneCount { get; private set; }
    public int TotalCombo { get; private set; }
    public long OffsetUsec { get; private set; }

    public ImmutableArray<Tempo> _tempos { get; private set; }
    public ImmutableArray<TimeSignature> _timeSignatures { get; private set; }
    public ImmutableArray<SvChange> _svChanges { get; private set; }

    public ImmutableArray<NoteData> _notes { get; private set; }
    public ImmutableArray<NoteData> _barlines { get; private set; }
    // ======== end ========

    public Chart(RawChart rawChart)
    {
        // ======== metadata ========
        
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