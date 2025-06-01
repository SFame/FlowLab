using DG.Tweening;
using TMPro;
using UnityEngine;

public class TypeConverterSupport : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_FailText;
    [SerializeField] private float m_FailShowDuration = 2f;
    [SerializeField] private float m_FailFadeDuration = 0.1f;

    public void ShowFail()
    {
        m_FailText.DOKill();
        m_FailText.DOFade(1f, 0f);
        m_FailText.DOFade(0f, m_FailFadeDuration).SetDelay(m_FailShowDuration);
    }

    public void HideFail()
    {
        m_FailText.DOKill();
        m_FailText.DOFade(0f, 0f);
    }
}