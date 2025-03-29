using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ������Ʈ Ǯ�� ��� ���� ����
/// 
/// **Constructor param
/// createFunc: ȣ�⸶�� ���ο���� ��ȯ�ϴ� �ν��Ͻ� ���� �޼���
/// initSize: ���� ���� �ν��Ͻ� ����
/// maxSize: Ǯ �ִ� ũ��
/// actionOnGet: Get()�� �ش� �ν��Ͻ��� ������ Action
/// actionOnRelease: Release()�� �ش� �ν��Ͻ��� ������ Action
/// actionOnDestroy: �ν��Ͻ� �ı� �� ������ Action
/// 
/// **Public properties
/// CountAll: ��ü �ν��Ͻ� ����
/// CountActive: Ȱ��ȭ�� �ν��Ͻ� ����
/// CountInactive: Ǯ �ȿ� �ִ� �ν��Ͻ� ����
/// 
/// **Public methods
/// Get(): Pool���� �ν��Ͻ� Get
/// Release(T instance): �ν��Ͻ� ȸ��
/// Release(Predicate<T> predicate): ���ǿ� �´� �ν��Ͻ� ȸ��
/// Remove(T instance): �ν��Ͻ� �ı�
/// Release(Predicate<T> predicate): ���ǿ� �´� �ν��Ͻ� �ı�
/// Clear(): ��ü �ν��Ͻ� �ı�
/// 
/// IEnumerable<T>, IDisposable ����
/// </summary>
/// <typeparam name="T">Type of pool</typeparam>
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
        if (_pool.Count > 0)  // pool�� �������� ���� ��
        {
            pooled = _pool.Dequeue();  // pool���� �������� ���� �õ�
            if (IsNull(pooled))  // pool���� ������ ��ü�� null
            {
                RemoveNull();  // ���� ��Ȳ: pool, actives�� null ���� �õ�
                if (_pool.Count > 0)  // null ���� �� pool���� �������� ���� �õ�
                {
                    pooled = _pool.Dequeue();
                }
                else if (_activeInstances.Count > 0)  // actives�� �����ִ� ������Ʈ�� �ִٸ� ������ַ� �Ǵ�
                {
                    pooled = _createFunc?.Invoke();
                }
                else  // pool�� ������� ��. actives�� ����ִٸ� ��ü�� ������ ���� ������ �Ǵ�, Ǯ �ʱ�ȭ
                {
                    Debug.LogError("Reinitializing the entire pool");
                    Clear();
                    Instantiate();
                    pooled = _pool.Count > 0 ? _pool.Dequeue() : null;
                }
            }
        }
        else  // pool�� ����ִ� ��Ȳ
        {
            pooled = _createFunc?.Invoke();
        }

        if (IsNull(pooled))  // �ռ� ����ó���� ��ġ���� null�� ��ȯ�ϸ� �״�� null ��ȯ
        {
            Debug.LogError("Create func failed to create object");
            return null;
        }

        if (_activeInstances.Add(pooled))  // �ߺ��� ������Ʈ�� ��� �Ұ�
        {
            _actionOnGet?.Invoke(pooled);
            return pooled;
        }
        Debug.LogError("Duplicate instance: " + pooled.ToString());
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

        List<T> foundInstances = Filter(predicate, _activeInstances);
        if (foundInstances == null)
        {
            return false;
        }

        foreach (T instance in foundInstances)
        {
            Release(instance);
        }
        return foundInstances.Count > 0;
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

        List<T> foundInstances = Filter(predicate, _activeInstances.Concat(_pool));
        if (foundInstances == null)
        {
            return false;
        }

        foreach (T instance in foundInstances)
        {
            Remove(instance);
        }
        return foundInstances.Count > 0;
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
        int poolSize = Mathf.Min(_initSize, _maxSize);
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

    private List<T> Filter(Predicate<T> predicate, IEnumerable<T> instances)
    {
        if (predicate == null)
        {
            Debug.LogWarning("Predicate cannot be null");
            return null;
        }
        
        List<T> foundInstances = new();
        foreach (T instance in instances)
        {
            if (!IsNull(instance) && predicate(instance))
            {
                foundInstances.Add(instance);
            }
        }
        return foundInstances;
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