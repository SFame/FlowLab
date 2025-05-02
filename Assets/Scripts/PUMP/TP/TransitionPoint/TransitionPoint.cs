using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class TransitionPoint : MonoBehaviour, 
                                        ITransitionPoint, IPointerEnterHandler, IMoveable, IGameObject,
                                        IPointerExitHandler, IPointerClickHandler
{
    #region Privates
    private RectTransform _imageRect;
    private string _name;
    private Node _node;
    private Canvas _rootCanvas;
    protected readonly Color _highlightedColor = Color.green;
    protected readonly Color _activeColor = Color.red;
    protected readonly Color _defaultColor = Color.black;

    private void OnDestroy()
    {
        Connection?.Dispose();
    }
    #endregion

    #region On Inspector Component
    [SerializeField]
    private TextMeshProUGUI _nameText;
    [SerializeField]
    private Image _image;
    #endregion

    #region Component
    protected Image Image => _image;
    protected RectTransform ImageRect
    {
        get
        {
            _imageRect ??= _image?.GetComponent<RectTransform>();
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
    public abstract bool State { get; set; }
    public int Index { get; set; }
    public TPConnection Connection { get; set; }
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (_nameText is null)
            {
                Debug.LogError($"{GetType().Name}: _nameText is null");
                return;
            }
            _nameText.text = value;
        }
    }
    public bool BlockConnect { get; set; }
    public GameObject GameObject => gameObject;
    public Vector2 Location => (Vector2)ImageRect?.position;

    public abstract void LinkTo(ITransitionPoint targetTp, TPConnection connection = null);
    public abstract void AcceptLink(TPConnection connection);
    public abstract void ClearConnection();
    #endregion

    #region Use in child
    public Node Node
    {
        get => _node;
        set
        {
            if (_node is null)
                _node = value;
        }
    }

    public Action<PositionInfo> OnMove { get; set; }

    protected virtual List<ContextElement> ContextElements
    {
        get => new List<ContextElement>()
        {
            new ContextElement(clickAction: () => Connection?.Disconnect(), text: "Disconnect")
        };
    }

    protected void SetImageColor(Color color)
    {
        if (Image is null)
            return;

        Image.color = color;
    }
    #endregion

    #region MouseEvent
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        SetImageColor(_highlightedColor);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        SetImageColor(State ? _activeColor : _defaultColor);
    }
    
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            Utils.ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, ContextElements.ToArray());
    }
    #endregion
}