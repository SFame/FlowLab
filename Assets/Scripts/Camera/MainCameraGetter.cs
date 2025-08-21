using UnityEngine;

public class MainCameraGetter : MonoBehaviour
{
    #region Privates
    [SerializeField] private Camera m_MainCamera;
    [SerializeField] private MainCameraController m_MainCameraController;

    private static Camera _main;
    private static MainCameraController _mainCameraController;

    private void Awake()
    {
        if (m_MainCamera == null)
        {
            Debug.LogError($"{GetType().Name}: MainCamera must be assigned");
            return;
        }

        if (m_MainCameraController == null)
        {
            Debug.LogError($"{GetType().Name}: MainCameraController must be assigned");
            return;
        }

        _main = m_MainCamera;
        _mainCameraController = m_MainCameraController;
    }
    #endregion

    #region Interface
    public static Camera GetMainCam() => _main;
    public static MainCameraController GetController() => _mainCameraController;
    #endregion
}