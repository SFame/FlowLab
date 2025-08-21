using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PUMPTool : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private float m_PollingRate = 0.1f;
    [SerializeField] private float m_SlideDuration = 0.5f;
    [SerializeField] private float m_HandleFadeDuration= 0.1f;

    [SerializeField] private float m_DetectionAreaWidth = 250f;
    [SerializeField] private float m_DetectionAreaHeightOffset = 100f;

    [SerializeField] private SaveLoadUiController m_SaveLoadUiController;
    [SerializeField] private NodePalette m_NodePalette;
    [SerializeField] private Image m_HandleImage;
    [SerializeField] private Button m_SaveLoadButton;
    [SerializeField] private Button m_NodePaletteButton;
    #endregion

    private Vector2 _hiddenPosition;
    private Vector2 _visiblePosition;
    private bool _isVisible = false;
    private SafetyCancellationTokenSource _cts;
    private RectTransform _rectTransform;
    private float _minY;
    private float _maxY;
    private int _screenWidth;
    private int _screenHeight;

    private void SetButtonCallbacks()
    {
        if (m_SaveLoadButton == null || m_SaveLoadUiController == null || m_NodePaletteButton == null || m_NodePalette == null)
        {
            Debug.LogError("PUMPTool: Inspector 확인");
            return;
        }

        m_SaveLoadButton.onClick.AddListener(OpenSaveLoadPanel);
        m_NodePaletteButton.onClick.AddListener(OpenPalette);
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        SetButtonCallbacks();
        InitializePositions(UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate)).Forget();
    }

    private void OnEnable()
    {
        StartPollingTask();
    }

    private void OnDisable()
    {
        StopPollingTask();
    }

    private void OnDestroy()
    {
        StopPollingTask();
    }

    private async UniTask InitializePositions(UniTask task)
    {
        await task;

        _hiddenPosition = _rectTransform.anchoredPosition;
        _visiblePosition = new Vector2(_hiddenPosition.x + _rectTransform.rect.width, _hiddenPosition.y);

        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        float halfHeightOffset = m_DetectionAreaHeightOffset * 0.5f;
        _minY = corners[0].y - halfHeightOffset;
        _maxY = corners[1].y + halfHeightOffset;
    }

    private void StartPollingTask()
    {
        _cts?.CancelAndDispose();
        _cts = new SafetyCancellationTokenSource();
        StartPolling(_cts.Token).Forget();
    }

    private void StopPollingTask()
    {
        _cts?.CancelAndDispose();
        _cts = null;
    }

    private async UniTaskVoid StartPolling(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UpdateScreenResolution();
                CheckMousePosition();
                await UniTask.Delay(TimeSpan.FromSeconds(m_PollingRate), cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"Polling error: {ex}");
        }
    }

    private void CheckMousePosition()
    {
        if (Input.anyKey)
            return;

        Vector2 mousePos = Input.mousePosition;

        bool isInXRange = mousePos.x < m_DetectionAreaWidth;
        bool isInYRange = mousePos.y >= _minY && mousePos.y <= _maxY;

        if (isInXRange && isInYRange)
        {
            if (!_isVisible)
                ShowToolbar();
        }
        else
        {
            if (_isVisible)
                HideToolbar();
        }
    }

    private void ShowToolbar()
    {
        _isVisible = true;
        _rectTransform.DOKill();
        _rectTransform.DOAnchorPos(_visiblePosition, m_SlideDuration).SetEase(Ease.OutQuad);
        m_HandleImage.DOKill();
        m_HandleImage.DOFade(0f, m_HandleFadeDuration * 0.5f).SetEase(Ease.Linear);
    }

    private void HideToolbar()
    {
        _isVisible = false;
        _rectTransform.DOKill();
        _rectTransform.DOAnchorPos(_hiddenPosition, m_SlideDuration).SetEase(Ease.InQuad);
        m_HandleImage.DOKill();
        m_HandleImage.DOFade(0f, 0f);
        m_HandleImage.DOFade(1f, m_HandleFadeDuration).SetDelay(m_SlideDuration).SetEase(Ease.Linear);
    }

    private void UpdateScreenResolution()
    {
        if (Screen.width == _screenWidth && Screen.height == _screenHeight)
        {
            return;
        }

        _screenWidth = Screen.width;
        _screenHeight = Screen.height;
        InitializePositions(UniTask.CompletedTask).Forget();
    }


    public void OpenPalette()
    {
        m_NodePalette.SetActive(true);
    }

    public void OpenSaveLoadPanel()
    {
        m_SaveLoadUiController.SetActive(true);
    }

    public void TogglePalette()
    {
        m_NodePalette.IsActive = !m_NodePalette.IsActive;
    }

    public void ToggleSaveLoadPanel()
    {
        m_SaveLoadUiController.IsActive = !m_SaveLoadUiController.IsActive;
    }
}