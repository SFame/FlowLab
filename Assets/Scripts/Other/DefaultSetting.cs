using UnityEngine;

public class DefaultSetting : MonoBehaviour
{
    [SerializeField] private int m_ScreenWidth = 1920;
    [SerializeField] private int m_ScreenHeight = 1080;

    private void Awake()
    {
        Screen.SetResolution(m_ScreenWidth, m_ScreenHeight, FullScreenMode.FullScreenWindow);
    }
}
