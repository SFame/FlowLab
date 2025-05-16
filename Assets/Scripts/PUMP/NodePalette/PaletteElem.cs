using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PaletteElem : MonoBehaviour, IDraggable
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private RectTransform m_Rect;
    [SerializeField] private Image m_Image;
    #endregion

    #region Privates
    private string _displayName;
    private Vector2 _dragOffset;
    private bool _mouseOnPalette;
    private List<RaycastResult> _raycastResults = new();
    private PUMPBackground _background;
    private Node _newNode;

    private void SetPosition(Vector2 position)
    {
        Rect.position = new Vector2(position.x, Rect.position.y);
    }
    #endregion
    
    public event Action OnDragStart;
    public event Action OnDragEnd;
    public event Action OnPaletteExit;
    public event Action OnInstantiate;

    public RectTransform Rect => m_Rect;
    public Image Image => m_Image;

    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            m_Text.text = _displayName;
        }
    }
    
    public Type NodeType { get; set; }
    
    public Vector2 ContentFixPosition { get; set; }

    public Node NewNode => _newNode;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _mouseOnPalette = true;
        _background = null;
        _dragOffset = (Vector2)Rect.position - eventData.position;
        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mouseOnPalette)
            SetPosition(eventData.position + _dragOffset);

        FindPumpBackground(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke();
        
        Rect.localScale = Vector3.one;
        
        if (_background != null && _newNode != null && _newNode is INodeLifecycleCallable callable)
        {
            callable.CallOnCompletePlacementFromPalette();
        }
        
        _newNode = null;
        _mouseOnPalette = true;
        _background = null;
    }

    private void FindPumpBackground(PointerEventData eventData)
    {
        if (_background is null)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            if (_mouseOnPalette)
            {
                _mouseOnPalette = _raycastResults.Any(result => result.gameObject.TryGetComponent<NodePalette>(out _));
                
                if (_mouseOnPalette)
                {
                    _raycastResults.Clear();
                    _background = null;
                }
                else
                {
                    OnPaletteExit?.Invoke();
                }
            }
            
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject.TryGetComponent(out _background))
                {
                    _newNode = _background.AddNewNode(NodeType);
                    Rect.localScale = Vector3.zero;
                    OnInstantiate?.Invoke();
                    break;
                }
                    
            }
        }
        
        if (_background is null)
            return;

        _newNode.Support.SetPosition(eventData.position);
    }
}
