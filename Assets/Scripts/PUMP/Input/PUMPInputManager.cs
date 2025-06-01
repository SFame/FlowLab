using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OdinSerializer;
using Utils;
using System.Text;

/// <summary>
/// MonoBehaviour Component
/// </summary>
public class PUMPInputManager : MonoBehaviour
{
    #region Interface
    public static PUMPInputManager Current
    {
        get => _current;
        private set
        {
            _current = value;
            OnCurrentUpdated?.Invoke(_current);
        }
    }

    public static event Action<PUMPInputManager> OnCurrentUpdated;

    public List<BackgroundActionKeyMap> KeyMap { get; } = new();

    public bool Enable { get; set; } = true;

    public void AddBlocker(object blocker) => _blocker.Add(blocker);

    public void RemoveBlocker(object blocker) => _blocker.Remove(blocker);

    public void SortKeyMap()
    {
        KeyMap.Sort((a, b) =>
        {
            if (a.m_Modifiers == null && b.m_Modifiers == null)
                return 0;

            if (a.m_Modifiers == null)
                return 1;

            if (b.m_Modifiers == null)
                return -1;

            if (a.m_Modifiers.Count < b.m_Modifiers.Count)
                return 1;

            if (a.m_Modifiers.Count > b.m_Modifiers.Count)
                return -1;

            return 0;
        });
    }
    #endregion

    #region Privates / Protected
    protected virtual void Initialize() { }
    protected virtual void Terminate() { }

    private readonly HashSet<object> _blocker = new();
    private static PUMPInputManager _current;

    private void Update()
    {
        if (!Enable)
            return;

        if (_blocker.Count > 0)
            return;

        for (int i = 0; i < KeyMap.Count; i++)
        {
            if (KeyMap[i].Run()) // 1프레임당 1회 호출 제한
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

        Initialize();
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }

        Terminate();
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
    SelectAll,
    SelectDelete,
    SelectDisconnect,
}

/// <summary>
/// KeyMap, Action Pair struct & Logic
/// </summary>
[Serializable]
public class BackgroundActionKeyMap
{
    #region OnInspector
    [SerializeField, OdinSerialize, Tooltip("Action Type")] public BackgroundActionType m_ActionType;
    [SerializeField, OdinSerialize, Tooltip("Shift, Ctrl, Alt...")] public List<KeyCode> m_Modifiers;
    [SerializeField, OdinSerialize, Tooltip("Character Key")] public List<KeyCode> m_ActionKeys;
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

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"[{m_ActionType}] ");

        if (m_Modifiers != null && m_Modifiers.Count > 0)
        {
            sb.Append(string.Join(" + ", m_Modifiers.Select(m => m.ToString())));
            sb.Append(" + ");
        }

        if (m_ActionKeys != null && m_ActionKeys.Count > 0)
        {
            if (m_ActionKeys.Count == 1)
            {
                sb.Append(m_ActionKeys[0].ToString());
            }
            else
            {
                sb.Append($"({string.Join(" | ", m_ActionKeys.Select(a => a.ToString()))})");
            }
        }
        else
        {
            sb.Append("(No Action Keys)");
        }

        return sb.ToString();
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

    private bool IsKeyMapInvalid => m_ActionKeys == null || m_ActionKeys.Count == 0;

    private bool StateCheck()
    {
        if (IsKeyMapInvalid)
            return false;

        if (m_Modifiers == null || m_Modifiers.Count == 0 || m_Modifiers.All(Input.GetKey))
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
/// Enum <-> Action 매핑
/// </summary>
public static class BackgroundActionMapper
{
    public static Action GetAction(BackgroundActionType actionEnum) => actionEnum switch
    {
        BackgroundActionType.Undo => UndoAction,
        BackgroundActionType.Redo => RedoAction,
        BackgroundActionType.SelectAll => SelectAll,
        BackgroundActionType.SelectDelete => SelectDeleteAction,
        BackgroundActionType.SelectDisconnect => SelectDisconnectAction,
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

    private static void SelectAll()
    {
        PUMPBackground.Current?.SelectAll();
    }

    private static void SelectDeleteAction()
    {
        PUMPBackground.Current?.DestroySelected();
    }

    private static void SelectDisconnectAction()
    {
        PUMPBackground.Current?.DisconnectSelected();
    }
    #endregion
}