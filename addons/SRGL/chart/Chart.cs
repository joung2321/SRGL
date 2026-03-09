namespace SRGL;

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

    private Tempo[] _tempos;
    private TimeSignature[] _timeSignatures;
    private SvChange[] _svChanges;

    private NoteData[] _notes;
    private NoteData[] _barlines;
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

        _timeSignatures = Preprocessor.PreprocessTimeSignatures(rawChart.TimeSignatures, rawChart.PPQN);
        _tempos = Preprocessor.PreprocessTempos(rawChart.Tempos, rawChart.PPQN);

        _svChanges = Preprocessor.PreprocessSvChanges(rawChart.SvChanges, rawChart.PPQN, _tempos);
        _notes = Preprocessor.PreprocessNotes(rawChart.Notes, rawChart.PPQN, _tempos, _timeSignatures, _svChanges);

        _barlines = Preprocessor.GenerateBarlines(rawChart.EndOfTrack, _ppqn, _timeSignatures, _tempos, _svChanges);
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

    public bool TryGetNoteData(int index, out NoteData note)
    {
        if(0 <= index && index < _notes.Length)
        {
            note = _notes[index];
            return true;
        }
        else
        {
            note = new NoteData
            {
                LogicData = new NoteLogicData { StartTimeUsec = long.MaxValue }
            };
            return false;
        }
    }

    public bool TryGetBarlineData(int index, out NoteData barline)
    {
        if(0 <= index && index < _barlines.Length)
        {
            barline = _barlines[index];
            return true;
        }
        else
        {
            barline = new NoteData
            {
                LogicData = new NoteLogicData { StartTimeUsec = long.MaxValue }
            };
            return false;
        }
    }

    public double TimeToPosition(double targetTimeSec, ref int cachedIndex)
    {
        return Converter.TimeToPosition(targetTimeSec, _svChanges, ref cachedIndex);
    }
}