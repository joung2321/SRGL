namespace SRGL;

using Godot;
using SRGL.Common;

public abstract partial class NoteObject : PoolableNode2D
{
    protected NoteVisualData _visualData { get; private set; }
    protected Node2D _judgementPoint { get; private set; }
    protected NoteState _state { get; private set; }

    public void Init(NoteVisualData visualData, int variationIndex, Node2D judgementPoint)
    {
        _visualData = visualData;
        _judgementPoint = judgementPoint;

        SetState(NoteState.Idle);
        OnInit(visualData, variationIndex);
    }

    public void SetState(NoteState state)
    {
        if(_state == state) { return; }

        _state = state;
        OnStateChanged();
    }

    protected virtual void OnInit(NoteVisualData visualData, int variationIndex) {}

    /// <summary>
    /// Called when the note's state has changed.
    /// </summary>
    protected virtual void OnStateChanged() {}

    /// <summary>
    /// e.g.) Updating position of a tap note:
    /// <code>Position = _judgementPoint.Position + (float)(userSpeedPxPerSec * (_visualData.Position - position)) * Vector2.Up;</code>
    /// </summary>
    public abstract void UpdatePosition(double position, double userSpeedPxPerSec);

    // ======== implementation of PoolableNode2D ========
    protected override void _OnDespawn() {}
    protected override void _OnSpawn() {}

    protected override void _SetActive(bool active)
    {
        Visible = active;
        ProcessMode = active? ProcessModeEnum.Inherit: ProcessModeEnum.Disabled;
    }
    // ======== end ========
}