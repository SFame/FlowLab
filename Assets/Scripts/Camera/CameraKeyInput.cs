using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraKeyInput : MonoBehaviour
{
    [SerializeField] private CameraController m_CameraController;
    [SerializeField] private List<CameraKeymap> m_KeyMaps;

    private CameraActionLauncher _launcher;
    private List<CameraKeymap> _alwaysCache = new();

    private CameraActionLauncher Launcher => _launcher ??= new CameraActionLauncher(m_CameraController);

    //private void Update()
    //{
    //    _alwaysCache.Clear();
    //    Launcher.InjectScrollDelta(Input.mouseScrollDelta.y);

    //    foreach (CameraKeymap keyMap in m_KeyMaps)
    //    {
    //        if (keyMap.Always)
    //        {
    //            _alwaysCache.Add(keyMap);
    //            continue;
    //        }

    //        if (Input.GetKey(keyMap.KeyCode))
    //        {
    //            Launcher.LaunchAction(keyMap.ActionType);
    //        }
    //    }

    //    foreach (CameraKeymap alwaysKeymap in _alwaysCache)
    //    {
    //        Launcher.LaunchAction(alwaysKeymap.ActionType);
    //    }
    //}
}

[Serializable]
public struct CameraKeymap
{
    [SerializeField] public CameraActionType ActionType;
    [SerializeField] public InputKeyMap KeyMap;
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
            case CameraActionType.LeftMove:
                LeftMove();
                return;
            case CameraActionType.RightMove:
                RightMove();
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
    LeftMove,
    RightMove,
    UpMove,
    DownMove,
}