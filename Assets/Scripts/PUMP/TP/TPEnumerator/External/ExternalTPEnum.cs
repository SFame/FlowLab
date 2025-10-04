using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ExternalTPEnum : MonoBehaviour, ITPEnumerator, IHighlightable
{
    [SerializeField]
    private GameObject _handleTemplate;

    [SerializeField]
    private RectTransform _tpSlider;

    [SerializeField]
    private float m_Spacing = 10f;

    [SerializeField]
    private float m_HandleMargin = 10f;

    #region Privates
    private bool _hasSet = false;
    private Node _node;
    private RectTransform _rect;
    private float _minHeight;
    private bool _hasHandleSizeSet = false;
    private Vector2 _handleSize = Vector2.zero;
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
    #endregion

    #region Interface
    public List<ExternalTPHandle> Handles { get; private set; } = new();

    public List<float> GetHandlesRatio() => Handles.Select(GetPositionRatio).ToList();

    public void SetHandlePositionsToRatio(List<float> ratios)
    {
        if (Handles.Count != ratios.Count)
        {
            Debug.LogWarning($"{GetType().Name}: Handles <=> Ratios couns dosen't match");
            return;
        }

        for (int i = 0; i < ratios.Count; i++)
        {
            SetPositionToRatio(Handles[i], ratios[i], m_HandleMargin);
        }
    }
    #endregion

    private Vector2 HandleSize
    {
        get
        {
            if (_hasHandleSizeSet)
            {
                return _handleSize;
            }

            RectTransform handlePrefabRect = _handleTemplate.GetComponent<RectTransform>();
            Vector2 size = new Vector2(handlePrefabRect.rect.width, handlePrefabRect.rect.height);
            _handleSize = size;
            return _handleSize;
        }
    }

    private float GetPositionRatio(ExternalTPHandle handle)
    {
        float sliderHeight = _tpSlider.rect.height;
        float sliderTopPositionY = _tpSlider.rect.max.y;
        float handleHeight = handle.Rect.rect.height;
        float handlePivotY = handle.Rect.pivot.y;

        Vector3 handleLocalPos = _tpSlider.InverseTransformPoint(handle.Rect.position);
        float handleY = handleLocalPos.y;

        float pivotOffset = handleHeight * (handlePivotY - 0.5f);
        float adjustedHandleY = handleY - pivotOffset;

        float ratio = 1f - (adjustedHandleY - (sliderTopPositionY - m_HandleMargin)) / -(sliderHeight - m_HandleMargin * 2);

        return ratio;
    }

    private void SetPositionToRatio(ExternalTPHandle handle, float ratio, float margin)
    {
        float sliderHeight = _tpSlider.rect.height;
        float sliderTopPositionY = _tpSlider.rect.max.y;
        float handleHeight = handle.Rect.rect.height;
        float handlePivotY = handle.Rect.pivot.y;

        float pivotOffset = handleHeight * (handlePivotY - 0.5f);
        float yPosition = (sliderTopPositionY - margin) - (sliderHeight - margin * 2) * (1f - ratio) + pivotOffset;

        Vector3 currentLocalPos = handle.Rect.localPosition;
        Vector3 newLocalPos = new Vector3(currentLocalPos.x, yPosition, currentLocalPos.z);

        handle.Rect.localPosition = newLocalPos;
    }

    private void HandleDraggingCallback(PointerEventData eventData, ExternalTPHandle handle)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _tpSlider,
            eventData.position,
            WorldCanvasGetter.RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : WorldCanvasGetter.RootCanvas.worldCamera,
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

    private void SizeUpdate(int count, Vector2 handleSize, float spacing)
    {
        if (count == 0)
        {
            Vector2 zeroSize = new Vector2(handleSize.x, 0f);
            Rect.sizeDelta = zeroSize;
            OnSizeUpdatedWhenTPChange?.Invoke(zeroSize);
            return;
        }

        float height = handleSize.y * count + spacing * (count + 1);

        height = Mathf.Max(height, MinHeight);

        Vector2 size = new Vector2(handleSize.x, height);
        Rect.sizeDelta = size;
        OnSizeUpdatedWhenTPChange?.Invoke(size);
    }

    private void SetPositionToPixel(ExternalTPHandle handle, int index, int totalCount, float spacing)
    {
        float sliderHeight = _tpSlider.rect.height;
        float sliderTopPositionY = _tpSlider.rect.max.y;
        float handleHeight = handle.Rect.rect.height;
        float handlePivotY = handle.Rect.pivot.y;

        float totalHandlesHeight = handleHeight * totalCount + spacing * (totalCount - 1);

        float startY = (sliderHeight - totalHandlesHeight) / 2;

        float firstHandleCenter = sliderTopPositionY - startY - handleHeight / 2;

        float yPosition = firstHandleCenter - index * (handleHeight + spacing);

        float pivotOffset = handleHeight * (handlePivotY - 0.5f);
        yPosition += pivotOffset;

        Vector3 currentLocalPos = handle.Rect.localPosition;
        Vector3 newLocalPos = new Vector3(currentLocalPos.x, yPosition, currentLocalPos.z);
        handle.Rect.localPosition = newLocalPos;
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

        return new TPEnumeratorToken(Handles.Select(handle => handle.TP));
    }

    public ITransitionPoint[] GetTPs()
    {
        return Handles.Select(handle => handle.TP).ToArray();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public ITPEnumerator SetPadding(float value)
    {
        // 슬라이더 방식은 SetPadding 구현하지 않음
        return this;
    }

    public ITPEnumerator SetMargin(float value)
    {
        // 슬라이더 방식은 SetMargin 구현하지 않음
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

    public ITPEnumerator SetTPs(TransitionType[] types)
    {
        if (types is null)
        {
            throw new ArgumentNullException("'types' Cannot be null");
        }

        foreach (ExternalTPHandle handle in Handles)
        {
            handle.Destroy();
        }

        Handles.Clear();

        int count = types.Length;
        SizeUpdate(count, HandleSize, m_Spacing);

        for (int i = 0; i < count; i++)
        {
            ExternalTPHandle handle = InstantiateHandle();

            if (handle is null)
                return this;

            SetPositionToPixel(handle, i, count, m_Spacing);

            if (handle.TP != null)
            {
                handle.TP.SetType(types[i]);
                handle.TP.Node = Node;
                handle.TP.Index = i;
            }

            Handles.Add(handle);

            if (handle.TP is ISortingPositionGettable gettable)
            {
                Node.Background.LineEdgeSortingManager.AddGettable(gettable);
            }
        }

        _hasSet = true;
        return this;
    }

    public void SetTPConnections(ITransitionPoint[] targetTps, List<Vector2>[] vertices, DeserializationCompleteReceiver completeReceiver)
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