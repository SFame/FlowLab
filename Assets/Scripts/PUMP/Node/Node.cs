using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ResourceGetter("PUMP/Sprite/ingame/null_node")]
public abstract class Node : DraggableUGUI, IPointerClickHandler, IDragSelectable, ILocatable, IHighlightable
{
    #region Privates
    private bool _initialized = false;
    private Image _image;
    private TextMeshProUGUI _nodeNameText;
    private PUMPBackground _background;
    private bool _isBackgroundSet = false;
    private Canvas _rootCanvas;
    private readonly Color _highlightedColor = Color.green;

    private float _inEnumHeight;
    private float _outEnumHeight;

    private bool _inEnumActive = true;
    private bool _outEnumActive = true;
    #endregion

    #region Component
    protected Image Image
    {
        get
        {
            if (_image is null)
                _image = GetComponent<Image>();

            return _image;
        }
    }

    protected TextMeshProUGUI NodeNameText
    {
        get
        {
            if ( _nodeNameText is null)
                _nodeNameText = GetComponentInChildren<TextMeshProUGUI>();

            return _nodeNameText;
        }
    }

    protected Canvas RootCanvas
    {
        get
        {
            if (_rootCanvas is null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            return _rootCanvas;
        }
    }
    #endregion

    #region Interface
    public PUMPBackground Background
    {
        get
        {
            if (!_isBackgroundSet || _background == null)
                Debug.LogError("Set Node field 'Background' in PUMPBackground");
            return _background;
        }

        set
        {
            if (value == null)
            {
                Debug.LogError("Background is null");
                return;
            }
            _background = value;
            _isBackgroundSet = true;
        }
    }

    public Vector2 Location => Rect.position;

    /// <summary>
    /// 직렬화 시 TP의 연결정보 Get
    /// </summary>
    /// <returns></returns>
    public TPConnectionInfo GetTPConnectionInfo()
    {
        if (!_initialized)
        {
            Debug.LogError($"{GetType().Name}: Required call Initialize()");
            return null;
        }

        return new TPConnectionInfo(InputToken.TPs, OutputToken.TPs);
    }

    /// <summary>
    /// 역 직렬화 시 TP의 연결정보 Set
    /// </summary>
    /// <param name="connectionInfo"></param>
    public void SetTPConnectionInfo(TPConnectionInfo connectionInfo)
    {
        if (!_initialized)
        {
            Debug.LogError($"{GetType().Name}: Required call Initialize()");
            return;
        }

        InputToken.Enumerator.SetTPsConnection(connectionInfo.InConnectionTargets, connectionInfo.InVertices);
        OutputToken.Enumerator.SetTPsConnection(connectionInfo.OutConnectionTargets, connectionInfo.OutVertices);
    }

    public (ITransitionPoint[] inTps, ITransitionPoint[] outTps) GetTPs()
    {
        return (InputToken.TPs, OutputToken.TPs);
    }

    public int GetTPIndex(ITransitionPoint findTp)
    {
        for (int i = 0; i < InputToken.Count; i++)
        {
            if (InputToken[i] == findTp)
                return i;
        }
        
        for (int i = 0; i < OutputToken.Count; i++)
        {
            if (OutputToken[i] == findTp)
                return i;
        }

        return -1;
    }

    public void CallCompletePlacementFromPalette()
    {
        OnCompletePlacementFromPalette();
        ReportChanges();
    }

    public event Action<Node> OnDestroy;

    public void Destroy()
    {
        Disconnect();
        OnDestroy?.Invoke(this);
        Destroy(gameObject);
    }

    public void Disconnect()
    {
        SelectRemoveRequest?.Invoke();
        foreach (ITransitionPoint tp in InputToken)
            tp.Connection?.Disconnect();

        foreach (ITransitionPoint tp in OutputToken)
            tp.Connection?.Disconnect();
    }
    
    public void SetHighlight(bool highlighted)
    {
        Image.color = highlighted ? _highlightedColor : DefaultColor;
    }
    #endregion

    #region IO
    protected TPEnumeratorToken InputToken { get; set; } // InputToken[n].State를 set 하지 말 것.
    protected TPEnumeratorToken OutputToken { get; set; }
    #endregion

    #region Overriding required
    protected virtual void OnLoad_BeforeStateUpdate() { }
    protected virtual void OnLoad_AfterStateUpdate() { }
    protected virtual void OnCompletePlacementFromPalette() { }

    protected virtual List<ContextElement> ContextElements
    {
        get => new List<ContextElement>()
        {
            new ContextElement(
                clickAction: () =>
                {
                    Destroy();
                    ReportChanges();
                }, 
                text: "Remove"),
            
            new ContextElement(
                clickAction: () =>
                {
                    Disconnect();
                    ReportChanges();
                }, 
                text: "Disconnect"),
        };
    }
    
    public virtual string NodePrefebPath { get; }
    protected virtual string TP_EnumInPrefebPath { get; } = "PUMP/Prefab/TP/TPEnumIn";
    protected virtual string TP_EnumOutPrefebPath { get; } = "PUMP/Prefab/TP/TPEnumOut";

    protected virtual Color DefaultColor { get; set; } = Color.white;
    protected virtual float TextSize { get; } = 30f;

    protected abstract string SpritePath { get; }
    protected abstract string NodeDisplayName { get; }
    protected abstract List<string> InputNames { get; }
    protected abstract List<string> OutputNames { get; }

    protected abstract float InEnumeratorXPos { get; }
    protected abstract float OutEnumeratorXPos { get; }

    protected abstract float EnumeratorTPMargin { get; }
    protected abstract Vector2 EnumeratorTPSize { get; }
    protected abstract Vector2 DefaultNodeSize { get; }
    protected virtual bool SizeFreeze { get; } = false;
    #endregion

    #region Can use in child
    protected bool InEnumActive
    {
        get => _inEnumActive;
        set
        {
            InputToken?.Enumerator?.SetActive(value);
            _inEnumActive = value;
        }
    }

    protected bool OutEnumActive
    {
        get => _outEnumActive;
        set
        {
            OutputToken?.Enumerator?.SetActive(value);
            _outEnumActive = value;
        }
    }
    
    protected void ResetToken(bool stateUpdate = true)
    {
        if (InputToken?.Enumerator == null || OutputToken?.Enumerator == null)
        {
            Debug.LogError($"{GetType().Name}: Token(or Token's Enumerator) is null");
            return;
        }
        
        SetToken(InputToken.Enumerator, OutputToken.Enumerator);

        if (stateUpdate)
            StateUpdate();
    }

    /// <summary>
    /// Selected 관리자에게 선택 객체들 해제 요청
    /// </summary>
    protected void SelectedRemoveRequestInvoke()
    {
        SelectRemoveRequest?.Invoke();
    }

    /// <summary>
    /// 변경사항이 있다면 호출
    /// PUMPBackground.SetSerializeNodeInfos() 메서드의 트랜지션에 영향받는 위치에서 "절대" 호출하지 말 것.
    /// </summary>
    public void ReportChanges()
    {
        ((IChangeObserver)Background)?.ReportChanges();
    }
    #endregion

    #region In TP State Update Callback (Overriding required)
    protected abstract void StateUpdate(TransitionEventArgs args = null);
    #endregion

    #region Initialize
    public void Initialize()
    {
        OnDragging += pointerEventArgs => OnSelectedMove?.Invoke(this, pointerEventArgs.delta);
        MoveEnd += _ => ReportChanges();
        SetName();
        SetSprite();
        SetRect();
        SetTPEnumerator();
        HeightSynchronizationWithEnum();
        OnLoad_BeforeStateUpdate();
        StateUpdate();
        OnLoad_AfterStateUpdate();
        _initialized = true;
    }

    private void SetSprite()
    {
        Sprite sprite = Resources.Load<Sprite>(SpritePath);
        if (sprite is null)
        {
            Debug.LogError($"{GetType().Name}: Can't find resource <Sprite>");
            return;
        }

        Image.sprite = sprite;
    }

    private void SetName()
    {
        name = GetType().Name;
        NodeNameText.text = NodeDisplayName;
        NodeNameText.fontSize = TextSize;
    }

    private void SetRect()
    {
        Rect.sizeDelta = DefaultNodeSize;
    }

    /// <summary>
    /// TransitionPoint 관련 설정
    /// </summary>
    private void SetTPEnumerator()
    {
        var tuple = GetTPEnumResources();
        if (tuple is null)
            return;

        RectTransform inRect = tuple.Value.enumIn.GetComponent<RectTransform>();
        RectTransform outRect = tuple.Value.enumOut.GetComponent<RectTransform>();

        SetParent(Rect, inRect, outRect);

        SetAnchor(rect: inRect, min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));
        SetAnchor(rect: outRect, min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));

