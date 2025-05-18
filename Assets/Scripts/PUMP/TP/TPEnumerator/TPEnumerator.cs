using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TPEnumeratorToken;

public class TPEnumerator : MonoBehaviour, ITPEnumerator
{
    #region On Inspactor
    [SerializeField] private GameObject m_TpPrefab;
    [SerializeField] protected GridLayoutGroup m_Layout;
    #endregion

    #region Privates
    protected bool _hasSet = false;
    private Node _node;
    private RectTransform _rect;
    private float _margin;

    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }
    #endregion
    
    #region Interface to child
    protected List<ITransitionPoint> TPs { get; } = new();
    #endregion

    #region Interface
    public event Action<Vector2> OnSizeUpdatedWhenTPChange;
    
    public float MinHeight { get; set; }
    
    public Node Node
    {
        get => _node;
        set
        {
            if (_node is null)
                _node = value;
        }
    }

    public void SetTPConnections(ITransitionPoint[] targetTps, List<Vector2>[] vertices, DeserializationCompleteReceiver completeReceiver)
    {
        if (!(targetTps.Length == vertices.Length && vertices.Length == TPs.Count))
        {
            Debug.Log($"{name}: 직렬화 정보와 불일치: data: {targetTps.Length} / TP: {TPs.Count}");

            foreach (ITransitionPoint tp in TPs)
            {
                tp.Connection?.Disconnect();
                tp.BlockConnect = true;
            }

            // subscribe (no action)
            completeReceiver.Subscribe(() =>
            {
                foreach (ITransitionPoint tp in TPs)
                {
                    if (tp != null)
                    {
                        tp.BlockConnect = false;
                    }
                }
            });
            // ---------------------

            return;
        }

        for (int i = 0; i < TPs.Count; i++)
        {
            if (TPs[i].Connection == null && targetTps[i] != null && vertices[i] != null) // 연결되어 있지 않을 때만 실행, targetTp가 있을 때만 실행
            {
                TPConnection newConnection = new() { LineEdges = vertices[i] };

                newConnection.DisableFlush = true;
                TPs[i].LinkTo(targetTps[i], newConnection);
                newConnection.DisableFlush = false;
            }
        }
    }

    /// <summary>
    /// Token Get
    /// </summary>
    /// <returns></returns>
    public TPEnumeratorToken GetToken()
    {
        if (!_hasSet)
        {
            Debug.LogError("Require TPEnum set first");
            return null;
        }

        return new TPEnumeratorToken(TPs);
    }

    public ITransitionPoint[] GetTPs()
    {
        return TPs.ToArray();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
    #endregion

    #region Wrapped interface
    public ITPEnumerator SetTPs(TransitionType[] types)
    {
        DestroyTPs();

        if (Node is null)
        {
            throw new InvalidOperationException("Must set TPEnumerator's Node info first");
        }

        if (types is null)
        {
            throw new ArgumentNullException("'types' Cannot be null");
        }

        for (int i = 0; i < types.Length; i++)
        {
            GameObject TPObj = InstantiateTP();
            if (TPObj is null)
                break;

            ITransitionPoint tp = TPObj.GetComponent<ITransitionPoint>();
            tp.SetType(types[i]);
            tp.Node = Node;
            tp.Index = i;

            TPObj.transform.SetParent(m_Layout.transform);

            TPs.Add(tp);
        }

        SizeUpdate();
        _hasSet = true;
        return this;
    }

    public ITPEnumerator SetTPSize(Vector2 value)
    {
        m_Layout.cellSize = value;
        return this;
    }

    public ITPEnumerator SetPadding(float value)
    {
        m_Layout.spacing = new Vector2(0f, value);
        return this;
    }

    public ITPEnumerator SetMargin(float value)
    {
        _margin = value;
        return this;
    }

    public ITPEnumerator SetHeight(float value)
    {
        Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, value);
        SetHightZeroWhenNonTP();
        return this;
    }
    #endregion

    #region Non Interface
    private GameObject TP_Prefeb
    {
        get
        {
            if (m_TpPrefab is null)
            {
                Debug.LogError($"{name}: Can't find TP Prefab");
            }
            return m_TpPrefab;
        }
    }

    private void SizeUpdate()
    {
        int count = TPs.Count;
        
        float height = (m_Layout.cellSize.y * count) + 
                       (m_Layout.spacing.y * (count - 1)) + 
                       m_Layout.padding.top + m_Layout.padding.bottom;

        height = Mathf.Max(height, MinHeight);

        float marginValue = count == 0 ? 0f : _margin * 2f;
        float width = m_Layout.cellSize.x;
        
        Vector2 size = new Vector2(width, height + marginValue);
        Rect.sizeDelta = size;
        SetHightZeroWhenNonTP();
        OnSizeUpdatedWhenTPChange?.Invoke(size);
    }

    private void SetHightZeroWhenNonTP()
    {
        if (TPs.Count == 0)
            Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, 0f);
    }

    private GameObject InstantiateTP()
    {
        if (TP_Prefeb is null)
            return null;

        return Instantiate(TP_Prefeb);
    }

    private void DestroyTPs()
    {
        foreach (ITransitionPoint tp in TPs)
        {
            tp.Connection?.Disconnect();
            if (tp is IGameObject gameObject)
            {
                Destroy(gameObject.GameObject);
            }
        }
        TPs.Clear();
    }
    #endregion
}

