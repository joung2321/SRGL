namespace SRGL;

using Godot;
using System;
using SRGL.Common;

public abstract partial class EffectObject : Node2D, IPoolable
{
    // timer
    [Export] private double _lifetimeSec;
    private double _remainingTimeSec;

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
