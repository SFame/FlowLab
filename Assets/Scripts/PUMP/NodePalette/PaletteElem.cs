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
    [SerializeField] private Image m_Image;
    #endregion

    #region Privates
    private string _displayName;
    private bool _mouseOnPalette;
    private List<RaycastResult> _raycastResults = new();
    private PUMPBackground _background;
    private Node _newNode;
    #endregion
    
    public event Action OnDragStart;
    public event Action OnDragEnd;
    public event Action OnInstantiate;

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

    public Node NewNode => _newNode;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _mouseOnPalette = true;
        _background = null;
        _newNode = null;
        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        FindPumpBackground(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke();
        
        
        if (_background != null && _newNode != null && _newNode is INodeLifecycleCallable callable)
        {
            callable.CallOnCompletePlacementFromPalette();
        }
        
        _mouseOnPalette = true;
        _background = null;
        _newNode = null;
    }

    private void FindPumpBackground(PointerEventData eventData)
    {
        if (_background is null)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject.TryGetComponent(out _background))
                {
                    _newNode = _background.AddNewNode(NodeType);
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