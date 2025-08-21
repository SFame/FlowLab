using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class SelectionAreaController : MonoBehaviour, IPointerDownHandler, IDraggable
{
    #region  On Inspector
    [SerializeField] private GameObject m_SelectionRangePrefab;
    [SerializeField] private RectTransform m_SelectingZone;
    #endregion

    #region Privates
    private RectTransform _draggingRangeRect;
    private Vector2 _selectStartPos;

    private RectTransform SelectingRangeRect
    {
        get
        {
            if (_draggingRangeRect is null)
            {
                _draggingRangeRect = Instantiate(m_SelectionRangePrefab, m_SelectingZone).GetComponent<RectTransform>();
                _draggingRangeRect.sizeDelta = Vector2.zero;
                _draggingRangeRect.anchorMin = new Vector2(0f, 1f);
                _draggingRangeRect.anchorMax = new Vector2(0f, 1f);
                _draggingRangeRect.SetAsLastSibling();
                _draggingRangeRect.gameObject.SetActive(false);
            }
            return _draggingRangeRect;
        }
    }
    #endregion

    public event Action<Vector2> OnMouseDown;
    public event Action OnMouseBeginDrag;
    public event Action OnMouseDrag;
    public event Action<Vector2, Vector2> OnMouseEndDrag;

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        OnMouseDown?.Invoke(eventData.position.ScreenToWorldPoint());
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        OnMouseBeginDrag?.Invoke();
        _selectStartPos = eventData.position.ScreenToWorldPoint();
        SelectingRangeRect.gameObject.SetActive(true);
        SelectingRangeRect.sizeDelta = Vector2.zero;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        OnMouseDrag?.Invoke();
        Vector2 currentPos = eventData.position.ScreenToWorldPoint();

        float pivotX = currentPos.x < _selectStartPos.x ? 1 : 0;
        float pivotY = currentPos.y < _selectStartPos.y ? 1 : 0;
        SelectingRangeRect.pivot = new Vector2(pivotX, pivotY);

        float width = Mathf.Abs(currentPos.x - _selectStartPos.x);
        float height = Mathf.Abs(currentPos.y - _selectStartPos.y);
        SelectingRangeRect.sizeDelta = new Vector2(width, height);

        SelectingRangeRect.position = _selectStartPos;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        SelectingRangeRect.gameObject.SetActive(false);
        OnMouseEndDrag?.Invoke(_selectStartPos, eventData.position.ScreenToWorldPoint());
    }
}