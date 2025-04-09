using System;
using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

public class SaveScrollElem : MonoBehaviour, ISaveScrollElem, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private RawImage image;
    #endregion
    
    #region Privates
    private Action<PUMPSaveDataStructure> _onDoubleClick;
    private Action<PUMPSaveDataStructure, PointerEventData> _onRightClick;
    private PUMPSaveDataStructure Data { get; set; }
    private float _lastClickTime;
    private Vector2 _lastClickPos;
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

    private void OnDestroy()
    {
        SetImage(null);
    }

    private void SetImage(Texture2D texture)
    {
        Texture beforeTexture = image.texture;
        if (beforeTexture != null)
            Destroy(beforeTexture);
        
        image.texture = texture;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
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
        SetImage(Capture.LoadTextureFromFile(Data.ImagePath));
    }

    public void Initialize(PUMPSaveDataStructure data)
    {
        _onDoubleClick = null;
        _onRightClick = null;
        Data = data;
        Refresh();
    }
}