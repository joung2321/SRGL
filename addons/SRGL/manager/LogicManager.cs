namespace SRGL;

using Godot;
using System;
using SRGL.Common;

public partial class LogicManager: Node
{
    private Chart _c;
    private SongPlayer _sp;
    private JudgementQueue _jq;

    private int _svIndex;
    private int _noteIndex;
    private int _barlineIndex;
    
    private long _audioLatencyUsec;
    private long _userOffsetUsec;

    /// <summary>default value: 3_000_000 us (= 3 sec)</summary>
    public long LookAheadUsec = 3 * 1_000_000;

    // event
    public event Action<int, NoteVisualData> NoteSpawned;
    public event Action<double> NotePositionUpdated;

    public LogicManager(Chart c, SongPlayer sp, JudgementQueue jq, ButtonStateMachine bsm, long userOffsetUsec)
    {
        // chart
        _c = c;
        
        // audio
        _sp = sp;

        // judgement
        _jq = jq;

        // input
        InputManager im = new InputManager(bsm);
        bsm.ButtonEvent += OnButtonEvent;
        AddChild(im);

        // options
        _audioLatencyUsec = (long)Math.Round(AudioServer.GetOutputLatency() * 1_000_000);
        _userOffsetUsec = userOffsetUsec;
    }
    
    public void Init()
    {
        _sp.Stop();
        _jq.Clear();

        _svIndex = 0;
        _noteIndex = 0;
        _barlineIndex = 0;
    }

    public override void _Process(double delta)
    {
        long timeUsec = _sp.GetSongTimeUsec((long)Time.GetTicksUsec()) - _audioLatencyUsec - _userOffsetUsec - _c.OffsetUsec;
        double position = _c.TimeToPosition((double)timeUsec / 1_000_000, ref _svIndex);
        
        // notes
        while(_c.TryGetNoteData(_noteIndex, out NoteData data) && timeUsec + LookAheadUsec >= data.LogicData.StartTimeUsec)
        {
            // logic
            int id = _jq.EnqueueNote(data.LogicData);
            _noteIndex++;
            
            // visual
            if(id >= 0) { NoteSpawned?.Invoke(id, data.VisualData); }
        }
        
        // barlines
        while(_c.TryGetBarlineData(_barlineIndex, out NoteData data) && timeUsec + LookAheadUsec >= data.LogicData.StartTimeUsec)
        {
            // logic
            int id = _jq.EnqueueNote(data.LogicData);
            _barlineIndex++;
            
            // visual
            if(id >= 0) { NoteSpawned?.Invoke(id, data.VisualData); }
        }
        
        // update
        NotePositionUpdated?.Invoke(position);
        if(_sp.Playing) { _jq.Update(timeUsec); }
    }

    private void OnButtonEvent(long ticksUsec, int laneIndex, bool pressed)
    {
        if(_sp.Playing)
        {
            long timeUsec = _sp.GetSongTimeUsec(ticksUsec) - _audioLatencyUsec - _userOffsetUsec - _c.OffsetUsec;

            if(pressed) { _jq.Press(timeUsec, laneIndex); }
            else { _jq.Release(timeUsec, laneIndex); }
        }
    }
}