        SetOffset(rect: inRect, min: new Vector2(inRect.offsetMin.x, 0f), max: new Vector2(inRect.offsetMax.x, 0f));
        SetOffset(rect: outRect, min: new Vector2(outRect.offsetMin.x, 0f), max: new Vector2(outRect.offsetMax.x, 0f));

        SetXPos(rect: inRect, value: InEnumeratorXPos);
        SetXPos(rect: outRect, value: OutEnumeratorXPos);

        ITPEnumerator tpEnumIn = inRect.GetComponent<ITPEnumerator>();
        ITPEnumerator tpEnumOut = outRect.GetComponent<ITPEnumerator>();
        
        tpEnumIn.MinHeight = DefaultNodeSize.y;
        tpEnumOut.MinHeight = DefaultNodeSize.y;


        if (!SizeFreeze)
        {
            tpEnumIn.OnSizeUpdatedWhenTPChange += size =>
            {
                _inEnumHeight = size.y;
                float maxValue = HeightSynchronizationWithEnum();
                tpEnumIn.SetHeight(maxValue);
                tpEnumOut.SetHeight(maxValue);
            };

            tpEnumOut.OnSizeUpdatedWhenTPChange += size =>
            {
                _outEnumHeight = size.y;
                float maxValue = HeightSynchronizationWithEnum();
                tpEnumIn.SetHeight(maxValue);
                tpEnumOut.SetHeight(maxValue);
            }; 
        }

