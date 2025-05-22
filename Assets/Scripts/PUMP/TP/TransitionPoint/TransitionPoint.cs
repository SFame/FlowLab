using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;

public abstract class TransitionPoint : MonoBehaviour, ITransitionPoint, IPointerEnterHandler, IMoveable, 
                                        IGameObject, IPointerExitHandler, IPointerClickHandler
{
    #region On Inspacetor
    [SerializeField] private bool m_MultiType = false;

    [Space(10)]

    [SerializeField] protected Color m_HighlightedColor = Color.green;
    [SerializeField] protected Color m_StateActiveColor = Color.red;
    [SerializeField] protected Color m_DefaultColor = Color.black;
    #endregion

    [Space(10)]

    #region On Inspector Component
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private Image m_Image;
    #endregion

    #region Privates
    private RectTransform _imageRect;
    private string _name;
    private Node _node;
    private Canvas _rootCanvas;


    private void OnDestroy()
    {
        Connection?.Dispose();
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
    protected Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= GetComponentInParent<Canvas>().rootCanvas;
            return _rootCanvas;
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
            }

            return context;
        }
    }

    protected void SetImageColor(Color color)
    {
        Image.color = color;
    }

    protected void SetTextColor(Color color)
    {
        m_NameText.color = color;
    }
    #endregion

    #region MouseEvent
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        SetImageColor(m_HighlightedColor);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        SetImageColor(((IStateful)this).IsActivateState() ? m_StateActiveColor : m_DefaultColor);
    }
    
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            Utils.ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, ContextElements.ToArray());
    }
    #endregion
}