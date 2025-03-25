using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PUMPTool : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private SaveLoadUiController saveLoadUiController;
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button saveLoadButton;

    [SerializeField] private AnimationCurve rectMoveCurve;
    [SerializeField] private float rectMoveDuration = 0.5f;
    [SerializeField] private float openButtonFadeDuration = 0.2f;
    #endregion
    
    #region Privates
    private RectTransform _rect;
    private float _defaultRectWidth;
    private Vector2 _hidePosition;
    private Vector2 _showPosition;
    private UniTask _moveTask = UniTask.CompletedTask;
    
    private Image _openButtonImage;
    
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

    private async UniTask MoveRectAsync(Vector2 targetPosition)
    {
        float elapsed = 0f;
        float curveValue;
        Vector2 startPosition = Rect.anchoredPosition;
        while (elapsed < rectMoveDuration)
        {
            curveValue = rectMoveCurve.Evaluate(elapsed / rectMoveDuration);
            Rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveValue);
            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }
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
        
        float elapsed = 0f;
        
        while (elapsed < openButtonFadeDuration)
        {
            currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / openButtonFadeDuration);
            OpenButtonImage.color = currentColor;
            
            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }
        
        currentColor.a = targetAlpha;
        OpenButtonImage.color = currentColor;
        
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
    #endregion
}