        tpEnumIn.Node = this;
        tpEnumOut.Node = this;
        
        SetToken(tpEnumIn, tpEnumOut);
    }

    /// <summary>
    /// 처음 토큰 설정
    /// </summary>
    private void SetToken(ITPEnumerator enumIn, ITPEnumerator enumOut)
    {
        if (enumIn == null || enumOut == null)
        {
            Debug.LogError($"{GetType().Name}.SetToken(): enum is null");
            return;
        }
        
        InputToken = SetTPEnumSize(enumIn)
            .SetTPs(InputNames.Count)
            .GetToken();

        OutputToken = SetTPEnumSize(enumOut)
            .SetTPs(OutputNames.Count)
            .GetToken();
        
        if (InputToken is null || OutputToken is null)
        {
            Debug.LogError("Token casting fail");
            return;
        }
        
        InputToken.SetNames(InputNames);
        OutputToken.SetNames(OutputNames);

        SubscribeTPInStateUpdateEvent();
    }

    /// <summary>
    /// Resources에서 TPEnumerator 프리펩 가져옴
    /// </summary>
    /// <returns></returns>
    private (GameObject enumIn, GameObject enumOut)? GetTPEnumResources()
    {
        GameObject enumIn = Resources.Load<GameObject>(TP_EnumInPrefebPath);
        GameObject enumOut = Resources.Load<GameObject>(TP_EnumOutPrefebPath);

        if (enumIn is null || enumOut is null)
        {
            Debug.LogError($"{GetType().Name}: TPEnumResources can't find");
            return null;
        }

        return (Instantiate(enumIn), Instantiate(enumOut));
    }

    private void SetParent(Transform parent, params Transform[] childs)
    {
        foreach (Transform child in childs)
            child.SetParent(parent);
    }

    private void SetAnchor(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
    }

    private void SetOffset(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private void SetXPos(RectTransform rect, float value)
    {
        Vector2 position = rect.anchoredPosition;
        position.x = value;
        rect.anchoredPosition = position;
    }

    private float HeightSynchronizationWithEnum()
    {
        float maxHeight = Mathf.Max(_inEnumHeight, _outEnumHeight, DefaultNodeSize.y);
        Rect.sizeDelta = new Vector2(DefaultNodeSize.x, maxHeight);
        return maxHeight;
    }

    private ITPEnumerator SetTPEnumSize(ITPEnumerator tpEnum) => tpEnum.SetTPsMargin(EnumeratorTPMargin).SetTPSize(EnumeratorTPSize);

    /// <summary>
    /// TP In의 변경 시의 이벤트 구독
    /// </summary>
    private void SubscribeTPInStateUpdateEvent()
    {
        if (InputToken is null)
        {
            Debug.LogError($"{GetType().Name}: InputToken in null");
            return;
        }

        foreach (ITransitionPoint tp in InputToken)
        {
            if (tp is ITPIn tpIn)
                tpIn.OnStateChange += StateUpdate;
        }
    }
    #endregion

    #region  Other (ToString()..)
    public override string ToString() => $"Type: {GetType().Name}, DisplayName: {NodeDisplayName}";
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = _selectedContextElements?.Invoke() ?? ContextElements;
            Utils.ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }
    #endregion

    #region Selecting handler
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetHighlight(value);
            _isSelected = value;
        }
    }
    private bool _isSelected = false;

    public object SelectingTag
    {
        get => _selectedContextElements;
        set => _selectedContextElements = value as Func<List<ContextElement>>;
    }

    private Func<List<ContextElement>> _selectedContextElements;
    
    public void MoveSelected(Vector2 direction) => MovePosition(direction);
    
    public event OnSelectedMoveHandler OnSelectedMove;
    public event Action SelectRemoveRequest;

    private void OnDisable()
    {
        SelectRemoveRequest?.Invoke();
        IsSelected = false;
    }
    #endregion
}

