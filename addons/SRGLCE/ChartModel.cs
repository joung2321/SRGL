namespace SRGLCE;

using Godot;
using System;
using System.Collections.Generic;
using SRGL;
using SRGL.Common;
using System.Text.Json;

// a chart file which is currently open
public class ChartModel
{
    public string FilePath;

    // ======== metadata ========
    public const string FormatVersion = "0.1.0"; // semantic versioning e.g.) 1.2.3

    public string Title;
    public string Composer;
    public string Illustrator;
    public string Charter;

    public int DifficultyCategory;
    public float DifficultyLevel;

    public string Description;

    // ======== chart data ========
    public string ImagePath;
    public string AudioPath = "";

    private const long DEFAULT_PPQN = 960;
    private long _ppqn = DEFAULT_PPQN; // pulses per quarter note
    public long PPQN
    {
        get => _ppqn;
        set
        {
            if(value <= 0) { GD.PrintErr("Invalid PPQN"); }
            else { _ppqn = value; }
        }
    }
    
    public long EndOfTrack; // length of this chart (PPQN value)
    public int LaneCount => _lanes.Count; // the number of logical lanes
    public long OffsetUsec; // audio offset [us]

    private List<RawChart.RawTempo> _tempos = new List<RawChart.RawTempo>(64);
    private List<RawChart.RawTimeSignature> _timeSignatures = new List<RawChart.RawTimeSignature>(64);
    private List<RawChart.RawSvChange> _svChanges = new List<RawChart.RawSvChange>(64);
    private List<List<RawChart.RawNote>> _lanes = new List<List<RawChart.RawNote>>(8);

    public IList<RawChart.RawTempo> Tempos => _tempos.AsReadOnly();
    public IList<RawChart.RawTimeSignature> TimeSignatures => _timeSignatures.AsReadOnly();
    public IList<RawChart.RawSvChange> SvChanges => _svChanges.AsReadOnly();
    public IList<RawChart.RawNote> GetLane(int laneIndex)
    {
        if(0 <= laneIndex && laneIndex < _lanes.Count) { return _lanes[laneIndex].AsReadOnly(); }
        else { return null; }
    }

    // constructor
    public ChartModel() { LoadDefaultChart(); }

    // clears lists safely
    private void ClearLists()
    {
        // clear lists
        _tempos.Clear();
        _timeSignatures.Clear();
        _svChanges.Clear();

        // clear _lanes (list of lists)
        for(int i=0; i<_lanes.Count; i++)
        {
            if(_lanes[i] != null)
            {
                _lanes[i].Clear();
                _lanes[i] = null; // free memory
            }
        }
        _lanes.Clear();
    }
    
    public void LoadDefaultChart()
    {
        ClearLists();

        FilePath = null;

        Title = "";
        Composer = "";
        Illustrator = "";
        Charter = "";
        
        DifficultyCategory = 0;
        DifficultyLevel = 0;
        Description = "";
        
        _ppqn = DEFAULT_PPQN;

        for(int i=0; i<4; i++) { InsertLane(0); }
        InsertTempo(new RawChart.RawTempo{ StartTick = 0, Bpm = 90 });
        InsertTimeSignature(new RawChart.RawTimeSignature{ StartTick = 0, Numerator = 4, Denominator = 4 });
        InsertSvChange(new RawChart.RawSvChange{ StartTick = 0, Multiplier = 1, Interpolation = InterpolationType.Step });
    }

