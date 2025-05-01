using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

public class SaveScrollElem : MonoBehaviour, ISaveScrollElem, IPointerClickHandler, IClassedDataTargetUi
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private RawImage image;
    [SerializeField] private SaveDisplayer m_Displayer;

    [Space(10)]

    [SerializeField] private Image m_HighlightImage;
    [SerializeField] private Color m_HighlightColor;
    [SerializeField] private Color m_DefaultColor;
    #endregion

    #region Privates
    private Action<PUMPSaveDataStructure> _onDoubleClick;
    private Action<PUMPSaveDataStructure, PointerEventData> _onRightClick;
    private bool _classedDataTarget_IsPointerEnter;
    private float _lastClickTime;
    private Vector2 _lastClickPos;
    private List<Type> _displayExclusionType = new() { typeof(ExternalInput), typeof(ExternalOutput) };
    private const float DOUBLE_CLICK_TIME = 0.5f;
    private const float DOUBLE_CLICK_MAX_DISTANCE = 10f; // 픽셀 단위
    
    private void InvokeOnRightClick(PointerEventData eventData)
    {
        _onRightClick?.Invoke(Data, eventData);
    }
    
    private void InvokeOnDoubleClick()
    {
        _onDoubleClick?.Invoke(Data);
    }

    private bool IsExclusionType(Type nodeType)
    {
        return _displayExclusionType.Contains(nodeType);
    }

    private void SetDisplay(List<Vector2> normalizedPosition)
    {
        if (normalizedPosition == null)
        {
            Debug.LogError($"{name}: SetDisplay() Received null args");
            return;
        }

        m_Displayer.SetNodeCells(normalizedPosition);
    }

    private void OnDestroy()
    {
        m_Displayer.Dispose();
    }

    public PUMPSaveDataStructure Data { get; set; }

    void IClassedDataTargetUi.IsPointEnter(bool isEnter)
    {
        _classedDataTarget_IsPointerEnter = isEnter;

        if (m_HighlightImage == null)
            return;

        m_HighlightImage.color = isEnter ? m_HighlightColor : m_DefaultColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_classedDataTarget_IsPointerEnter)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InvokeOnRightClick(eventData);
            return;
        }
        
        float timeSinceLastClick = Time.time - _lastClickTime;
        float distanceFromLastClick = Vector2.Distance(eventData.position, _lastClickPos);

        if (timeSinceLastClick <= DOUBLE_CLICK_TIME && distanceFromLastClick <= DOUBLE_CLICK_MAX_DISTANCE)
            InvokeOnDoubleClick();

        _lastClickTime = Time.time;
        _lastClickPos = eventData.position;
    }
    #endregion
    
    public event Action<PUMPSaveDataStructure> OnDoubleClick
    {
        add => _onDoubleClick += value;
        remove => _onDoubleClick -= value;
    }

    public event Action<PUMPSaveDataStructure, PointerEventData> OnRightClick
    {
        add => _onRightClick += value;
        remove => _onRightClick -= value;
    }

    public void Refresh()
    {
        if (Data == null)
            return;

        nameText.text = Data.Name;
        DateTime date = Data.LastUpdate;
        dateText.text = $"<b>{date.Month:D2}</b> / <b>{date.Day:D2}</b> / <b>{date.Year}</b>\n<b>{date.Hour:D2}</b>:<b>{date.Minute:D2}</b>";
        SetDisplay(Data.NodeInfos
            .Where(info => !IsExclusionType(info.NodeType))
            .Select(info => info.NodePosition)
            .ToList());
    }

    public void Initialize(PUMPSaveDataStructure data)
    {
        _onDoubleClick = null;
        _onRightClick = null;
        Data = data;
        Refresh();
    }
}