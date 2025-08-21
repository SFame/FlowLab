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

    public static Bounds GetBounds()
    {
        Vector3[] worldCorners = new Vector3[4];
        RootCanvasRect.GetWorldCorners(worldCorners);

        Vector3 min = worldCorners[0];
        Vector3 max = worldCorners[0];

        for (int i = 1; i < 4; i++)
        {
            min = Vector3.Min(min, worldCorners[i]);
            max = Vector3.Max(max, worldCorners[i]);
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        return new Bounds(center, size);
    }
    #endregion
}