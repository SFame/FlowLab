using UnityEngine;

public class WorldCanvasGetter : MonoBehaviour
{
    #region Privates
    [SerializeField] private Canvas m_RootCanvas;
    [SerializeField] private PUMPSeparator m_PumpSeparator;

    private void Awake()
    {
        if (m_RootCanvas == null)
        {
            Debug.LogError($"{GetType().Name}: RootCanvas must be assigned");
            return;
        }

        if (m_PumpSeparator == null)
        {
            Debug.LogError($"{GetType().Name}: PumpSeparator must be assigned");
            return;
        }

        RootCanvas = m_RootCanvas;
        RootCanvasRect = m_RootCanvas.GetComponent<RectTransform>();
        PumpSeparator = m_PumpSeparator;
    }
    #endregion

    #region Interface
    public static Canvas RootCanvas { get; private set; }
    public static RectTransform RootCanvasRect { get; private set; }
    public static PUMPSeparator PumpSeparator { get; private set; }
    #endregion
}