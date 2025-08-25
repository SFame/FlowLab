using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

public abstract class TransitionPoint : MonoBehaviour, ITransitionPoint, IPointerEnterHandler, IMoveable, 
                                        IGameObject, IPointerExitHandler, IPointerClickHandler, ISortingPositionGettable
{
    #region On Inspacetor
    [Header("Options")]
    [SerializeField] private bool m_MultiType = false;

    [SerializeField] private AnimationCurve m_RadialBlinkCurve;
    [SerializeField] private float m_RadialBlinkDuration = 0.4f;

    [Space(10)]

    [Header("Colors")]
    [SerializeField] protected Color m_HighlightedColor = Color.green;
    [SerializeField] protected Color m_StateActiveColor = Color.red;
    [SerializeField] protected Color m_DefaultColor = Color.black;


    [Space(10)]

    [Header("Components")]
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private Image m_Image;
    [SerializeField] private Image m_TypeRingImage;
    [SerializeField] private Image m_RadialShadowImage;
    [SerializeField] private Image m_HighlighterImage;
    #endregion

    #region Privates
    private RectTransform _imageRect;
    private string _name;
    private Node _node;


    private void OnDestroy()
    {
        OnGettableRemove?.Invoke();
        Connection?.Dispose();
        _radialCts.CancelAndDispose();
        _stateDisplayCts.CancelAndDispose();
    }
    #endregion

    #region Component
    protected Image Image => m_Image;
    protected RectTransform ImageRect
    {
        get
        {
            _imageRect ??= m_Image?.GetComponent<RectTransform>();
            return _imageRect;
        }
    }
    #endregion

    #region Interface
    public abstract Transition State { get; set; }
    public abstract TransitionType Type { get; protected set; }
    public int Index { get; set; }
    public TPConnection Connection { get; set; }
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (m_NameText is null)
            {
                Debug.LogError($"{GetType().Name}: m_NameText is null");
                return;
            }
            m_NameText.text = value;
        }
    }
    public bool BlockConnect { get; set; }
    public GameObject GameObject => gameObject;

    public Vector2 WorldPosition
    {
        get
        {
            if (ImageRect == null)
            {
                Debug.LogError("TransitionPoint: ImageRect not found");
                return Vector2.zero;
            }

            return ImageRect.position;
        }
    }

    public Vector2 LocalPosition
    {
        get
        {
            if (Node == null || Node.Support == null)
            {
                Debug.LogError("TransitionPoint: Node not found");
                return Vector2.zero;
            }

            return RectTransformUtils.ConvertWorldToLocalPosition(WorldPosition, Node.Support.Rect);
        }
    }

    public event Action<TransitionType> OnTypeChanged;
    public event Action<TransitionType> OnBeforeTypeChange;

    public void SetType(TransitionType type)
    {
        if (type == Type)
            return;

        OnBeforeTypeChange?.Invoke(type);
        Type = type;
        OnTypeChanged?.Invoke(Type);
    }

    public abstract void LinkTo(ITransitionPoint targetTp, TPConnection connection = null);
    public abstract void AcceptLink(TPConnection connection);
    public abstract void ClearConnection();
    #endregion

    #region Use in child
    public Node Node
    {
        get => _node;
        set => _node ??= value;
    }

    public Action<PositionInfo> OnMove { get; set; }

    protected virtual List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> context = new() { new ContextElement(clickAction: () => Connection?.Disconnect(), text: "Disconnect") };

            if (m_MultiType)
            {
                context.Add(new ContextElement(clickAction: () =>
                {
                    SetType(TransitionType.Bool);
                    Node.ReportChanges();
                }, text: $"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>"));

                context.Add(new ContextElement(clickAction: () =>
                {
                    SetType(TransitionType.Int);
                    Node.ReportChanges();
                }, text: $"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>"));

                context.Add(new ContextElement(clickAction: () =>
                {
                    SetType(TransitionType.Float);
                    Node.ReportChanges();
                }, text: $"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>"));
                context.Add(new ContextElement(clickAction: () =>
                {
                    SetType(TransitionType.String);
                    Node.ReportChanges();
                }, text: $"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>"));
                context.Add(new ContextElement(clickAction: () =>
                {
                    SetType(TransitionType.Pulse);
                    Node.ReportChanges();
                }, text: $"Type: <color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color>"));
            }

            return context;
        }
    }

    protected void SetImageColor(Color color)
    {
        Image.color = color;
    }

    protected void SetTypeRingColor(Color color)
    {
        m_TypeRingImage.color = color;
    }

    protected void SetTextColor(Color color)
    {
        m_NameText.color = color;
    }
    #endregion

    #region MouseEvent
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        m_HighlighterImage.color = m_HighlightedColor;
        HoverStart();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        Color hColor = m_HighlightedColor;
        hColor.a = 0f;
        m_HighlighterImage.color = hColor;
        HoverEnd();
    }
    
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            Utils.ContextMenuManager.ShowContextMenu(PUMPUiManager.RootCanvas, eventData.position, ContextElements.ToArray());
    }
    #endregion

    #region RadialShadow

    private Color _radialDefaultColor;
    private bool _radialInitialized = false;
    private SafetyCancellationTokenSource _radialCts = new();

    private void ShadowInitialize()
    {
        if (_radialInitialized)
            return;

        _radialInitialized = true;

        _radialDefaultColor = Type.GetColor();
        _radialDefaultColor.a = 0f;
        m_RadialShadowImage.color = _radialDefaultColor;
    }

    protected void RadialDefaultColorUpdate(Color color)
    {
        _radialDefaultColor = color;
        _radialDefaultColor.a = 0f;
        m_RadialShadowImage.color = new Color(_radialDefaultColor.r, _radialDefaultColor.g, _radialDefaultColor.b, m_RadialShadowImage.color.a);
    }

    private void BlinkRadial()
    {
        ShadowInitialize();

        _radialCts = _radialCts.CancelAndDisposeAndGetNew();

        m_RadialBlinkCurve.CurveAction
        (
            m_RadialBlinkDuration,
            lerp =>
            {
                Color currentColor = _radialDefaultColor;
                currentColor.a = lerp;
                m_RadialShadowImage.color = currentColor;
            },
            null,
            _radialCts.Token
        ).Forget();
    }

    private void ActiveRadial(bool isActive)
    {
        ShadowInitialize();

        _radialCts.CancelAndDispose();

        Color currentColor = isActive
            ? new Color(_radialDefaultColor.r, _radialDefaultColor.g, _radialDefaultColor.b, 1f)
            : new Color(_radialDefaultColor.r, _radialDefaultColor.g, _radialDefaultColor.b, 0f);

        m_RadialShadowImage.color = currentColor;
    }

    protected void ShowRadial(Transition state)
    {
        if (state.Type == TransitionType.Bool)
        {
            if (state.IsNull)
            {
                ActiveRadial(false);
                return;
            }

            ActiveRadial(state);
            return;
        }

        BlinkRadial();
    }
    #endregion

    #region StateDisplay
    private SafetyCancellationTokenSource _stateDisplayCts = new();
    private readonly float _mouseMoveThreshold = 0.5f;
    private readonly float _mouseHoverDelay = 0.5f;
    private Vector2 _mouseLastPosition;

    private void HoverStart()
    {
        _stateDisplayCts = _stateDisplayCts.CancelAndDisposeAndGetNew();
        CheckMouseMove(_stateDisplayCts.Token).Forget();
    }

    private void HoverEnd()
    {
        _stateDisplayCts.CancelAndDispose();
        StateDisplay.Clear();
    }

    private async UniTaskVoid CheckMouseMove(CancellationToken token)
    {
        _mouseLastPosition = Input.mousePosition;
        float currentHoverDelay = 0f;
        bool isShow = false;
        Transition stateCache = Transition.False;
        try
        {
            while (!token.IsCancellationRequested)
            {
                Vector2 currentPosition = Input.mousePosition;
                float sqrDistance = (_mouseLastPosition - currentPosition).sqrMagnitude;
                _mouseLastPosition = currentPosition;

                if (sqrDistance < _mouseMoveThreshold)
                {
                    if (isShow)
                    {
                        if (stateCache != State)
                        {
                            stateCache = State;
                            StateDisplay.Update(stateCache);
                        }
                        await UniTask.Yield(token);
                        continue;
                    }

                    if (currentHoverDelay > _mouseHoverDelay)
                    {
                        isShow = true;
                        stateCache = State;
                        StateDisplay.Render(stateCache, MainCameraGetter.GetMainCam().WorldToScreenPoint(WorldPosition), PUMPUiManager.RootCanvas);
                    }
                    else
                    {
                        currentHoverDelay += Time.deltaTime;
                    }
                }
                else
                {
                    currentHoverDelay = 0f;

                    if (isShow)
                    {
                        StateDisplay.Clear();
                        isShow = false;
                    }
                }

                await UniTask.Yield(token);
            }
        }
        catch (OperationCanceledException) { }
    }
    #endregion

    #region EdgeSorting
    public event Action OnGettableRemove;

    public bool IsActive => gameObject.activeInHierarchy;

    public Vector2 GetPosition()
    {
        return WorldPosition;
    }
    #endregion
}