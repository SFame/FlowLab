using UnityEngine;

public class PUMPComponentGetter : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private PUMPTool m_PumpTool;
    [SerializeField] private PUMPSaveLoadPanel m_PumpSaveLoadPanel;
    #endregion

    #region Interface
    public PUMPTool PumpTool => m_PumpTool;
    public PUMPSaveLoadPanel PumpSaveLoadPanel => m_PumpSaveLoadPanel;

    #endregion
}