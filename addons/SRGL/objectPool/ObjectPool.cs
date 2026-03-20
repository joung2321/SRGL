namespace SRGL;

using Godot;
using System.Collections.Generic;

/// <summary>
/// [CAUTION] For simplicity, do NOT define ObjectPool.DespawnAll()!
/// </summary>
public class ObjectPool<T> where T: PoolableNode2D
{
    private Node2D _parent;
    private PackedScene _scene;

    private Stack<T> _pool;

    public ObjectPool(Node2D parent, string scenePath, int poolSize = 0)
    {
        _parent = parent;
        _scene = GD.Load<PackedScene>(scenePath);

        _pool = new Stack<T>();

        if(poolSize > 0)
        {
            for(int i=0; i<poolSize; i++)
            {
                _pool.Push(CreateNewObject());
            }
        }
    }
    
    private T CreateNewObject()
    {
        T obj = _scene.Instantiate<T>();
        obj.SetActive(false);
        obj.ReturnToPool += Despawn;

        _parent.AddChild(obj);

        return obj;
    }

    public T Spawn()
    {
        T obj = (_pool.Count > 0)? _pool.Pop(): CreateNewObject();

        obj.OnSpawn();

        return obj;
    }

    private void Despawn(IPoolable obj) // Action<IPoolable>
    {
        if(obj is T tObj)
        {
            tObj.OnDespawn();
            tObj.SetActive(false);

            _pool.Push(tObj);
        }
    }
}