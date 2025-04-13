using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ExternalTPEnum : MonoBehaviour, ITPEnumerator, IHighlightable
{
    [SerializeField]
    private GameObject _handleTemplate;
    [SerializeField]
    private RectTransform _tpSlider;

    #region Privates
    private bool _hasSet = false;
    private Node _node;
    private RectTransform _rect;
    private Canvas _canvas;
    private float _minHeight;
    private void Awake()
    {
        _handleTemplate?.SetActive(false);
    }
    #endregion

    #region Components
    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    private Canvas RootCanvas
    {
        get
        {
            _canvas ??= GetComponentInParent<Canvas>().rootCanvas;
            return _canvas;
        }
    }
    #endregion

    #region Interface
    public List<ExternalTPHandle> Handles { get; private set; } = new();

    public List<float> GetHandlesRatio() => Handles.Select(handle => GetPositionRatio(handle)).ToList();
    public void SetHandlePositionsToRatio(List<float> ratios)
    {
        if (Handles.Count != ratios.Count)
        {
            Debug.LogWarning($"{GetType().Name}: Handles <=> Ratios couns dosen't match");
            return;
        }

        for (int i = 0; i < ratios.Count; i++)
        {
            SetPositionToRatio(Handles[i], ratios[i]);
        }
    }
    #endregion

    private float GetPositionRatio(ExternalTPHandle handle)
    {
        float parentHeight = _tpSlider.rect.height;
        float parentMinY = _tpSlider.rect.min.y;
        float parentMaxY = _tpSlider.rect.max.y;
        float handleHeight = handle.Rect.rect.height;

        Vector3 handleLocalPos = _tpSlider.InverseTransformPoint(handle.Rect.position);
        float handleY = handleLocalPos.y;
        float handlePivotY = handle.Rect.pivot.y;

        float handleBottomY = handleY - handleHeight * handlePivotY;
        float handleTopY = handleBottomY + handleHeight;

        float ratio = (parentMaxY - handleTopY) / (parentHeight - handleHeight);

        return ratio;
    }

    private void SetPositionToRatio(ExternalTPHandle handle, float ratio)
    {
        float parentHeight = _tpSlider.rect.height;
        float parentMinY = _tpSlider.rect.min.y;
        float parentMaxY = _tpSlider.rect.max.y;
        float handleHeight = handle.Rect.rect.height;
        float handlePivotY = handle.Rect.pivot.y;

        float handleTopY = parentMaxY - (ratio * (parentHeight - handleHeight));
        float handleY = handleTopY - (handleHeight * (1 - handlePivotY));

        Vector3 currentLocalPos = handle.Rect.localPosition;
        Vector3 newLocalPos = new Vector3(currentLocalPos.x, handleY, currentLocalPos.z);

        handle.Rect.localPosition = newLocalPos;
    }

    private void HandleDraggingCallback(PointerEventData eventData, ExternalTPHandle handle)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _tpSlider,
            eventData.position,
            RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : RootCanvas.worldCamera,
            out Vector2 mouseLocalPos
        );

        float currentYPos = handle.Rect.localPosition.y;
        handle.Rect.localPosition = new Vector2(0f, mouseLocalPos.y);

        float ratio = GetPositionRatio(handle);
        if (ratio < 0 || ratio > 1)
            handle.Rect.localPosition = new Vector2(0f, currentYPos);

        if (handle.TP is IMoveable moveable)
        {
            moveable.OnMove?.Invoke(default);
        }
    }

    private void HandleEndDragCallback(PointerEventData eventData, ExternalTPHandle handle)
    {
        Node.ReportChanges();
    }

    private ExternalTPHandle InstantiateHandle()
    {
        if (_handleTemplate == null || _tpSlider == null)
        {
            Debug.LogError($"{GetType().Name}: Can't find Object: {_handleTemplate} / {_tpSlider}");
            return null;
        }

        GameObject handleObject = Instantiate(_handleTemplate, _tpSlider);
        handleObject.SetActive(true);

        if (handleObject.TryGetComponent(out ExternalTPHandle handle))
        {
            handle.OnDragging += eventData => HandleDraggingCallback(eventData, handle);
            handle.OnDragEnd += _ => Node.ReportChanges();
            return handle;
        }

        Destroy(handleObject);
        Debug.LogError($"{GetType().Name}: Can't find Handle Component in template");
        return null;
    }

    #region TP_Enumerator
    private Vector2 TpSize { get; set; }

    public Node Node
    {
        get => _node;
        set
        {
            if (_node is null)
                _node = value;
        }
    }
    public float MinHeight
    {
        get => _minHeight;
        set
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
            _minHeight = value;
        }
    }

    public event Action<Vector2> OnSizeUpdatedWhenTPChange;

    public TPEnumeratorToken GetToken()
    {
        if (!_hasSet)
        {
            Debug.LogError("Require TPEnum set first");
            return null;
        }

        return new TPEnumeratorToken(Handles.Select(handle => handle.TP), this);
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public ITPEnumerator SetTPsMargin(float value)
    {
        // 슬라이더 방식은 Set Margin 구현하지 않음
        return this;
    }

    public ITPEnumerator SetTPSize(Vector2 value)
    {
        TpSize = value;
        return this;
    }

    public ITPEnumerator SetHeight(float value)
    {
        Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, value);
        return this;
    }

    public ITPEnumerator SetTPs(int count)
    {
        foreach (ExternalTPHandle handle in Handles)
            handle.Destroy();
        Handles.Clear();

        float interval = 1f / (count + 1);

        for (int i = 0; i < count; i++)
        {
            ExternalTPHandle handle = InstantiateHandle();

            if (handle is null)
                return this;

            float ratio = (i + 1) * interval;
            SetPositionToRatio(handle, ratio);

            if (handle.TP != null)
                handle.TP.Node = Node;

            Handles.Add(handle);
        }

        _hasSet = true;
        return this;
    }

    public void SetTPsConnection(ITransitionPoint[] targetTps, List<Vector2>[] vertices, DeserializationCompleteReceiver completeReceiver)
    {
        if (!(targetTps.Length == vertices.Length && vertices.Length == Handles.Count))
        {
            Debug.Log($"{name}: 직렬화 정보와 불일치: data: {targetTps.Length} / TP: {Handles.Count}");

            IEnumerable<ITransitionPoint> tps = Handles.Select(handle => handle.TP);
            
            foreach (ITransitionPoint tp in tps)
            {
                tp.Connection?.Disconnect();
                tp.BlockConnect = true;
            }

            completeReceiver.Subscribe(() =>
            {
                foreach (ITransitionPoint tp in tps)
                {
                    if (tp != null)
                    {
                        tp.BlockConnect = false;
                    }
                }
            });

            return;
        }

        for (int i = 0; i < Handles.Count; i++)
        {
            if (Handles[i].TP.Connection == null && targetTps[i] != null && vertices[i] != null) // 연결되어 있지 않을 때만 실행, targetTp가 있을 때만 실행
            {
                TPConnection newConnection = new() { LineEdges = vertices[i] };

                newConnection.DisableFlush = true;
                Handles[i].TP.LinkTo(targetTps[i], newConnection);
                newConnection.DisableFlush = false;
            }
        }
    }

    public void SetHighlight(bool highlight)
    {
        foreach (ExternalTPHandle handle in Handles)
        {
            handle?.SetHighlight(highlight);
        }
    }
    #endregion
}
