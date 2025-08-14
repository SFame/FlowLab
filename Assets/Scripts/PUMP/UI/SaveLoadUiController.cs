using DG.Tweening;
using UnityEngine;

public class SaveLoadUiController : MonoBehaviour
{
    [SerializeField] private CloseablePanel m_CloseablePanel;
    [SerializeField] private float m_FadeDuration = 0.1f;

    private CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    private CanvasGroup _canvasGroup;
    private bool _initialized = false;
    private bool _isActive = false;

    public void SetActive(bool active)
    {
        Initialize();

        CanvasGroup.DOKill();
        _isActive = active;

        if (active)
        {
            gameObject.SetActive(true);
            CanvasGroup.DOFade(1f, m_FadeDuration);
            return;
        }

        CanvasGroup.DOFade(0f, m_FadeDuration).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetActive(value);
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (m_CloseablePanel == null)
        {
            Debug.LogError("SaveLoadUiController: ClosablePanel is null");
            return;
        }

        m_CloseablePanel.ControlActive = false;
        m_CloseablePanel.OnClose += () => SetActive(false);
    }
}