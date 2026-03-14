namespace SRGL;

using System;
using System.Collections.Generic;
using SRGL.Common;

public class NoteManager
{
    private readonly JudgementLine _judgementLine;
    private readonly Func<int, int> SelectVisualVariation;

    public double UserSpeedPxPerSec;

    private Dictionary<int, ObjectPool<NoteObject>> _notePools;

    // ======== NoteManager tracks spawned notes using swap-and-pop. ========
    private const int _capacity = 512;
    private int _count; // the number of spawned notes, index for dense array

    // sparse array
    private int[] _idToIndex = new int[_capacity]; // In dense arrays, (a index of a note whose id is x) = _idToIndex[x % _capacity].

    // dense array
    private NoteObject[] _spawnedNotes = new NoteObject[_capacity];
    private int[] _spawnedNoteIds = new int[_capacity]; // _spawnedNoteIds[x] = hashed id of _spawnedNotes[x]
    // ======== end ========

    public NoteManager(JudgementLine judgementLine, Func<int, int> selectVisualVariation)
    {
        _judgementLine = judgementLine;
        SelectVisualVariation = selectVisualVariation;

        _notePools = new Dictionary<int, ObjectPool<NoteObject>>();

        _count = 0;
        for(int i=0; i<_capacity; i++)
        {
            _idToIndex[i] = -1;
            _spawnedNoteIds[i] = -1;
        }
    }

    /// <summary>
    /// Add a scene whose root node is a derived class of NoteObject.<br/>
    /// [CAUTION] For barlines, visualType is Constants.BarlineVisualType = -1.
    /// </summary>
    public void AddNoteType(int visualType, string scenePath, int poolSize = 0)
    {
        _notePools[visualType] = new ObjectPool<NoteObject>(_judgementLine, scenePath, poolSize);
    }

    public void SpawnNote(int id, NoteVisualData visualData)
    {
        if(_notePools.TryGetValue(visualData.VisualType, out ObjectPool<NoteObject> p))
        {
            NoteObject no = p.Spawn();
            no.Init(visualData, SelectVisualVariation(visualData.Lane), _judgementLine.GetJudgementPoint(visualData.Lane));
            no.SetActive(true);

            int hashedId = id % _capacity;
            if(_idToIndex[hashedId] >= 0)
            {
                throw new SrglException("note id collision. use larger NoteManager._capacity");
            }
            else
            {
                _idToIndex[hashedId] = _count;
                _spawnedNotes[_count] = no;
                _spawnedNoteIds[_count] = hashedId;
                _count++;
            }
        }
    }

    public void UpdateNotePosition(double position)
    {
        // linear traversal in dense array
        for(int i=0; i<_count; i++)
        {
            _spawnedNotes[i].UpdatePosition(position, UserSpeedPxPerSec);
        }
    }

    public void ChangeNoteState(int id, NoteState state)
    {
        int index = _idToIndex[id % _capacity];

        if(index >= 0) { _spawnedNotes[index].SetState(state); }
    }

    public void DespawnNote(int id)
    {
        int hashedId = id % _capacity;
        int index = _idToIndex[hashedId];

        if(index >= 0)
        {
            // despawn note (make a hole in the dense array)
            _idToIndex[hashedId] = -1;
            _spawnedNotes[index].InvokeReturnToPool();
            _spawnedNotes[index] = null;
            _spawnedNoteIds[index] = -1;

            // subtract 1 from _count (Now, _spawnedNoteIds[_count] means the backmost note in the dense array.)
            _count--;

            // fill the hole
            if(index < _count)
            {
                int hashedId_back = _spawnedNoteIds[_count];

                _idToIndex[hashedId_back] = index;
                _spawnedNotes[index] = _spawnedNotes[_count];
                _spawnedNoteIds[index] = hashedId_back;

                _spawnedNotes[_count] = null;
                _spawnedNoteIds[_count] = -1;
            }
        }
    }

    public void DespawnAllNotes()
    {
        // linear traversal in dense array
        for(int i=0; i<_count; i++)
        {
            int hashedId = _spawnedNoteIds[i];

            _idToIndex[hashedId] = -1;
            _spawnedNotes[i].InvokeReturnToPool();
            _spawnedNotes[i] = null;
            _spawnedNoteIds[i] = -1;
        }
        _count = 0;
    }

    public void Listen(JudgementQueue jq, LogicManager lm)
    {
        jq.NoteStateChanged += ChangeNoteState;
        jq.NoteDespawned += DespawnNote;

        lm.NoteSpawned += SpawnNote;
        lm.NotePositionUpdated += UpdateNotePosition;
    }
}
