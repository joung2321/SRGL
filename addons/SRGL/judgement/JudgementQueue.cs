namespace SRGL;

using System;
using System.Collections.Generic;
using SRGL.Common;

public class JudgementQueue
{
    // note id
    private int _nextId;

    // judgement criteria
    private TimingWindow _tw;
    private Dictionary<int, IJudgementStrategy> _strategies;

    // non-dummy notes
    private Queue<NoteLogicInstance>[] _qs; // queues
    private ActiveNoteTracker[] _trackers;

    // dummy notes
    private Queue<NoteLogicInstance> _dummies;

    // ======== events ========
    public event Action<Judgement> NoteJudged;
    public event Action<int> NoteDespawned;
    public event Action<int, NoteState> NoteStateChanged;

    // wrapping of this.NoteJudged
    private void InvokeNoteJudged(Judgement judgement) { NoteJudged?.Invoke(judgement); }
    private readonly Action<Judgement> _cachedInvokeNoteJudged; // cached delegate
    // ======== end ========

    public JudgementQueue(int laneCount, TimingWindow tw)
    {
        // note id
        _nextId = 0;

        // judgement criteria
        _tw = tw;
        _strategies = new Dictionary<int, IJudgementStrategy>();

        // non-dummy notes
        if(laneCount <= 0)
        {
            _qs = Array.Empty<Queue<NoteLogicInstance>>();
            _trackers = Array.Empty<ActiveNoteTracker>();
        }
        else
        {
            _qs = new Queue<NoteLogicInstance>[laneCount];
            _trackers = new ActiveNoteTracker[laneCount];

            for(int i=0; i<laneCount; i++)
            {
                _qs[i] = new Queue<NoteLogicInstance>();
                _trackers[i] = new ActiveNoteTracker();
                _trackers[i].Init();
            }
        }

        // dummy notes
        _dummies = new Queue<NoteLogicInstance>();

        // cache delegate
        _cachedInvokeNoteJudged = InvokeNoteJudged;
    }

    public void AddStrategy(int logicType, IJudgementStrategy strategy)
    {
        _strategies.Add(logicType, strategy);
    }

    /// <returns>Non-negative note id if the given note is successfully enquened; otherwise, -1.</returns>
    public int EnqueueNote(NoteLogicData data)
    {
        int id = -1;

        if((data.Options & NoteOptions.Dummy) != 0)
        {
            id = _nextId;
            _dummies.Enqueue(new NoteLogicInstance{ Id = _nextId, Data = data });
            _nextId++;
        }
        else if(0 <= data.Lane && data.Lane < _qs.Length)
        {
            id = _nextId;
            _qs[data.Lane].Enqueue(new NoteLogicInstance{ Id = _nextId, Data = data });
            _nextId++;
        }

        return id;
    }

    public void Clear()
    {
        // note id
        _nextId = 0;

        // non-dummy notes
        for(int i=0, len=_qs.Length; i<len; i++)
        {
            _qs[i].Clear();
            _trackers[i].Init();
        }

        // dummy notes
        _dummies.Clear();
    }
    
    public void Press(long timeUsec, int laneIndex)
    {
        if(_qs[laneIndex].TryPeek(out NoteLogicInstance nli) && _strategies.TryGetValue(nli.Data.LogicType, out IJudgementStrategy strategy))
        {
            NoteState prevState = _trackers[laneIndex].State;
            bool shouldDespawn = strategy.OnPress(timeUsec, nli, _trackers[laneIndex], _tw, _cachedInvokeNoteJudged);

            if(shouldDespawn)
            {
                _qs[laneIndex].Dequeue();
                _trackers[laneIndex].Init();
                NoteDespawned?.Invoke(nli.Id);
            }
            else if(prevState != _trackers[laneIndex].State)
            {
                NoteStateChanged?.Invoke(nli.Id, _trackers[laneIndex].State);
            }
        }
    }

    public void Release(long timeUsec, int laneIndex)
    {
        if(_qs[laneIndex].TryPeek(out NoteLogicInstance nli) && _strategies.TryGetValue(nli.Data.LogicType, out IJudgementStrategy strategy))
        {
            NoteState prevState = _trackers[laneIndex].State;
            bool shouldDespawn = strategy.OnRelease(timeUsec, nli, _trackers[laneIndex], _tw, _cachedInvokeNoteJudged);

            if(shouldDespawn)
            {
                _qs[laneIndex].Dequeue();
                _trackers[laneIndex].Init();
                NoteDespawned?.Invoke(nli.Id);
            }
            else if(prevState != _trackers[laneIndex].State)
            {
                NoteStateChanged?.Invoke(nli.Id, _trackers[laneIndex].State);
            }
        }
    }

    public void Update(long timeUsec)
    {
        // non-dummy notes
        for(int i=0, len=_qs.Length; i<len; i++)
        {
            while(_qs[i].TryPeek(out NoteLogicInstance nli) && _strategies.TryGetValue(nli.Data.LogicType, out IJudgementStrategy strategy))
            {
                NoteState prevState = _trackers[i].State;
                bool shouldDespawn = strategy.OnUpdate(timeUsec, nli, _trackers[i], _tw, _cachedInvokeNoteJudged);

                if(shouldDespawn)
                {
                    _qs[i].Dequeue();
                    _trackers[i].Init();
                    NoteDespawned?.Invoke(nli.Id);
                }
                else
                {
                    if(prevState != _trackers[i].State) { NoteStateChanged?.Invoke(nli.Id, _trackers[i].State); }
                    break;
                }
            }
        }

        // dummy notes
        while(_dummies.TryPeek(out NoteLogicInstance nli))
        {
            if(_tw.IsTooLate(nli.Data.StartTimeUsec, timeUsec))
            {
                _dummies.Dequeue();
                NoteDespawned?.Invoke(nli.Id);
            }
            else { break; }
        }
    }
}