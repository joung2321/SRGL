namespace SRGL;

using System;

public interface IPoolable
{
    /// <summary>
    /// To return to a object pool, invoke this event.
    /// </summary>
    event Action<IPoolable> ReturnToPool;

    /// <summary>
    /// Called by ObjectPool.Spawn()
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// Called by ObjectPool.Despawn()
    /// </summary>
    void OnDespawn();
    
    void SetActive(bool active);
}