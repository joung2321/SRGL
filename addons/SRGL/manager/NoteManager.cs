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
    private Dictionary<int, NoteObject> _spawnedNotes;

    public NoteManager(JudgementLine judgementLine, Func<int, int> selectVisualVariation)
    {
        _judgementLine = judgementLine;
        SelectVisualVariation = selectVisualVariation;

        _notePools = new Dictionary<int, ObjectPool<NoteObject>>();
        _spawnedNotes = new Dictionary<int, NoteObject>();
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

            _spawnedNotes[id] = no;
        }
    }

    public void UpdateNotePosition(double position)
    {
        foreach(KeyValuePair<int, NoteObject> kvp in _spawnedNotes)
        {
            kvp.Value.UpdatePosition(position, UserSpeedPxPerSec);
        }
    }

    public void ChangeNoteState(int id, NoteState state)
    {
        if(_spawnedNotes.TryGetValue(id, out NoteObject note))
        {
            note.SetState(state);
        }
    }

    public void DespawnNote(int id)
    {
        if(_spawnedNotes.TryGetValue(id, out NoteObject note))
        {
            _spawnedNotes[id].InvokeReturnToPool();
            _spawnedNotes.Remove(id);
        }
    }

    public void DespawnAllNotes()
    {
        foreach(KeyValuePair<int, NoteObject> kvp in _spawnedNotes)
        {
            kvp.Value.InvokeReturnToPool();
        }
        _spawnedNotes.Clear();
    }

    public void Listen(LogicManager lm)
    {
        lm.NoteSpawned += SpawnNote;
        lm.NotePositionUpdated += UpdateNotePosition;
        lm.NoteStateChanged += ChangeNoteState;
        lm.NoteDespawned += DespawnNote;
    }
}
