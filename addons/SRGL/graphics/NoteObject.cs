namespace SRGL;

using Godot;
using System;
using SRGL.Common;

public abstract partial class NoteObject : Node2D, IPoolable
{
    protected NoteVisualData _visualData { get; private set; }
    protected Node2D _judgementPoint { get; private set; }
    protected NoteState _state { get; private set; }

    public void Init(NoteVisualData visualData, int variationIndex, Node2D judgementPoint)
    {
        _visualData = visualData;
        _judgementPoint = judgementPoint;

        SetState(NoteState.Idle);
        SetVisualVariation(variationIndex);
    }

    public void SetState(NoteState state)
    {
        _state = state;
        OnStateChanged(state);
    }

    protected virtual void OnStateChanged(NoteState state) {}

    public virtual void SetVisualVariation(int variationIndex) {}

    /// <summary>
    /// e.g.) Updating position of a tap note:
    /// <code>Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position)) * Vector2.Up;</code>
    /// </summary>
    public abstract void UpdatePosition(double position, double userSpeedPxPerSec);

    // ======== implementation of IPoolable ========
    public event Action<IPoolable> ReturnToPool;
    public void InvokeReturnToPool() { ReturnToPool?.Invoke(this); } // wrapping of IPoolable.ReturnToPool

    public void OnDespawn() {}
    public void OnSpawn() {}

    public void SetActive(bool active)
    {
        Visible = active;
        ProcessMode = active? ProcessModeEnum.Inherit: ProcessModeEnum.Disabled;
    }
    // ======== end ========
}