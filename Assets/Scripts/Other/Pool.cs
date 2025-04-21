using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pool<T> : IEnumerable<T>, IDisposable where T : class
{
    #region Private fields
    private Queue<T> _pool;

    private HashSet<T> _activeInstances;

    private readonly Func<T> _createFunc;

    private readonly Action<T> _actionOnGet;
    
    private readonly Action<T> _actionOnRelease;

    private readonly Action<T> _actionOnDestroy;

    private readonly Predicate<T> _isNullPredicate;

    private readonly int _initSize;

    private readonly int _maxSize;

    private readonly HashSet<T> _foundCache = new();

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

    #region Constructor
    public Pool(Func<T> createFunc, 
                int initSize = DEFAULT_INIT_SIZE, 
                int maxSize = DEFAULT_MAX_SIZE, 
                Action<T> actionOnGet = null,
                Action<T> actionOnRelease = null, 
                Action<T> actionOnDestroy = null, 
                Predicate<T> isNullPredicate = null)
    {
        if (createFunc == null)
        {
            Debug.LogError("Create func cannot be null");
            return;
        }

        _createFunc = createFunc;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
        _actionOnDestroy = actionOnDestroy;
        _initSize = Mathf.Min(initSize, maxSize);
        _maxSize = maxSize;
        _isNullPredicate = isNullPredicate ?? (instance => false);
        
        Init();
    }
    #endregion

    #region Public methods
    public T Get()
    {
        CheckDispose();

        T pooled = null;
        if (_pool.Count > 0)
        {
            pooled = _pool.Dequeue();
            if (IsNull(pooled))
            {
                RemoveNull();
                if (_pool.Count > 0)
                {
                    pooled = _pool.Dequeue();
                }
                else if (_activeInstances.Count > 0)
                {
                    pooled = _createFunc?.Invoke();
                }
                else
                {
                    Debug.LogError("Reinitializing the entire pool");
                    Clear();
                    Instantiate();
                    pooled = _pool.Count > 0 ? _pool.Dequeue() : null;
                }
            }
        }
        else
        {
            pooled = _createFunc?.Invoke();
        }

        if (IsNull(pooled))
        {
            Debug.LogError("Create func failed to create object");
            return null;
        }

        if (_activeInstances.Add(pooled))
        {
            _actionOnGet?.Invoke(pooled);
            return pooled;
        }
        Debug.LogError("Duplicate instance: " + pooled);
        return null;
    }

    public bool Release(T instance)
    {
        CheckDispose();

        if (IsNull(instance))
        {
            Debug.LogWarning("Attempted to release null instance");
            RemoveNull();
            return false;
        }

        if (!_activeInstances.Remove(instance))
        {
            Debug.LogWarning("Instance not found in active instance: " + instance.ToString());
            return false;
        }

        _actionOnRelease?.Invoke(instance);

        if (_pool.Count <= _maxSize)
        {
            _pool.Enqueue(instance);
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

        bool success = Filter(predicate, _activeInstances, _foundCache);

        if (!success)
        {
            return false;
        }

        foreach (T instance in _foundCache)
        {
            Release(instance);
        }

        return _foundCache.Count > 0;
    }

    public bool Remove(T instance)
    {
        CheckDispose();

        bool success = false;
        if (!IsNull(instance))
        {
            success = _activeInstances.Remove(instance);
            if (!success)
            {
                success = _pool.Contains(instance);
                if (success)
                {
                    _pool = new Queue<T>(_pool.Where(item => item != instance));
                }
            }
            if (success)
            {
                _actionOnDestroy?.Invoke(instance);
            }
        }
        return success;
    }

    public bool Remove(Predicate<T> predicate)
    {
        CheckDispose();

        bool success = FilterTow(predicate, _activeInstances, _pool, _foundCache);

        if (!success)
        {
            return false;
        }

        foreach (T instance in _foundCache)
        {
            Remove(instance);
        }

        return _foundCache.Count > 0;
    }

    public void Clear()
    {
        CheckDispose();

        DestroyInvoke();
        _pool.Clear();
        _activeInstances.Clear();
    }
    #endregion

    #region Other methods
    private void Init()
    {
        _pool = new();
        _activeInstances = new();
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
            _pool.Enqueue(instance);
        }
    }

    private void DestroyInvoke()
    {
        foreach (T instance in _pool.Concat(_activeInstances))
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
            Debug.LogWarning("Predicate cannot be null");
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

    private bool FilterTow(Predicate<T> predicate, IEnumerable<T> instances1, IEnumerable<T> instances2, HashSet<T> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result is null");
        }

        result.Clear();

        if (predicate == null)
        {
            Debug.LogWarning("Predicate cannot be null");
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
        _pool = new Queue<T>(_pool.Where(instance => !IsNull(instance)));
        _activeInstances.RemoveWhere(IsNull);
        Debug.LogError("Null instances removed");
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