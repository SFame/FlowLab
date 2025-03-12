using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PaletteElem : MonoBehaviour, IDraggable
{
    #region Variables
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] public RectTransform _rect;
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

    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            _text.text = _displayName;
        }
    }
    
    public Type NodeType { get; set; }
    
    public Vector2 ContentFixPosition { get; set; }

    public Node NewNode => _newNode;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _background = null;
        _dragOffset = (Vector2)_rect.position - eventData.position;
        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
            _rect.position = eventData.position + _dragOffset;
        
        FindPumpBackground(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            OnDragEnd?.Invoke();
        }
        _rect.localScale = Vector3.one;
        
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
                    _rect.localScale = Vector3.zero;
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
