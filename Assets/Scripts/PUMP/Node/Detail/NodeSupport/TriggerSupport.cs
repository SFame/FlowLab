using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TriggerSupport : MonoBehaviour
{
    [SerializeField] private Graphic m_Graphic;
    [SerializeField] private float m_ClickAlpha = 0.4f;
    [SerializeField] private float m_FadeDuration = 0.1f;

    public void PlayEffect()
    {
        m_Graphic.DOKill();
        m_Graphic.DOFade(m_ClickAlpha, 0f);
        m_Graphic.DOFade(0f, m_FadeDuration);
    }
}