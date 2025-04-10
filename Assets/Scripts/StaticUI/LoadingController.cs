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
    [SerializeField] private TextMeshProUGUI m_Tmp;
    [SerializeField] private List<string> m_TextAnimation;
    [SerializeField] private float m_animationInterval = 0.5f;

    private bool _isShowing = false;
    private CancellationTokenSource _cts;

    public float SliderValue
    {
        get => m_Slider.value;
        set => m_Slider.value = value;
    }

    public void Show()
    {
        if (_isShowing)
            return;

        _isShowing = true;
        m_ShowHideTarget?.SetActive(true);
        SliderValue = 0;
        TextAnimationActive(true);
    }

    public void Hide()
    {
        _isShowing = false;
        m_ShowHideTarget?.SetActive(false);
        SliderValue = 0;
        TextAnimationActive(false);
    }

    private void TextAnimationActive(bool active)
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }
        _cts?.Dispose();

        if (active)
        {
            _cts = new();
            TextAnimationAsync(_cts.Token).Forget();
        }
    }

    private async UniTaskVoid TextAnimationAsync(CancellationToken token)
    {
        if (m_TextAnimation == null || m_TextAnimation == null)
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
}

public interface ILoadingUi
{
    float SliderValue { get; set; }
    void Show();
    void Hide();
}