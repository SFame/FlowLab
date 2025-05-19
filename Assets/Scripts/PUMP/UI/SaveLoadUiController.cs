using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SaveLoadUiController : MonoBehaviour, IPointerClickHandler
{
    private CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    private CanvasGroup _canvasGroup;

    public async UniTaskVoid SetActive(bool active, float fadeDuration = 0.4f)
    {
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

    public async UniTask Fade(float targetAlpha, float duration)
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

    public void OnPointerClick(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);

        if (result.Count <= 0)
            return;

        if (result[0].gameObject == gameObject)
            SetActive(false, 0.2f).Forget();
    }
}