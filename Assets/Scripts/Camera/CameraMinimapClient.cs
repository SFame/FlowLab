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
    public event Action OnClientDestroy;
    public event Action<bool> OnActiveStateChanged;
    public Vector2 CurrentWorldPosition => m_Controller.GetCameraPosition();
    public float OrderZ => -1.0f;
    public Vector2 Size => m_Controller.GetCameraSize();
    public Sprite Sprite => m_MinimapSprite;
    public Color SpriteColor => m_MinimapColor;

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
