using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// MonoBehaviour Component
/// </summary>
public class PUMPInputManager : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private List<BackgroundActionKeyMap> m_KeyMap;
    [SerializeField] private bool m_Enable = true;
    #endregion

    #region Interface
    public static PUMPInputManager Current { get; private set; }
    public void AddBlocker(object blocker) => _blocker.Add(blocker);
    public void SubBlocker(object blocker) => _blocker.Remove(blocker);
    #endregion

    #region Privates
    private readonly HashSet<object> _blocker = new();
    private void Update()
    {
        if (!m_Enable)
            return;

        if (_blocker.Count > 0)
            return;

        for (int i = 0; i < m_KeyMap.Count; i++)
        {
            if (m_KeyMap[i].Run()) // 1프레임당 1회 호출 제한
                break;
        }
    }

    private void Awake()
    {
        if (Current != null)
        {
            Destroy(Current.gameObject);
        }

        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }
    #endregion
}

/// <summary>
/// KeyMap, Action Pair struct & Logic
/// </summary>
[Serializable]
public class BackgroundActionKeyMap
{
    #region OnInspector
    [SerializeField, Tooltip("Action Type")] private BackgroundActionType m_ActionType;
    [SerializeField, Tooltip("Shift, Ctrl, Alt...")] private List<KeyCode> m_Modifiers;
    [SerializeField, Tooltip("Character Key")] private List<KeyCode> m_ActionKeys;
    #endregion

    #region Interface
    public bool Run()
    {
        if (m_ActionType != _prevType)
        {
            _lastKey = null;
            RefreshAction();
            _prevType = m_ActionType;
        }

        if (_action == null)
        {
            _lastKey = null;
            return false;
        }

        return StateCheck();
    }
    #endregion

    #region Privates
    private Action _action;
    private BackgroundActionType _prevType;
    private KeyCode? _lastKey = null;

    private Pool<List<KeyCode>> _pressKeysPool;
    private Pool<List<KeyCode>> _actionKeysCopyPool;
    private Pool<List<KeyCode>> PressKeysPool
    {
        get
        {
            _pressKeysPool ??= new Pool<List<KeyCode>>
            (
                createFunc: () => new List<KeyCode>(),
                initSize: 500,
                actionOnGet: keyCodes =>
                {
                    if (keyCodes.Count != 0) // 무결성을 위한 Clear
                        keyCodes.Clear ();

                    foreach (KeyCode key in m_ActionKeys)
                    {
                        if (Input.GetKey(key) && !Input.GetKeyDown(key))
                            keyCodes.Add(key);
                    }
                },
                actionOnRelease: keyCodes => keyCodes.Clear()
            );
            return _pressKeysPool;
        }
    }

    private Pool<List<KeyCode>> ActionKeysCopyPool
    {
        get
        {
            _actionKeysCopyPool ??= new Pool<List<KeyCode>>
            (
                createFunc: () => new List<KeyCode>(),
                initSize: 500,
                actionOnGet: keyCodes =>
                {
                    if (keyCodes.Count != 0) // 무결성을 위한 Clear
                        keyCodes.Clear();

                    keyCodes.AddRange(m_ActionKeys);
                },
                actionOnRelease: keyCodes => keyCodes.Clear()
            );
            return _actionKeysCopyPool;
        }
    }

    private bool IsKeyMapInvalid => m_Modifiers == null || m_ActionKeys == null || m_ActionKeys.Count == 0;

    private bool StateCheck()
    {
        if (IsKeyMapInvalid)
            return false;

        if (m_Modifiers.Count == 0 || m_Modifiers.All(Input.GetKey))
        {
            bool result = false;
            if (_lastKey != null && Input.GetKeyDown(_lastKey.Value))
            {
                _action?.Invoke();
                result = true;
            }

            _lastKey = SubCacheAndGetResult();
            return result;
        }

        _lastKey = null;
        return false;
    }

    private void RefreshAction()
    {
        _action = BackgroundActionMapper.GetAction(m_ActionType);
    }

    private KeyCode? SubCacheAndGetResult()
    {
        List<KeyCode> actionKeysCopy = ActionKeysCopyPool.Get();
        List<KeyCode> pressingList = PressKeysPool.Get();

        foreach (KeyCode pressing in pressingList)
        {
            actionKeysCopy.Remove(pressing);
        }

        KeyCode? result = actionKeysCopy.Count == 1 ? actionKeysCopy[0] : null;

        ActionKeysCopyPool.Release(actionKeysCopy);
        PressKeysPool.Release(pressingList);

        return result;
    }
    #endregion
}

/// <summary>
/// Action Type
/// </summary>
public enum BackgroundActionType
{
    NoAction,
    Undo,
    Redo,
    DragDelete,
    DragDisconnect,
}

/// <summary>
/// Enum <-> Action 매핑
/// </summary>
public static class BackgroundActionMapper
{
    public static Action GetAction(BackgroundActionType actionEnum) => actionEnum switch
    {
        BackgroundActionType.Undo => UndoAction,
        BackgroundActionType.Redo => RedoAction,
        BackgroundActionType.DragDelete => DragDeleteAction,
        BackgroundActionType.DragDisconnect => DragDisconnectAction,
        BackgroundActionType.NoAction => null,
        _ => null
    };

    #region Actions
    private static void UndoAction()
    {
        PUMPBackground.Current?.Undo();
    }

    private static void RedoAction()
    {
        PUMPBackground.Current?.Redo();
    }

    private static void DragDeleteAction()
    {
        PUMPBackground.Current?.DestroyDraggables();
    }

    private static void DragDisconnectAction()
    {
        PUMPBackground.Current?.DisconnectDraggables();
    }
    #endregion
}
