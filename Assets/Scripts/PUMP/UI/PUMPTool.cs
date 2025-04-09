using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(CanvasGroup))]
public class PUMPTool : MonoBehaviour, IRaycastAlphaControl
{
    #region On Inspector
    [SerializeField] private SaveLoadUiController saveLoadUiController;
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button saveLoadButton;

    [SerializeField] private AnimationCurve rectMoveCurve;
    [SerializeField] private float rectMoveDuration = 0.5f;
    [SerializeField] private float openButtonFadeDuration = 0.2f;
    [SerializeField] private float interactAlphaFadeDuration = 0.2f;
    #endregion
    
    public void SetInteract(bool interact)
    {
        if (CanvasGroup == null)
        {
            Debug.LogWarning($"{name}Canvas group is null");
            return;
        }
        
        if (_isInteracting == interact)
            return;
        
        _isInteracting = interact;
        _cts?.Cancel();
        _cts = new();

        if (interact)
        {
            Other.LerpAction(interactAlphaFadeDuration, t =>
                {
                    CanvasGroup.alpha = t;
                },
                () =>
                {
                    CanvasGroup.alpha = 1;
                    CanvasGroup.interactable = true;
                    CanvasGroup.blocksRaycasts = true;
                },
                _cts.Token).Forget();
        }
        else
        {
            Other.LerpAction(interactAlphaFadeDuration, t =>
                {
                    CanvasGroup.alpha = 1f - t;
                },
                () =>
                {
                    CanvasGroup.alpha = 0;
                    CanvasGroup.interactable = false;
                    CanvasGroup.blocksRaycasts = false;
                },
                _cts.Token).Forget();
        }
    }
    
    #region Privates
    private RectTransform _rect;
    private float _defaultRectWidth;
    private Vector2 _hidePosition;
    private Vector2 _showPosition;
    private UniTask _moveTask = UniTask.CompletedTask;
    private CancellationTokenSource _cts;
    private bool _isInteracting = true;
    
    private Image _openButtonImage;
    private CanvasGroup _canvasGroup;
    
    private void Awake()
    {
        _defaultRectWidth = Rect.rect.width;
        _hidePosition = new Vector2(_defaultRectWidth * -0.5f, Rect.anchoredPosition.y);
        _showPosition = new Vector2(_defaultRectWidth * 0.5f, Rect.anchoredPosition.y);
        Rect.anchoredPosition = _hidePosition;
        SetButtonCallbacks();
    }

    private void SetButtonCallbacks()
    {
        openButton.onClick.AddListener(() => OpenTool().Forget());
        closeButton.onClick.AddListener(() => CloseTool().Forget());
        saveLoadButton.onClick.AddListener(() => saveLoadUiController.SetActive(true, 0.2f).Forget());
    }

    private async UniTaskVoid OpenTool()
    {
        if (_moveTask.Status != UniTaskStatus.Succeeded)
            return;
        
        await _moveTask;
        _moveTask = UniTask.WhenAll(MoveRectAsync(_showPosition), SetActiveCloseButton(false));
    }

    private async UniTaskVoid CloseTool()
    {
        if (_moveTask.Status != UniTaskStatus.Succeeded)
            return;
        
        await _moveTask;
        _moveTask = MoveRectAsync(_hidePosition).ContinueWith(() => SetActiveCloseButton(true));
    }

    private UniTask MoveRectAsync(Vector2 targetPosition)
    {
        Vector2 startPosition = Rect.anchoredPosition;
        return rectMoveCurve.CurveAction
        (
            rectMoveDuration,
            t => Rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t)
        );
    }

    private async UniTask SetActiveCloseButton(bool activate)
    {
        if (openButton == null || OpenButtonImage == null)
            return;
        
        if (activate)
            openButton.gameObject.SetActive(true);
        
        Color currentColor = OpenButtonImage.color;
        float startAlpha = currentColor.a;
        float targetAlpha = activate ? 1 : 0;

        await Other.LerpAction
        (
            openButtonFadeDuration,
            t =>
            {
                currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                OpenButtonImage.color = currentColor;
            }
        );
        
        if (!activate)
            openButton.gameObject.SetActive(false);
    }
    #endregion
    
    #region Properties
    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    private Image OpenButtonImage
    {
        get
        {
            _openButtonImage ??= openButton.GetComponent<Image>();
            return _openButtonImage;
        }
    }

    private CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }
    #endregion
}

public interface IRaycastAlphaControl
{
    void SetInteract(bool interact);
}