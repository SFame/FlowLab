using UnityEngine;

public class SetVisibleTarget : MonoBehaviour, ISetVisibleTarget
{
    [SerializeField] private CanvasGroup m_CanvasGroup;

    public void SetVisible(bool visible)
    {
        if (m_CanvasGroup == null)
            return;

        m_CanvasGroup.alpha = visible ? 1 : 0;
        m_CanvasGroup.interactable = visible;
        m_CanvasGroup.blocksRaycasts = visible;
    }
}