using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraKeyInput : MonoBehaviour
{
    [SerializeField] private CameraController m_CameraController;
    [SerializeField] private List<CameraKeymap> m_KeyMaps;

    private Dictionary<InputKeyMap, Action> _keyMapsDict;
    private CameraActionLauncher _launcher;
    private CameraActionLauncher Launcher => _launcher ??= new CameraActionLauncher(m_CameraController);

    private Dictionary<InputKeyMap, Action> AsKeyMap(List<CameraKeymap> camKeyMaps)
    {
        Dictionary<InputKeyMap, Action> keyMaps = new();
        foreach (CameraKeymap camKeymap in camKeyMaps)
        {
            InputKeyMap keyMap = new InputKeyMap(camKeymap.ActionKey, camKeymap.ModifierKeys.ToHashSet(), true, camKeymap.ActionHold);
            if (!keyMaps.TryAdd(keyMap, () => Launcher.LaunchAction(camKeymap.ActionType)))
            {
                Debug.LogWarning($"CameraKeyInput: {camKeymap.ActionType.ToString()} 중복됨");
            }
        }

        return keyMaps;
    }

    private void Awake()
    {
        if (m_KeyMaps == null)
        {
            Debug.Log("CameraKeymap 할당 필요");
            return;
        }

        _keyMapsDict = AsKeyMap(m_KeyMaps);

        foreach (var(keyMap, action) in _keyMapsDict)
        {
            InputManager.Subscribe(keyMap, action);
        }
    }

    private void OnDestroy()
    {
        if (_keyMapsDict == null)
        {
            return;
        }

        foreach (var (keyMap, _) in _keyMapsDict)
        {
            InputManager.Unsubscribe(keyMap);
        }
    }
}

[Serializable]
public struct CameraKeymap
{
    [SerializeField] public CameraActionType ActionType;
    [SerializeField] public ActionKeyCode ActionKey;
    [SerializeField] public List<ModifierKeyCode> ModifierKeys;
    [SerializeField] public bool ActionHold;
}

public class CameraActionLauncher
{
    private const float SCROLL_MOVE_WEIGHT = 4.0f;
    private readonly CameraController _controller;

    public CameraActionLauncher(CameraController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException();
        }

        _controller = controller;
    }

    public void LaunchAction(CameraActionType type)
    {
        switch (type)
        {
            case CameraActionType.Up:
                UpMove();
                return;
            case CameraActionType.Down:
                DownMove();
                return;
            case CameraActionType.Left:
                LeftMove();
                return;
            case CameraActionType.Right:
                RightMove();
                return;
            case CameraActionType.Drag:
                DragAction();
                return;
            case CameraActionType.ZoomIn:
                ZoomIn();
                return;
            case CameraActionType.ZoomOut:
                ZoomOut();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void ZoomIn()
    {
        _controller.ZoomIn();
    }

    private void ZoomOut()
    {
        _controller.ZoomOut();
    }

    private void UpMove()
    {
        _controller.MovePosition(Vector2.up);
    }

    private void DownMove()
    {
        _controller.MovePosition(Vector2.down);
    }

    private void LeftMove()
    {
        _controller.MovePosition(Vector2.left);
    }

    private void RightMove()
    {
        _controller.MovePosition(Vector2.right);
    }

    private void DragAction()
    {
        Vector2 worldDelta = _controller.Camera.ScreenToWorldPoint(Input.mousePositionDelta) -
                             _controller.Camera.ScreenToWorldPoint(Vector3.zero);

        _controller.MovePositionAbsolutely(-worldDelta);
    }
}

public enum CameraActionType
{
    Up,
    Down,
    Left,
    Right,
    Drag,
    ZoomIn,
    ZoomOut,
}