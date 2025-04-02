using System;
using System.Collections.Generic;
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
    private bool _isDragging;
    private List<RaycastResult> _raycastResults = new();
    private PUMPBackground _background;
    private Node _newNode;
    #endregion
    
    public event Action OnDragStart;
    public event Action OnDragEnd;
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
        _isDragging = true;
        _background = null;
        _dragOffset = (Vector2)Rect.position - eventData.position;
        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
            Rect.position = eventData.position + _dragOffset;
        
        FindPumpBackground(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            OnDragEnd?.Invoke();
        }
        Rect.localScale = Vector3.one;
        
        if (_background != null && _newNode != null)
            _newNode.CallCompletePlacementFromPalette();
        
        _newNode = null;
        _isDragging = false;
        _background = null;
    }

    private void FindPumpBackground(PointerEventData eventData)
    {
        if (_background is null)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject.GetComponent<NodePalette>() != null)
                {
                    _raycastResults.Clear();
                    _background = null;
                    break;
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
        
        _isDragging = false;
        OnDragEnd?.Invoke();
        
        _newNode.SetPosition(eventData.position);
    }
}
