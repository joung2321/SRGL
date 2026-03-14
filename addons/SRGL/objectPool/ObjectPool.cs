namespace SRGL;

using Godot;
using System.Collections.Generic;
using System.Linq;

public class ObjectPool<T> where T: Node2D, IPoolable
{
    private Node2D _parent;
    private PackedScene _scene;

    private Stack<T> _pool;
    private HashSet<T> _spawnedObjects;

    public ObjectPool(Node2D parent, string scenePath, int poolSize = 0)
    {
        _parent = parent;
        _scene = GD.Load<PackedScene>(scenePath);

        _pool = new Stack<T>();
        _spawnedObjects = new HashSet<T>();

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
        _spawnedObjects.Add(obj);

        return obj;
    }

    private void Despawn(IPoolable obj)
    {
        if(obj is T tObj && _spawnedObjects.Remove(tObj))
        {
            tObj.OnDespawn();
            tObj.SetActive(false);

            _pool.Push(tObj);
        }
    }

    public void DespawnAll()
    {
        foreach(T obj in _spawnedObjects.ToArray())
        {
            Despawn(obj);
        }
    }
}