    public void Load(string path)
    {
        // load chart file
        RawChart rc = RawChartLoader.Load(path);

        // sort arrays
        if(rc.Tempos         != null) { Array.Sort(rc.Tempos); }
        if(rc.TimeSignatures != null) { Array.Sort(rc.TimeSignatures); }
        if(rc.SvChanges      != null) { Array.Sort(rc.SvChanges); }
        if(rc.Notes          != null) { Array.Sort(rc.Notes); }
        
        // verify raw chart
        try { RawChartVerifier.Verify(rc); }
        catch(SrglException e)
        {
            GD.PrintErr(e.Message);
            return;
        }

        // fill info
        FilePath = path;

        Title = rc.Title;
        Composer = rc.Composer;
        Illustrator = rc.Illustrator;
        Charter = rc.Charter;

        DifficultyCategory = rc.DifficultyCategory;
        DifficultyLevel = rc.DifficultyLevel;

        Description = rc.Description;

        ImagePath = rc.ImagePath;
        // AudioPath = rc.AudioPath;

        PPQN = rc.PPQN;
        EndOfTrack = rc.EndOfTrack;
        OffsetUsec = rc.OffsetUsec;

        // clear lists
        ClearLists();

        // fill lists
        foreach(RawChart.RawTempo x in rc.Tempos) { _tempos.Add(x); }
        foreach(RawChart.RawTimeSignature x in rc.TimeSignatures) { _timeSignatures.Add(x); }
        foreach(RawChart.RawSvChange x in rc.SvChanges) { _svChanges.Add(x); }

        // fill list of lists
        for(int i=0; i<rc.LaneCount; i++) { InsertLane(0); }
        foreach(RawChart.RawNote x in rc.Notes) { InsertNote(x); }
    }

    // converts this ChartModel to JSON formatted string
    public string Serialize()
    {
        // sort lists
        _tempos.Sort();
        _timeSignatures.Sort();
        _svChanges.Sort();

        // serialize list of lists
        int noteCount = 0;
        List<RawChart.RawNote> notes;

        foreach(List<RawChart.RawNote> lane in _lanes) { noteCount += lane.Count; }
        notes = new List<RawChart.RawNote>(noteCount);
        foreach(List<RawChart.RawNote> lane in _lanes) { notes.AddRange(lane); }
        notes.Sort();

        // raw chart
        RawChart rc = new RawChart
        {
            FormatVersion = FormatVersion,

            Title = Title,
            Composer = Composer,
            Illustrator = Illustrator,
            Charter = Charter,

            DifficultyCategory = DifficultyCategory,
            DifficultyLevel = DifficultyLevel,

            Description = Description,

            ImagePath = ImagePath,
            AudioPath = "",

            PPQN = PPQN,
            EndOfTrack = EndOfTrack,
            LaneCount = LaneCount,
            OffsetUsec = OffsetUsec,

            Tempos = _tempos.ToArray(),
            TimeSignatures = _timeSignatures.ToArray(),
            SvChanges = _svChanges.ToArray(),
            Notes = notes.ToArray()
        };

        return JsonSerializer.Serialize(rc, RawChartJsonContext.Default.RawChart);
    }

    // ======== wrapping of List.BinarySearch ========
    public int IndexOfTempoAt(long startTick)
    {
        RawChart.RawTempo tmp = new RawChart.RawTempo{ StartTick = startTick };
        return _tempos.BinarySearch(tmp);
    }

    public int IndexOfTimeSignatureAt(long startTick)
    {
        RawChart.RawTimeSignature tmp = new RawChart.RawTimeSignature{ StartTick = startTick };
        return _timeSignatures.BinarySearch(tmp);
    }

    public int IndexOfSvChangeAt(long startTick)
    {
        RawChart.RawSvChange tmp = new RawChart.RawSvChange{ StartTick = startTick };
        return _svChanges.BinarySearch(tmp);
    }

    public int IndexOfNoteAt(long startTick, int laneIndex)
    {
        if(laneIndex < 0 || laneIndex >= _lanes.Count) { throw new SrglException("invalid laneIndex"); }

        RawChart.RawNote tmp = new RawChart.RawNote{ StartTick = startTick };
        return _lanes[laneIndex].BinarySearch(tmp);
    }

    // ======== lane ========
    public void InsertLane(int laneIndex)
    {
        _lanes.Insert(laneIndex, new List<RawChart.RawNote>(128));
    }

    public void RemoveLane(int laneIndex)
    {
        _lanes.RemoveAt(laneIndex);
    }

    // ======== tempo ========
    public void InsertTempo(RawChart.RawTempo value)
    {
        int index = _tempos.BinarySearch(value);

        if(index < 0 && value.IsValid())
        {
            index = ~index;
            _tempos.Insert(index, value);
        }
    }

