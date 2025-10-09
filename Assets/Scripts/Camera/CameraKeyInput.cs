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
    private float _scrollDelta = 0.0f;

    public CameraActionLauncher(CameraController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException();
        }

        _controller = controller;
    }

    public void InjectScrollDelta(float scrollDelta)
    {
        _scrollDelta = scrollDelta;
    }

    public void LaunchAction(CameraActionType type)
    {
        switch (type)
        {
            case CameraActionType.Up:
                UpAction();
                return;
            case CameraActionType.Down:
                DownAction();
                return;
            case CameraActionType.Left:
                LeftAction();
                return;
            case CameraActionType.Right:
                RightAction();
                return;
            case CameraActionType.Drag:
                DragAction();
                return;
            case CameraActionType.ZoomActive:
                ZoomActiveAction();
                return;
            case CameraActionType.HorizontalMoveActive:
                HorizontalMoveActiveAction();
                return;
            case CameraActionType.VerticalMoveActive:
                VerticalMoveActiveAction();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private float GetScrollDelta()
    {
        float temp = _scrollDelta;
        _scrollDelta = 0.0f;
        return temp;
    }

    private void ZoomIn()
    {
        _controller.ZoomIn();
    }

    private void ZoomOut()
    {
        _controller.ZoomOut();
    }

    private void UpAction()
    {
        _controller.MovePosition(Vector2.up);
    }

    private void DownAction()
    {
        _controller.MovePosition(Vector2.down);
    }

    private void LeftAction()
    {
        _controller.MovePosition(Vector2.left);
    }

    private void RightAction()
    {
        _controller.MovePosition(Vector2.right);
    }

    private void DragAction()
    {
        Vector2 worldDelta = _controller.Camera.ScreenToWorldPoint(Input.mousePositionDelta) -
                             _controller.Camera.ScreenToWorldPoint(Vector3.zero);

        _controller.MovePositionAbsolutely(-worldDelta);
    }

    private void ZoomActiveAction()
    {
        float scrollDelta = GetScrollDelta();
        if (scrollDelta > 0.5f)
        {
            ZoomIn();
        }
        else if (scrollDelta < -0.5f)
        {
            ZoomOut();
        }
    }

    private void HorizontalMoveActiveAction()
    {
        float scrollDelta = GetScrollDelta();
        if (scrollDelta > 0.5f)
        {
            _controller.MovePosition(Vector2.left, SCROLL_MOVE_WEIGHT);
            LeftAction();
        }
        else if (scrollDelta < -0.5f)
        {
            _controller.MovePosition(Vector2.right, SCROLL_MOVE_WEIGHT);
            RightAction();
        }
    }

    private void VerticalMoveActiveAction()
    {
        float scrollDelta = GetScrollDelta();
        if (scrollDelta > 0.5f)
        {
            _controller.MovePosition(Vector2.up, SCROLL_MOVE_WEIGHT);
        }
        else if (scrollDelta < -0.5f)
        {
            _controller.MovePosition(Vector2.down, SCROLL_MOVE_WEIGHT);
            DownAction();
        }
    }
}

public enum CameraActionType
{
    Up,
    Down,
    Left,
    Right,
    Drag,
    ZoomActive,
    HorizontalMoveActive,
    VerticalMoveActive
}