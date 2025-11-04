using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// [Constructor param]
///     createFunc: Function to create a new instance when the Pool runs out of objects
///     initSize: Number of instances created when the pool is initialized
///     maxSize: Maximum number of instances allowed in the Pool
///     actionOnGet: Action to apply to T instance when invoking the Get() method
///     actionOnRelease: Action to apply to T instance when invoking the Release() method
///     actionOnDestroy: Action to apply to T instance when invoking the Remove()|Clear()|Dispose() method
///     isNullPredicate: Predicate to apply to check if the instance is null. Consider `true` to be null when returned
///     logger: If a problem occurs, the corresponding Action is used to log it
///
/// [Instance method]
///     T Get(): Get instance from Pool
///     bool Release(T instance): Return the instance back to the pool
///     bool Release(Predicate<T> predicate): Returns an instance that meets the condition to the pool
///     bool ReleaseAll(): Returns all active instances to the pool
///     bool Remove(T instance): Remove the instance
///     bool Remove(Predicate<T> predicate): Remove the instance that meets the condition
///     void Clear(): Remove all instances
///
/// <![CDATA[
/// // example code
/// 
/// public class GameObjectPool : MonoBehaviour
/// {
///     private Pool<GameObject> _pool;
///     public GameObject prefab;
///
///     private void Awake()
///     {
///         _pool = new Pool<GameObject>(
///             createFunc: () => Instantiate(prefab),
///             initSize: 10,
///             maxSize: 100,
///             actionOnGet: obj => obj.SetActive(true),
///             actionOnRelease: obj => obj.SetActive(false),
///             actionOnDestroy: obj => Destroy(obj)
///         );
///     }
///
///     public GameObject GetObject() => _pool.Get();
///     public void ReleaseObject(GameObject obj) => _pool.Release(obj);
///
///     private void OnDestroy() => _pool.Dispose();
/// }
/// ]]>
/// </summary>
/// <typeparam name="T">Element Type</typeparam>
public class Pool<T> : IEnumerable<T>, IDisposable where T : class
{
    #region Private fields
    private LinkedList<T> _pool;

    private HashSet<T> _activeInstances;

    private readonly Func<T> _createFunc;

    private readonly Action<T> _actionOnGet;
    
    private readonly Action<T> _actionOnRelease;

    private readonly Action<T> _actionOnDestroy;

    private readonly Predicate<T> _isNullPredicate;

    private readonly int _initSize;

    private readonly int _maxSize;

    private readonly HashSet<T> _tempCache = new();

    private bool _disposed = false;
    #endregion

    #region Const
    private const int DEFAULT_INIT_SIZE = 1000;
    private const int DEFAULT_MAX_SIZE = 10000;
    #endregion

    #region Properties
    public int CountAll => CountActive + CountInactive;
    public int CountActive => _activeInstances.Count;
    public int CountInactive => _pool.Count;
    #endregion

    #region Initialize Interface
    public Pool(Func<T> createFunc, 
                int initSize = DEFAULT_INIT_SIZE, 
                int maxSize = DEFAULT_MAX_SIZE, 
                Action<T> actionOnGet = null,
                Action<T> actionOnRelease = null, 
                Action<T> actionOnDestroy = null, 
                Predicate<T> isNullPredicate = null,
                Action<string> logger = null)
    {
        if (logger != null)
        {
            Logger = logger;
        }

        if (createFunc == null)
        {
            Logger?.Invoke("Create func cannot be null");
            return;
        }

        _createFunc = createFunc ?? throw new ArgumentNullException($"{GetType().Name} parameter '{nameof(createFunc)}' is cannot be null");;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
        _actionOnDestroy = actionOnDestroy;
        _initSize = Math.Min(initSize, maxSize);
        _maxSize = maxSize;
        _isNullPredicate = isNullPredicate ?? (instance => instance == null);
        
        Init();
    }

    public Action<string> Logger { get; set; } = Debug.LogWarning;
    #endregion

    #region Public methods
    public T Get()
    {
        CheckDispose();

        T pooled = null;
        if (_pool.Count > 0)
        {
            pooled = _pool.First();
            _pool.RemoveFirst();
            if (IsNull(pooled))
            {
                RemoveNull();
                if (_pool.Count > 0)
                {
                    pooled = _pool.First();
                    _pool.RemoveFirst();
                }
                else if (_activeInstances.Count > 0)
                {
                    pooled = _createFunc?.Invoke();
                }
                else
                {
                    Logger?.Invoke("Reinitializing the entire pool");
                    Clear();
                    Instantiate();
                    pooled = null;
                    if (_pool.Count > 0)
                    {
                        pooled = _pool.First();
                        _pool.RemoveFirst();
                    }
                }
            }
        }
        else
        {
            pooled = _createFunc?.Invoke();
        }

        if (IsNull(pooled))
        {
            Logger?.Invoke("Create func failed to create object");
            return null;
        }

        if (_activeInstances.Add(pooled))
        {
            _actionOnGet?.Invoke(pooled);
            return pooled;
        }
        Logger?.Invoke("Duplicate instance: " + pooled);
        return null;
    }

