using Cysharp.Threading.Tasks;
using UnityEngine;

public class SaveLoadUiController : MonoBehaviour
{
    [SerializeField] private ClosablePanel m_ClosablePanel;

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

    public async UniTaskVoid SetActive(bool active, float fadeDuration = 0.2f)
    {
        Initialize();

        if (active)
        {
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(true);
        }
        else
            CanvasGroup.alpha = 1f;

        float targetAlpha = active ? 1f : 0f;
        await Fade(targetAlpha, fadeDuration);

        if (!active)
            gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (m_ClosablePanel == null)
        {
            Debug.LogError("SaveLoadUiController: ClosablePanel is null");
            return;
        }

        m_ClosablePanel.ControlActive = false;
        m_ClosablePanel.OnClose += () => SetActive(false, 0.2f).Forget();
    }

    private async UniTask Fade(float targetAlpha, float duration)
    {
        float startAlpha = CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;

            float t = normalizedTime * normalizedTime * (3f - 2f * normalizedTime); // y = 3x^2 - 2x^3
            CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        CanvasGroup.alpha = targetAlpha;
    }
}