    public void UpdateTempo(RawChart.RawTempo oldValue, RawChart.RawTempo newValue)
    {
        int oldIndex = _tempos.BinarySearch(oldValue);
        if(oldIndex < 0) { return; }

        if(oldValue.StartTick == newValue.StartTick)
        {
            _tempos[oldIndex] = newValue;
        }
        else if(newValue.IsValid()) { InsertTempo(newValue); }
    }

    public void RemoveTempo(RawChart.RawTempo value)
    {
        // protect initial tempo
        if(value.StartTick == 0) { return; }

        int index = _tempos.BinarySearch(value);
        if(index >= 0)
        {
            _tempos.RemoveAt(index);
        }
    }

    // ======== time signature ========
    public void InsertTimeSignature(RawChart.RawTimeSignature value)
    {
        int index = _timeSignatures.BinarySearch(value);

        if(index < 0 && value.IsValid())
        {
            index = ~index;
            _timeSignatures.Insert(index, value);
        }
    }

    public void UpdateTimeSignature(RawChart.RawTimeSignature oldValue, RawChart.RawTimeSignature newValue)
    {
        int oldIndex = _timeSignatures.BinarySearch(oldValue);
        if(oldIndex < 0) { return; }

        if(oldValue.StartTick == newValue.StartTick)
        {
            _timeSignatures[oldIndex] = newValue;
        }
        else if(newValue.IsValid()) { InsertTimeSignature(newValue); }
    }

    public void RemoveTimeSignature(RawChart.RawTimeSignature value)
    {
        // protect initial time signature
        if(value.StartTick == 0) { return; }

        int index = _timeSignatures.BinarySearch(value);
        if(index >= 0)
        {
            _timeSignatures.RemoveAt(index);
        }
    }

    // ======== sv change ========
    public void InsertSvChange(RawChart.RawSvChange value)
    {
        int index = _svChanges.BinarySearch(value);

        if(index < 0 && value.IsValid())
        {
            index = ~index;
            _svChanges.Insert(index, value);
        }
    }

    public void UpdateSvChange(RawChart.RawSvChange oldValue, RawChart.RawSvChange newValue)
    {
        int oldIndex = _svChanges.BinarySearch(oldValue);
        if(oldIndex < 0) { return; }

        if(oldValue.StartTick == newValue.StartTick)
        {
            _svChanges[oldIndex] = newValue;
        }
        else if(newValue.IsValid()) { InsertSvChange(newValue); }
    }

    public void RemoveSvChange(RawChart.RawSvChange value)
    {
        // protect initial tempo
        if(value.StartTick == 0) { return; }

        int index = _svChanges.BinarySearch(value);
        if(index >= 0)
        {
            _svChanges.RemoveAt(index);
        }
    }

    // ======== note ========
    public void InsertNote(RawChart.RawNote value)
    {
        // check lane validity
        if(!value.IsValid(LaneCount)) { return; }

        int index = _lanes[value.Lane].BinarySearch(value);

        if(index < 0)
        {
            index = ~index;
            _lanes[value.Lane].Insert(index, value);
        }
    }

    public void UpdateNote(RawChart.RawNote oldValue, RawChart.RawNote newValue)
    {
        // check lane validity
        if(!oldValue.IsValid(LaneCount) || !newValue.IsValid(LaneCount)) { return; }

        int oldIndex = _lanes[oldValue.Lane].BinarySearch(oldValue);
        if(oldIndex < 0) { return; }

        if(oldValue.Lane == newValue.Lane && oldValue.StartTick == newValue.StartTick)
        {
            _lanes[oldValue.Lane][oldIndex] = newValue;
        }
        else
        {
            InsertNote(newValue);
            
            long newIndex = _lanes[newValue.Lane].BinarySearch(newValue);
            if(newIndex >= 0) { RemoveNote(oldValue); }
        }
    }

    public void RemoveNote(RawChart.RawNote value)
    {
        // check lane validity
        if(!value.IsValid(LaneCount)) { return; }

        int index = _lanes[value.Lane].BinarySearch(value);
        if(index >= 0)
        {
            _lanes[value.Lane].RemoveAt(index);
        }
    }
}
