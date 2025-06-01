using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.Collections;
using UnityEngine;
using static TPEnumeratorToken;

/// <summary>
/// Node에서 참조할 TP객체들을 가져올 수 있는 토큰
/// </summary>
public class TPEnumeratorToken : IEnumerable<ITypeListenStateful>, IReadonlyToken, IDisposable
{
    #region Non Interface
    private StatefulAdapter[] _adapters;
    private bool _isReadonly = false;
    private bool _isNameDuplicated = true;
    private bool _disposed = false;

    public TPEnumeratorToken(IEnumerable<ITransitionPoint> tps)
    {
        _adapters = tps.Select(tp => new StatefulAdapter(tp)).ToArray();
    }

    ~TPEnumeratorToken()
    {
        ((IDisposable)this).Dispose();
    }
    #endregion

    #region Interface
    public ITypeListenStateful this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range. Count: {Count}");
            }

            return _adapters[index];
        }
    }

    public ITypeListenStateful this[string name]
    {
        get
        {
            if (_isNameDuplicated)
            {
                throw new AmbiguousMatchException($"Token has duplicate name: {name}");
            }

            StatefulAdapter matchedAdapter = _adapters.FirstOrDefault(adapter => adapter.Name == name);

            if (matchedAdapter == null)
            {
                throw new KeyNotFoundException($"Token with name '{name}' not found");
            }

            return matchedAdapter;
        }
    }

    public int Count => _adapters.Length;

    public bool HasOnlyNull
    {
        get
        {

            for (int i = 0; i < _adapters.Length; i++)
            {
                if (!_adapters[i].State.IsNull)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool HasAnyNull
    {
        get
        {
            for (int i = 0; i < _adapters.Length; i++)
            {
                if (_adapters[i].State.IsNull)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 첫번째 인덱스에 state를 적용합니다
    /// </summary>
    /// <param name="state">State</param>
    /// <exception cref="IndexOutOfRangeException">Token의 요소가 없을 때 throw 합니다</exception>
    public void PushFirst(Transition state)
    {
        if (Count <= 0)
            throw new IndexOutOfRangeException("Token has no elements. Cannot push to first index");

        this[0].State = state;
    }

    /// <summary>
    /// 해당 인덱스의 State에 적용합니다
    /// </summary>
    /// <param name="index">Target Index</param>
    /// <param name="state">State</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void PushAt(int index, Transition state)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException($"Index must be between 0 and {_adapters.Length - 1} / current: {index}");

        this[index].State = state;
    }

    /// <summary>
    /// 첫번째 인덱스에 state를 안전하게 적용합니다
    /// </summary>
    /// <param name="state">State</param>
    public void PushFirstSafety(Transition state)
    {
        if (Count <= 0)
            return;

        this[0].State = state;
    }

    /// <summary>
    /// 해당 인덱스의 State에 안전하게 적용합니다
    /// </summary>
    /// <param name="index">Target Index</param>
    /// <param name="state">State</param>
    public void PushAtSafety(int index, Transition state)
    {
        if (index < 0 || Count <= 0 || index >= Count)
            return;

        this[index].State = state;
    }

    /// <summary>
    /// Token의 State들을 컬렉션을 통해 한 번에 적용합니다
    /// </summary>
    /// <param name="states">일괄 적용할 Transition 컬렉션</param>
    /// <exception cref="ArgumentNullException">states가 null일 때 발생합니다</exception>
    /// <exception cref="ArgumentException">states.Count와 Token의 Count가 불일치 시 throw 합니다</exception>
    public void PushAll(ICollection<Transition> states)
    {
        if (states == null)
            throw new ArgumentNullException($"TPEnumeratorToken.PushAll(): {nameof(states)} is null");

        if (states.Count != Count)
            throw new ArgumentException("PushAll: Token Count와 입력 States Count 불일치");

        int i = 0;
        foreach (Transition state in states)
        {
            state.ThrowIfTypeMismatch(this[i].Type);
            this[i].State = state;
            i++;
        }
    }

    /// <summary>
    /// Token의 State들을 컬렉션을 통해 한 번에 적용합니다. Null을 포함하면 해당 인덱스는 해당 타입의 Null Value가 적용됩니다
    /// </summary>
    /// <param name="states">일괄 적용할 Transition 컬렉션. null을 포함할 수 있습니다</param>
    /// <exception cref="ArgumentNullException">states가 null일 때 발생합니다</exception>
    /// <exception cref="ArgumentException">states.Count와 Token의 Count가 불일치 시 throw 합니다</exception>
    public void PushAllowingNull(ICollection<Transition?> states)
    {
        if (states == null)
            throw new ArgumentNullException($"TPEnumeratorToken.PushAllowingNull(): {nameof(states)} is null");

        if (states.Count != Count)
            throw new ArgumentException("PushAllowingNull: Token Count와 입력 States Count 불일치");

        int i = 0;
        foreach (Transition? state in states)
        {
            if (state == null)
            {
                this[i].State = this[i].Type.Null();
                i++;
                continue;
            }

            state.Value.ThrowIfTypeMismatch(this[i].Type);
            this[i].State = state.Value;
            i++;
        }
    }

    /// <summary>
    /// 해당 인덱스의 State에 Null을 적용합니다
    /// </summary>
    /// <param name="index">Target Index</param>
    /// <exception cref="IndexOutOfRangeException">Token의 Index 범위를 벗어났을 때 throw 합니다</exception>
    public void PushNullAt(int index)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException($"Index must be between 0 and {_adapters.Length - 1} / current: {index}");

        ITypeListenStateful target = this[index];
        target.State = target.Type.Null();
    }

    /// <summary>
    /// 해당 인덱스의 State에 Null을 적용합니다
    /// </summary>
    /// <param name="index">Target Index</param>
    public void PushNullAtSafety(int index)
    {
        if (index < 0 || Count <= 0 || index >= Count)
            return;

        ITypeListenStateful target = this[index];
        target.State = target.Type.Null();
    }

    /// <summary>
    /// 모든 인덱스에 Null을 적용합니다
    /// </summary>
    public void PushAllAsNull()
    {
        foreach (StatefulAdapter sf in _adapters)
        {
            sf.State = sf.Type.Null();
        }
    }

    /// <summary>
    /// 타입을 변경할 수 있는 IPolymorphicStateful 객체 배열을 반환합니다
    /// </summary>
    /// <returns>IPolymorphicStateful Array</returns>
    public IPolymorphicStateful[] GetPolymorphics()
    {
        return _adapters.Select(adapter => (IPolymorphicStateful)adapter).ToArray();
    }

    /// <summary>
    /// 해당 인덱스의 타입을 설정합니다
    /// </summary>
    /// <param name="index">Target Index</param>
    /// <param name="type">설정 타입</param>
    /// <exception cref="IndexOutOfRangeException">Token의 Index 범위를 벗어났을 때 throw 합니다</exception>
    public void SetType(int index, TransitionType type)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException($"Index must be between 0 and {_adapters.Length - 1} / current: {index}");

        _adapters[index].SetType(type);
    }

    /// <summary>
    /// 모든 인덱스의 타입을 설정합니다
    /// </summary>
    /// <param name="type">설정 타입</param>
    public void SetTypeAll(TransitionType type)
    {
        foreach (StatefulAdapter adapter in _adapters)
        {
            adapter.SetType(type);
        }
    }

    /// <summary>
    /// 모든 포트의 Name을 설정합니다
    /// </summary>
    /// <param name="names">설정 Name 리스트</param>
    public void SetNames(List<string> names)
    {
        if (names is null)
        {
            Debug.LogError($"{GetType().Name}: names is null");
            return;
        }

        if (names.Count != _adapters.Length)
        {
            Debug.LogError($"{GetType().Name}: names length is not match: names {names.Count} / adapters: {_adapters.Length}");
            return;
        }

        if (names.Count != names.Distinct().Count())
        {
            _isNameDuplicated = true;
            Debug.LogWarning($"{GetType().Name}: names contain duplicates. This token cannot use the Name indexer");
        }
        else
        {
            _isNameDuplicated = false;
        }

        for (int i = 0; i < _adapters.Length; i++)
            _adapters[i].Name = names[i];
    }
    #endregion

    #region Other Interface
    IEnumerator<ITypeListenStateful> IEnumerable<ITypeListenStateful>.GetEnumerator() => ((IEnumerable<ITypeListenStateful>)_adapters).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _adapters.GetEnumerator();

    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);

        foreach (StatefulAdapter adapter in _adapters)
        {
            adapter.Dispose();
        }

        _adapters = null;
    }
    #endregion

    #region Stateful Adapter
    private class StatefulAdapter : ITypeListenStateful, IPolymorphicStateful, INameable, IDisposable
    {
        private readonly ITransitionPoint _tp;

        public StatefulAdapter(ITransitionPoint tp)
        {
            _tp = tp;
            _tp.OnTypeChanged += InvokeOnTypeChanged;
            _tp.OnBeforeTypeChange += InvokeOnBeforeTypeChange;
        }

        public bool IsReadonly { get; set; }

        public Transition State
        {
            get => _tp.State;
            set
            {
                if (IsReadonly)
                {
                    Debug.LogWarning("This token is readonly");
                    return;
                }

                _tp.State = value;
            }
        }

        public TransitionType Type => _tp.Type;

        public event Action<TransitionType> OnTypeChanged;
        public event Action<TransitionType> OnBeforeTypeChange;

        public string Name
        {
            get => _tp.Name;
            set => _tp.Name = value;
        }

        public void SetType(TransitionType type) => _tp.SetType(type);

        public void Dispose()
        {
            if (_tp == null)
                return;

            _tp.OnTypeChanged -= InvokeOnTypeChanged;
            _tp.OnBeforeTypeChange -= InvokeOnBeforeTypeChange;
        }

        private void InvokeOnTypeChanged(TransitionType type)
        {
            OnTypeChanged?.Invoke(type);
        }

        private void InvokeOnBeforeTypeChange(TransitionType type)
        {
            OnBeforeTypeChange?.Invoke(type);
        }
    }

    bool IReadonlyToken.IsReadonly
    {
        get => _isReadonly;
        set
        {
            _isReadonly = value;

            foreach (StatefulAdapter adapter in _adapters)
            {
                adapter.IsReadonly = _isReadonly;
            }
        }
    }

    // Readonly Token 설정 가능
    public interface IReadonlyToken
    {
        bool IsReadonly { get; set; }
    }
    #endregion
}
