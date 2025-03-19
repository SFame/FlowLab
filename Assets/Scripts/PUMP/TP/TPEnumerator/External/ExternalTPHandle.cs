using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ExternalTPHandle : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private TransitionPoint _tp;

    #region Privates
    private RectTransform _rect;
    #endregion
    public ITransitionPoint TP => _tp;

    public event Action<PointerEventData> OnDragging;
    public event Action<PointerEventData> OnDragEnd;

    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    public void Destroy()
    {
        if (TP != null)
        {
            TP.Connection?.Disconnect();
            Destroy(TP.GameObject);
        }
        Destroy(gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragging?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke(eventData);
    }
}