    public bool Release(T instance)
    {
        CheckDispose();

        if (IsNull(instance))
        {
            Logger?.Invoke("Attempted to release null instance");
            RemoveNull();
            return false;
        }

        if (!_activeInstances.Remove(instance))
        {
            Logger?.Invoke("Instance not found in active instance: " + instance.ToString());
            return false;
        }

        _actionOnRelease?.Invoke(instance);

        if (_pool.Count <= _maxSize)
        {
            _pool.AddLast(instance);
        }
        else
        {
            _actionOnDestroy?.Invoke(instance);
        }
        return true;
    }

    public bool Release(Predicate<T> predicate)
    {
        CheckDispose();

        bool success = Filter(predicate, _activeInstances, _tempCache);

        if (!success)
        {
            return false;
        }

        foreach (T instance in _tempCache)
        {
            Release(instance);
        }

        return _tempCache.Count > 0;
    }

    public bool ReleaseAll()
    {
        CheckDispose();

        if (_activeInstances.Count == 0)
        {
            return false;
        }

        _tempCache.Clear();
        foreach (T instance in _activeInstances)
        {
            _tempCache.Add(instance);
        }

        bool isAnyRelease = false;
        foreach (T instance in _tempCache)
        {
            if (Release(instance))
            {
                isAnyRelease = true;
            }
        }

        return isAnyRelease;
    }

    public bool Remove(T instance)
    {
        CheckDispose();

        if (IsNull(instance))
            return false;

        bool success = _activeInstances.Remove(instance) || _pool.Remove(instance);

        if (success)
        {
            _actionOnDestroy?.Invoke(instance);
        }

        return success;
    }

    public bool Remove(Predicate<T> predicate)
    {
        CheckDispose();

        bool success = FilterTwo(predicate, _activeInstances, _pool, _tempCache);

        if (!success)
        {
            return false;
        }

        foreach (T instance in _tempCache)
        {
            Remove(instance);
        }

        return _tempCache.Count > 0;
    }

    public void Clear()
    {
        CheckDispose();

        DestroyInvokeAll();
        _pool.Clear();
        _activeInstances.Clear();
    }
    #endregion

    #region Other methods
    private void Init()
    {
        _pool = new LinkedList<T>();
        _activeInstances = new HashSet<T>();
        Instantiate();
    }

    private void Instantiate()
    {
        int poolSize = Math.Min(_initSize, _maxSize);
        for (int i = 0; i < poolSize; i++)
        {
            T instance = _createFunc?.Invoke();
            if (IsNull(instance))
            {
                Debug.LogWarning("Create func returns null");
                break;
            }
            _pool.AddLast(instance);
        }
    }

    private void DestroyInvokeAll()
    {
        foreach (T instance in _pool)
        {
            if (!IsNull(instance))
            {
                _actionOnDestroy?.Invoke(instance);
            }
        }

        foreach (T instance in _activeInstances)
        {
            if (!IsNull(instance))
            {
                _actionOnDestroy?.Invoke(instance);
            }
        }
    }

    private bool Filter(Predicate<T> predicate, IEnumerable<T> instances, HashSet<T> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result is null");
        }

        result.Clear();

        if (predicate == null)
        {
            Logger?.Invoke("Predicate cannot be null");
            return false;
        }

        
        foreach (T instance in instances)
        {
            if (!IsNull(instance) && predicate(instance))
            {
                result.Add(instance);
            }
        }
        return true;
    }

    private bool FilterTwo(Predicate<T> predicate, IEnumerable<T> instances1, IEnumerable<T> instances2, HashSet<T> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result is null");
        }

        result.Clear();

        if (predicate == null)
        {
            Logger?.Invoke("Predicate cannot be null");
            return false;
        }


        foreach (T instance in instances1)
        {
            if (!IsNull(instance) && predicate(instance))
            {
                result.Add(instance);
            }
        }

        foreach (T instance in instances2)
        {
            if (!IsNull(instance) && predicate(instance))
            {
                result.Add(instance);
            }
        }
        return true;
    }

    private void RemoveNull()
    {
        if (_pool.Count > 0)
        {
            LinkedListNode<T> current = _pool.First;
            while (current != null)
            {
                LinkedListNode<T> next = current.Next;

                if (IsNull(current.Value))
                {
                    _pool.Remove(current);
                }

                current = next;
            }
        }
        
        _activeInstances.RemoveWhere(IsNull);
        Logger?.Invoke("Null instances removed");
    }
    
    private bool IsNull(T instance)
    {
        if (instance == null)
            return true;
        
        return _isNullPredicate(instance);
    }

    private void CheckDispose()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Pool<T>));
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Clear();
        }
        _disposed = true;
    }

    ~Pool()
    {
        Dispose(false);
    }

    public IEnumerator<T> GetEnumerator() => _pool.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}