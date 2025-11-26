using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class ConsoleWindowResizingHandle : MonoBehaviour, IDraggable, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ResizeCorner m_TargetCorner;
    [SerializeField] private Texture2D m_CursorTexture;
    [SerializeField] private Vector2 m_CursorOffset = Vector2.zero;

    public event Action<Vector2, ConsoleWindowResizingHandle> OnDrag;
    public event Action<ConsoleWindowResizingHandle> OnDragEnd;

    public ResizeCorner TargetCorner => m_TargetCorner;

    public bool OtherDrag
    {
        get => _otherDrag;
        set
        {
            if (_otherDrag != value && _otherDrag && _mouseEnder)
            {
                Other.InvokeActionDelay(() => SetCursor(true)).Forget();
            }

            _otherDrag = value;
        }
    }

    private bool _otherDrag;
    private bool _mouseEnder;
    private bool _setCursor;
    private bool _dragging;

    private void SetCursor(bool set)
    {
        if (m_CursorTexture == null)
        {
            return;
        }

        if (OtherDrag)
        {
            return;
        }

        if (_setCursor == set)
        {
            return;
        }

        _setCursor = set;

        if (set)
        {
            Vector2 hotspot = new Vector2(m_CursorTexture.width * m_CursorOffset.x, m_CursorTexture.height * m_CursorOffset.y);
            Cursor.SetCursor(m_CursorTexture, hotspot, CursorMode.Auto);
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
        SetCursor(true);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        OnDrag?.Invoke(eventData.delta, this);
        SetCursor(true);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke(this);
        _dragging = false;

        if (!_mouseEnder)
        {
            SetCursor(false);
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        _mouseEnder = true;
        SetCursor(true);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _mouseEnder = false;

        if (!_dragging)
        {
            SetCursor(false);
        }
    }
}