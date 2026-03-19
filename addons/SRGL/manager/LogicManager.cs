namespace SRGL;

using Godot;
using System;
using SRGL.Common;
using System.Collections.Generic;

public partial class LogicManager: Node
{
    private Chart _c;
    private SongPlayer _sp;
    private JudgementQueue _jq;

    private int _svIndex;
    private int _noteIndex;
    private int _barlineIndex;

    private long _userOffsetUsec;

    /// <summary>default value: 3_000_000 us (= 3 sec)</summary>
    public long LookAheadUsec = 3 * 1_000_000;

    // event
    public event Action<int, NoteVisualData> NoteSpawned;
    public event Action<double> NotePositionUpdated;

    public LogicManager(Chart c, SongPlayer sp, JudgementQueue jq, InputMapper im, long userOffsetUsec)
    {
        // verify that jq has a strategy for every LogicType in c
        HashSet<int> logicTypes = new HashSet<int>();
        Verifier v = new Verifier();

        foreach(NoteData data in c._notes) { logicTypes.Add(data.LogicData.LogicType); }
        foreach(int logicType in logicTypes) { v.Ensure(jq.HasStrategy(logicType), () => $"no strategy for LogicType = {logicType}"); }
        v.ThrowIfInvalid();

        // chart
        _c = c;
        
        // audio
        _sp = sp;

        // judgement
        _jq = jq;

        // input
        InputListener il = new InputListener(im);
        im.LaneInputChanged += OnLaneInputChanged;
        AddChild(il);

        // options
        _userOffsetUsec = userOffsetUsec;
    }
    
    public void Reset()
    {
        _sp.Stop();
        _jq.Clear();
        
        _svIndex = 0;
        _noteIndex = 0;
        _barlineIndex = 0;
    }

    public override void _Process(double delta)
    {
        long ticksUsec = (long)Time.GetTicksUsec();

        // compensate audio drift
        _sp.SyncWithAudio(delta, ticksUsec);

        // calculate time and position
        long timeUsec = _sp.GetSongTimeUsec(ticksUsec) - _sp.AudioLatencyUsec - _userOffsetUsec - _c.OffsetUsec;
        double position = Converter.TimeToPosition((double)timeUsec / 1_000_000, _c._svChanges, ref _svIndex);
        
        // ======== notes ========
        while(0 <= _noteIndex && _noteIndex < _c._notes.Length &&
              timeUsec + LookAheadUsec >= _c._notes[_noteIndex].LogicData.StartTimeUsec)
        {
            // logic
            int id = _jq.EnqueueNote(_c._notes[_noteIndex].LogicData);

            // visual
            if(id >= 0) { NoteSpawned?.Invoke(id, _c._notes[_noteIndex].VisualData); }

            _noteIndex++;
        }
        
        // ======== barlines ========
        while(0 <= _barlineIndex && _barlineIndex < _c._barlines.Length &&
              timeUsec + LookAheadUsec >= _c._barlines[_barlineIndex].LogicData.StartTimeUsec)
        {
            // logic
            int id = _jq.EnqueueNote(_c._barlines[_barlineIndex].LogicData);

            // visual
            if(id >= 0) { NoteSpawned?.Invoke(id, _c._barlines[_barlineIndex].VisualData); }

            _barlineIndex++;
        }
        
        // update
        NotePositionUpdated?.Invoke(position);
        if(_sp.Playing) { _jq.Update(timeUsec); }
    }

    private void OnLaneInputChanged(long ticksUsec, int laneIndex, bool pressed)
    {
        if(_sp.Playing)
        {
            long timeUsec = _sp.GetSongTimeUsec(ticksUsec) - _sp.AudioLatencyUsec - _userOffsetUsec - _c.OffsetUsec;

            if(pressed) { _jq.Press(timeUsec, laneIndex); }
            else { _jq.Release(timeUsec, laneIndex); }
        }
    }
}
