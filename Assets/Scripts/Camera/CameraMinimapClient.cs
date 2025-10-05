using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraMinimapClient : MonoBehaviour, IMinimapProxyClient
{
    #region OnInspector
    [SerializeField] private CameraController m_Controller;
    [SerializeField] private Sprite m_MinimapSprite;
    [SerializeField] private Color m_MinimapColor;
    #endregion

    public event Action<Vector2> OnClientMove;
    public event Action<Vector2> OnClientSizeUpdate;
    public event Action<Color> OnClientColorUpdate;
    public event Action OnClientDestroy;
    public event Action<bool> OnActiveStateChanged;
    string IMinimapProxyClient.MirrorName => "Camera";
    Vector2 IMinimapProxyClient.CurrentWorldPosition => m_Controller.GetCameraPosition();
    float IMinimapProxyClient.OrderZ => -1.0f;
    float IMinimapProxyClient.RotationZ => 0f;
    Vector2 IMinimapProxyClient.DefaultSize => m_Controller.GetCameraSize();
    Sprite IMinimapProxyClient.Sprite => m_MinimapSprite;
    Color IMinimapProxyClient. SpriteDefaultColor => m_MinimapColor;

    #region Privates
    private void Awake()
    {
        AwakeAsync().Forget();
    }

    private async UniTaskVoid AwakeAsync()
    {
        await UniTask.Yield();
        m_Controller.OnCameraMove += position => OnClientMove?.Invoke(position);
        m_Controller.OnCameraSizeUpdate += size => OnClientSizeUpdate?.Invoke(size);
        MinimapProxy.Register(this);
    }

    private void OnDestroy()
    {
        OnClientDestroy?.Invoke();
    }

    private void OnEnable()
    {
        OnActiveStateChanged?.Invoke(true);
    }

    private void OnDisable()
    {
        OnActiveStateChanged?.Invoke(false);
    }
    #endregion
}
