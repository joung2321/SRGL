namespace SRGL;

using Godot;
using System;
using SRGL.Common;

public abstract partial class EffectObject : Node2D, IPoolable
{
    [Export] private float _lifetimeSec;

    private Timer _t;
    
    public override void _Ready()
    {
        // add a timer as a child
        _t = new Timer();
        _t.WaitTime = _lifetimeSec;
        _t.OneShot = true;
        _t.Timeout += InvokeReturnToPool;

        AddChild(_t);
    }

    public void Play(Judgement judgement, Node2D judgementPoint)
    {
        Position = judgementPoint.Position;

        OnPlay(judgement, judgementPoint);
        _t.Start();
    }

    /// <summary>
    /// Play your animation here.<br/>
    /// [CAUTION] Before playing a animation, stop the animation first.
    /// </summary>
    /// <param name="judgement"></param>
    /// <param name="judgementPoint"></param>
    protected abstract void OnPlay(Judgement judgement, Node2D judgementPoint);

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
