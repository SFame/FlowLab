using UnityEngine;
using UnityEngine.UI;

public class TriggerSupport : MonoBehaviour, ISoundable
{
    [SerializeField] private Image m_NodeImage;
    [SerializeField] private Sprite m_ActiveImage;
    [SerializeField] private Sprite m_DeactiveImage;

    public event SoundEventHandler OnSounded;

    public void SetDown(bool isDown)
    {
        m_NodeImage.sprite = isDown ? m_ActiveImage : m_DeactiveImage;
    }

    public void PlaySound(bool isDown)
    {
        OnSounded?.Invoke(this, new SoundEventArgs(isDown ? 1 : 2));
    }
}