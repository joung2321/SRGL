namespace SRGL;

using Godot;
using System;

public abstract partial class PoolableNode2D : Node2D, IPoolable
{
    private bool _isSpawned = false;

    public event Action<IPoolable> ReturnToPool;
    public void InvokeReturnToPool() // wrapping of ReturnToPool
    {
        if(!_isSpawned) { return; }

        ReturnToPool?.Invoke(this);
        _isSpawned = false;
    }

    public override void _ExitTree()
    {
        ReturnToPool = null;
    }

    // template method pattern
    protected abstract void _OnSpawn();
    protected abstract void _OnDespawn();
    protected abstract void _SetActive(bool active);

    public void OnSpawn()
    {
        _isSpawned = true;
        _OnSpawn();
    }
    public void OnDespawn() { _OnDespawn(); }
    public void SetActive(bool active) { _SetActive(active); }
}
