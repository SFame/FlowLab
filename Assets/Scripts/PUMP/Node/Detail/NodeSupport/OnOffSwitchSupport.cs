using UnityEngine;
using UnityEngine.UI;

public class OnOffSwitchSupport : MonoBehaviour, ISoundable
{
    [SerializeField] private Image m_NodeImage;
    [SerializeField] private Image m_ShadowImage;
    [SerializeField] private Sprite m_ActivateSprite;
    [SerializeField] private Sprite m_DeactivateSprite;
    [SerializeField] private Color m_PushColor;

    public event SoundEventHandler OnSounded;

    public void SetPush(bool push)
    {
        m_ShadowImage.color = push ? m_PushColor : new Color(0f, 0f, 0f, 0f);
    }

    public void SetActivate(bool activate)
    {
        m_NodeImage.sprite = activate ? m_ActivateSprite : m_DeactivateSprite;
    }

    public void PlaySound(bool onDown)
    {
        OnSounded?.Invoke(this, new SoundEventArgs(onDown ? 1 : 2));
    }
}
