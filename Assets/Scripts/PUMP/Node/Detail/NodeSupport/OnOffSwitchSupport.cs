using UnityEngine;
using UnityEngine.UI;

public class OnOffSwitchSupport : MonoBehaviour
{
    [SerializeField] private Image m_ShadowImage;
    [SerializeField] private float m_ShadowAlpha = 0.35f;

    private Color _defaultColor;
    private Color _pressColor;

    public void Initialize()
    {
        _defaultColor = m_ShadowImage.color;
        _pressColor = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, m_ShadowAlpha);
    }

    public void SetShadow(bool apply)
    {
        m_ShadowImage.color = apply ? _pressColor : _defaultColor;
    }
}
