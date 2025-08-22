using DG.Tweening;
using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [SerializeField] private MinimapMouseListener m_MinimapMouseListener;
    [SerializeField] private CanvasGroup m_MinimapCanvasGroup;
    [SerializeField] private float m_MinimapFadeDuration = 0.4f;

    private bool _minimapActive = false;

    private void Awake()
    {
        m_MinimapMouseListener.OnMinimapDragging += MoveCamera;
        m_MinimapMouseListener.OnMinimapMouseDown += MoveCamera;
        m_MinimapCanvasGroup.alpha = 0f;
        m_MinimapCanvasGroup.blocksRaycasts = false;
        m_MinimapCanvasGroup.interactable = false;
    }

    private void MoveCamera(Vector2 ratio)
    {
        MainCameraGetter.GetController().SetPositionAsRatio(ratio);
    }

    public void ToggleMinimap()
    {
        m_MinimapCanvasGroup.DOKill();

        if (_minimapActive)
        {
            m_MinimapCanvasGroup.DOFade(0f, m_MinimapFadeDuration).OnComplete(() =>
            {
                m_MinimapCanvasGroup.blocksRaycasts = false;
                m_MinimapCanvasGroup.interactable = false;
            });
        }
        else
        {
            m_MinimapCanvasGroup.blocksRaycasts = true;
            m_MinimapCanvasGroup.interactable = true;
            m_MinimapCanvasGroup.DOFade(1f, m_MinimapFadeDuration);
        }

        _minimapActive = !_minimapActive;
    }
}