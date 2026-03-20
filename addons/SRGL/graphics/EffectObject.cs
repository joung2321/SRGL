namespace SRGL;

using Godot;
using SRGL.Common;

public abstract partial class EffectObject : PoolableNode2D
{
    // timer
    [Export] private double _lifetimeSec;
    private double _remainingTimeSec;

    public override void _EnterTree()
    {
        ZIndex = 1; // EffectObject should be in front of NoteObject
    }

    public void Play(Judgement judgement, Node2D judgementPoint)
    {
        Position = judgementPoint.Position;

        OnPlay(judgement);
        _remainingTimeSec = _lifetimeSec;
    }

    public override void _Process(double delta)
    {
        _remainingTimeSec -= delta;
        
        if(_remainingTimeSec <= 0) { InvokeReturnToPool(); }
    }

    /// <summary>
    /// Play your animation here.<br/>
    /// [CAUTION] Before playing a animation, stop the animation first.
    /// </summary>
    protected abstract void OnPlay(Judgement judgement);

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
