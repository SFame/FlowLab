using UnityEngine;
using UnityEngine.Serialization;

public class MainCameraGetter : MonoBehaviour
{
    #region Privates
    [SerializeField] private Camera m_MainCamera;
    [FormerlySerializedAs("m_MainCameraController")] [SerializeField] private CameraController m_CameraController;

    private static Camera _main;
    private static CameraController _cameraController;

    private void Awake()
    {
        if (m_MainCamera == null)
        {
            Debug.LogError($"{GetType().Name}: MainCamera must be assigned");
            return;
        }

        if (m_CameraController == null)
        {
            Debug.LogError($"{GetType().Name}: MainCameraController must be assigned");
            return;
        }

        _main = m_MainCamera;
        _cameraController = m_CameraController;
    }
    #endregion

    #region Interface
    public static Camera GetMainCam() => _main;
    public static CameraController GetController() => _cameraController;
    #endregion
}