/// <summary>
/// 직렬화용
/// </summary>
public class TPConnectionInfo
{
    /// <summary>
    /// 이 노드의 In에 연결된 Out들
    /// </summary>
    public ITransitionPoint[] InConnectionTargets { get; private set; }
    public List<Vector2>[] InVertices { get; private set; }

    /// <summary>
    /// 이 노드의 Out에 연결된 In들
    /// </summary>
    public ITransitionPoint[] OutConnectionTargets { get; private set; }
    public List<Vector2>[] OutVertices { get; private set; }

    public TPConnectionInfo(ITransitionPoint[] inConnections, ITransitionPoint[] outConnections)
    {
        SetConnectionTarget(inConnections, outConnections);
        SetVertices();
    }

    public TPConnectionInfo(ITransitionPoint[] inConnectionTargets, ITransitionPoint[] outConnectionTargets,
        List<Vector2>[] inVertices, List<Vector2>[] outVertices)
    {
        InConnectionTargets = inConnectionTargets;
        InVertices = inVertices;
        OutConnectionTargets = outConnectionTargets;
        OutVertices = outVertices;
    }

    #region Privates
    /// <summary>
    /// 연결 타겟 정보
    /// </summary>
    /// <param name="inConnections"></param>
    /// <param name="outConnections"></param>
    private void SetConnectionTarget(ITransitionPoint[] inConnections, ITransitionPoint[] outConnections)
    {
        InConnectionTargets = inConnections.Select(inConnection => inConnection?.Connection?.SourceState).ToArray();
        OutConnectionTargets = outConnections.Select(outConnection => outConnection?.Connection?.TargetState).ToArray();
    }
    
    /// <summary>
    /// 선분 정보
    /// </summary>
    private void SetVertices()
    {
        InVertices = new List<Vector2>[InConnectionTargets.Length];
        OutVertices = new List<Vector2>[OutConnectionTargets.Length];

        for (int i = 0; i < InConnectionTargets.Length; i++)
            InVertices[i] = InConnectionTargets[i]?.Connection?.LineConnector.GetVertices();

        for (int i = 0;i < OutConnectionTargets.Length; i++)
            OutVertices[i] = OutConnectionTargets[i]?.Connection?.LineConnector.GetVertices();
    }
    #endregion

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"In TP ({InConnectionTargets?.Length ?? 0}):");
        if (InConnectionTargets != null)
        {
            for (int i = 0; i < InConnectionTargets.Length; i++)
            {
                sb.AppendLine($"  [{i}] Connection: {InConnectionTargets[i]?.Name ?? "null"}");
                if (InVertices?[i] != null)
                {
                    sb.AppendLine($"    Vertices:");
                    foreach (var vertex in InVertices[i])
                        sb.AppendLine($"      {vertex}");
                }
                else
                    sb.AppendLine("    Vertices: null");
            }
        }

        sb.AppendLine("\n");

        sb.AppendLine($"Out TP ({OutConnectionTargets?.Length ?? 0}):");
        if (OutConnectionTargets != null)
        {
            for (int i = 0; i < OutConnectionTargets.Length; i++)
            {
                sb.AppendLine($"  [{i}] Connection: {OutConnectionTargets[i]?.Name ?? "null"}");
                if (OutVertices?[i] != null)
                {
                    sb.AppendLine($"    Vertices:");
                    foreach (var vertex in OutVertices[i])
                        sb.AppendLine($"      {vertex}");
                }
                else
                    sb.AppendLine("    Vertices: null");
            }
        }

        return sb.ToString();
    }
}