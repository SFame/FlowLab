using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public abstract class TPEnumerator : MonoBehaviour, ITPEnumerator
{
    #region Privates
    protected bool _hasSet = false;
    private GameObject _tp_Prefeb;
    private Node _node;
    private RectTransform _rect;
    private readonly Color _highlightColor = Color.green;
    private readonly Color _defaultColor = Color.white;

    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }
    #endregion

    #region On Inspector
    [SerializeField] protected GridLayoutGroup layout;
    #endregion

    #region Need Override
    protected abstract string PrefebPath { get; }
    #endregion
    
    #region Interface to child
    protected List<ITransitionPoint> TPs { get; private set; } = new();
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

    public void SetTPsConnection(ITransitionPoint[] targetTps, List<Vector2>[] vertices)
    {
        if (!(targetTps.Length == vertices.Length && vertices.Length == TPs.Count))
        {
            Debug.Log(@$"{GetType().Name}: 
            targetTps({targetTps.Length}), 
            vertices({vertices.Length}), 
            TPs({TPs.Count}) 
            Length dosen't match");
            return;
        }

        for (int i = 0; i < TPs.Count; i++)
        {
            if (TPs[i].Connection == null && targetTps[i] != null && vertices[i] != null) // 연결되어 있지 않을 때만 실행, targetTp가 있을 때만 실행
            {
                TPConnection newConnection = new() { LineEdges = vertices[i] };

                TPs[i].LinkTo(targetTps[i], newConnection);
            }
        }
    }

    /// <summary>
    /// Token Get
    /// </summary>
    /// <returns></returns>
    public abstract TPEnumeratorToken GetToken();


    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
    #endregion

    #region Wrapped interface
    public ITPEnumerator SetTPs(int count)
    {
        DestroyTPs();

        if (Node is null)
        {
            Debug.LogError("Must set TPEnumerator's Node info first");
            throw new Exception("Must set TPEnumerator's Node info first");
        }

        for (int i = 0; i < count; i++)
        {
            GameObject TPObj = InstantiateTP();
            if (TPObj is null)
                break;

            ITransitionPoint inOut = TPObj.GetComponent<ITransitionPoint>();
            inOut.Node = Node;

            TPObj.transform.SetParent(layout.transform);

            TPs.Add(inOut);
        }

        SizeUpdate();
        _hasSet = true;
        return this;
    }

    public ITPEnumerator SetTPSize(Vector2 value)
    {
        layout.cellSize = value;
        return this;
    }

    public ITPEnumerator SetTPsMargin(float value)
    {
        layout.spacing = new Vector2(0f, value);
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
            if (_tp_Prefeb is null)
            {
                _tp_Prefeb = Resources.Load<GameObject>(PrefebPath);
                if (_tp_Prefeb is null)
                {
                    Debug.LogError($"{GetType().Name}: Cannot find prefeb");
                    return null;
                }
            }
            return _tp_Prefeb;
        }
    }

    private void SizeUpdate()
    {
        int count = TPs.Count;
        
        float height = (layout.cellSize.y * count) + 
                       (layout.spacing.y * (count - 1)) + 
                       layout.padding.top + layout.padding.bottom;
        
        height = Mathf.Max(height, MinHeight);

        float width = layout.cellSize.x;
        
        Vector2 size = new Vector2(width, height);
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
            Destroy(tp.GameObject);  
        }
        TPs.Clear();
    }
    #endregion
}

/// <summary>
/// Node에서 참조할 TP객체들을 가져올 수 있는 토큰
/// </summary>
/// <typeparam name="TP"></typeparam>
public class TPEnumeratorToken : IEnumerable<ITransitionPoint>
{
    #region Privates
    private readonly ITransitionPoint[] _tpArray;
    #endregion

    public TPEnumeratorToken(IEnumerable<ITransitionPoint> tps, ITPEnumerator enumerator)
    {
        _tpArray = tps.ToArray();
        Enumerator = enumerator;
    }

    public ITPEnumerator Enumerator { get; private set; }
    public ITransitionPoint this[int index] => _tpArray[index];
    public ITransitionPoint this[string name] => _tpArray.FirstOrDefault(tp => tp.Name == name);
    public ITransitionPoint[] TPs => _tpArray;
    public int Count => _tpArray.Length;

    public void SetNames(List<string> names)
    {
        if (names is null)
        {
            Debug.LogError($"{GetType().Name}: names is null");
            return;
        }

        if (names.Count != _tpArray.Length)
        {
            Debug.LogError($"{GetType().Name}: names length is not match");
            return;
        }

        if (names.Count != names.Distinct().Count())
        {
            Debug.LogError($"{GetType().Name}: names contain duplicates");
            return;
        }

        for (int i = 0; i < _tpArray.Length; i++)
            _tpArray[i].Name = names[i];
    }

    public IEnumerator<ITransitionPoint> GetEnumerator() => ((IEnumerable<ITransitionPoint>)_tpArray).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _tpArray.GetEnumerator();
}