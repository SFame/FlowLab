using UnityEngine;

public class PUMPComponentGetter : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private PUMPTool m_PumpTool;
    #endregion

    #region Interface
    public PUMPTool PumpTool => m_PumpTool;
    #endregion
}