/// <summary>
/// Node에서 참조할 TP객체들을 가져올 수 있는 토큰
/// </summary>
/// <typeparam name="TP"></typeparam>
public class TPEnumeratorToken : IEnumerable<ITypeListenStateful>, IReadonlyToken, IDisposable
{
    #region Privates
    private StatefulAdapter[] _adapters;
    private bool _isReadonly = false;
    private bool _isNameDuplicated = true;
    private bool _disposed = false;
    #endregion

    public TPEnumeratorToken(IEnumerable<ITransitionPoint> tps)
    {
        _adapters = tps.Select(tp => new StatefulAdapter(tp)).ToArray();
    }

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

    public void ApplyStatesAll(ICollection<Transition> states)
    {
        if (states.Count != Count)
            throw new ArgumentException("ApplyStatesAll: Token Count와 입력 States Count 불일치");

        int i = 0;
        foreach (Transition state in states)
        {
            state.ThrowIfTypeMismatch(this[i].Type);
            this[i].State = state;
            i++;
        }
    }

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

    #region Other Interface
    IEnumerator<ITypeListenStateful> IEnumerable<ITypeListenStateful>.GetEnumerator() => ((IEnumerable<ITypeListenStateful>)_adapters).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _adapters.GetEnumerator();

    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (StatefulAdapter adapter in _adapters)
        {
            adapter.Dispose();
        }

        _adapters = null;
    }
    #endregion

    #region Stateful Adapter
    private class StatefulAdapter : ITypeListenStateful, INameable, IDisposable
    {
        private readonly ITransitionPoint _tp;
        private Action<TransitionType> _onTypeChanged;

        public StatefulAdapter(ITransitionPoint tp)
        {
            _tp = tp;
            _tp.OnTypeChanged += InvokeOnTypeChanged;
        }

        public bool IsReadonly { get; set; }

        public Transition State
        {
            get => _tp.State;
            set
            {
                if (IsReadonly)
                {
                    Debug.LogWarning("This token cannot be set");
                    return;
                }

                _tp.State = value;
            }
        }

        public TransitionType Type => _tp.Type;

        public event Action<TransitionType> OnTypeChanged
        {
            add => _onTypeChanged += value;
            remove => _onTypeChanged -= value;
        }

        public string Name
        {
            get => _tp.Name;
            set => _tp.Name = value;
        }

        public void Dispose()
        {
            if (_tp == null)
                return;

            _tp.OnTypeChanged -= InvokeOnTypeChanged;
        }

        private void InvokeOnTypeChanged(TransitionType type)
        {
            _onTypeChanged?.Invoke(type);
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

    // 이 토큰은 읽기 전용인지 설정 가능
    public interface IReadonlyToken
    {
        bool IsReadonly { get; set; }
    }
    #endregion
}