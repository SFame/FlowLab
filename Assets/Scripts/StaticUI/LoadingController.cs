using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour, ILoadingUi
{
    [SerializeField] private GameObject m_ShowHideTarget;
    [SerializeField] private Slider m_Slider;
    [SerializeField] private TextMeshProUGUI m_ProgressText;
    [SerializeField] private TextMeshProUGUI m_Tmp;
    [SerializeField] private List<string> m_TextAnimation;
    [SerializeField] private float m_animationInterval = 0.5f;

    private bool _isShowing = false;
    private SafetyCancellationTokenSource _textAnimationCts = new(false);
    private SafetyCancellationTokenSource _sliderAnimationCts = new(false);

    private float Value
    {
        set
        {
            m_Slider.value = value;
            int progressValue = Mathf.RoundToInt(value * 100);
            m_ProgressText.text = $"{progressValue} %";
        }
    }

    public float SliderMoveDuration { get; set; }

    public void Show()
    {
        if (_isShowing)
            return;

        _isShowing = true;
        m_ShowHideTarget?.SetActive(true);
        Value = 0;
        TextAnimationActive(true);
    }

    public void Hide()
    {
        _isShowing = false;
        m_ShowHideTarget?.SetActive(false);
        Value = 0;
        TextAnimationActive(false);
    }

    public void SetSliderValue(float value)
    {
        _sliderAnimationCts = _sliderAnimationCts.CancelAndDisposeAndGetNew();
        SliderAnimationAsync(value, _sliderAnimationCts.Token).Forget();
    }

    private void TextAnimationActive(bool active)
    {
        _textAnimationCts.CancelAndDispose();

        if (active)
        {
            _textAnimationCts = new(false);
            TextAnimationAsync(_textAnimationCts.Token).Forget();
        }
    }

    private async UniTaskVoid TextAnimationAsync(CancellationToken token)
    {
        if (m_TextAnimation == null)
            return;

        while (!token.IsCancellationRequested)
        {
            foreach (string text in m_TextAnimation)
            {
                m_Tmp.text = text;
                await UniTask.WaitForSeconds(m_animationInterval, true, PlayerLoopTiming.Update, token);
            }
        }
    }

    private async UniTaskVoid SliderAnimationAsync(float targetValue, CancellationToken token)
    {
        float currentValue = m_Slider.value;
        float elapsed = 0f;
        try
        {
            while (elapsed < SliderMoveDuration && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SliderMoveDuration;
                Value = Mathf.Lerp(currentValue, targetValue, t);
                await UniTask.Yield(token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Value = targetValue;
        }
    }
}

public interface ILoadingUi
{
    float SliderMoveDuration { get; set; }
    void SetSliderValue(float value);
    void Show();
    void Hide();
}