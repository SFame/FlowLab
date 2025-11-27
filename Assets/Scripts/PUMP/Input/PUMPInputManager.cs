using System;
using System.Collections.Generic;
using UnityEngine;
using OdinSerializer;

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

    public void AddBlocker(object blocker) => InputManager.AddBlocker(blocker);

    public void RemoveBlocker(object blocker) => InputManager.RemoveBlocker(blocker);
    #endregion

    #region Privates / Protected
    private static PUMPInputManager _current;
    private readonly List<InputKeyMap> _cacheKeymap = new();
    protected virtual void Initialize() { }
    protected virtual void Terminate() { }

    protected void Refresh()
    {
        Unsubscribe();
        _cacheKeymap!.Clear();

        foreach (BackgroundActionKeyMap backgroundActionKeyMap in KeyMap)
        {
            _cacheKeymap.Add(backgroundActionKeyMap.m_KeyMap);

            InputKeyMapArgs args = new InputKeyMapArgs()
            {
                Name = $"Background_{backgroundActionKeyMap.m_ActionType.ToString()}",
                Callback = _ => BackgroundActionMapper.GetAction(backgroundActionKeyMap.m_ActionType)?.Invoke(),
                OnRemove = backgroundActionKeyMap.m_OnRemove,
                ActionHold = backgroundActionKeyMap.m_ActionHold,
                Immutable = false,
            };
            InputManager.Subscribe(backgroundActionKeyMap.m_KeyMap, args);
        }
    }

    protected void Unsubscribe()
    {
        foreach (InputKeyMap keyMap in _cacheKeymap)
        {
            InputManager.Unsubscribe(keyMap);
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
        Refresh();
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }

        Terminate();
        Unsubscribe();
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
    Copy,
    Cut,
    Paste,
    ToggleSnapMode,
    OpenPalette,
    OpenSaveLoadPanel,
    SelectAll,
    SelectDelete,
    SelectDisconnect,
    MinimapToggle,
    OpenConsole,
}

/// <summary>
/// KeyMap, Action Pair struct & Logic
/// </summary>
[Serializable]
public class BackgroundActionKeyMap
{
    #region OnInspector
    [SerializeField, OdinSerialize, Tooltip("Action Type")] public BackgroundActionType m_ActionType;
    [SerializeField, OdinSerialize, Tooltip("Key Map")] public InputKeyMap m_KeyMap;
    [SerializeField, OdinSerialize, Tooltip("Hold")] public bool m_ActionHold;
    [NonSerialized, Tooltip("On Remove")] public Action<InputKeyMap> m_OnRemove;
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
        BackgroundActionType.Copy => CopyAction,
        BackgroundActionType.Cut => CutAction,
        BackgroundActionType.Paste => PasteAction,
        BackgroundActionType.ToggleSnapMode => ToggleSnapModeAction,
        BackgroundActionType.OpenPalette => OpenPaletteAction,
        BackgroundActionType.OpenSaveLoadPanel => OpenSaveLoadPanelAction,
        BackgroundActionType.SelectAll => SelectAllAction,
        BackgroundActionType.SelectDelete => SelectDeleteAction,
        BackgroundActionType.SelectDisconnect => SelectDisconnectAction,
        BackgroundActionType.MinimapToggle => MinimapToggleAction,
        BackgroundActionType.OpenConsole => OpenConsoleAction,
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

    private static void CopyAction()
    {
        PUMPBackground.Current?.CopySelected();
    }

    private static void CutAction()
    {
        PUMPBackground.Current?.CutSelected();
    }

    private static void PasteAction()
    {
        PUMPBackground.Current?.Paste();
    }

    private static void ToggleSnapModeAction()
    {
        LineEdgeSortingManager.Activate = !LineEdgeSortingManager.Activate;
    }

    private static void OpenPaletteAction()
    {
        PUMPBackground.Current?.ComponentGetter?.PumpTool?.TogglePalette();
    }

    private static void OpenSaveLoadPanelAction()
    {
        PUMPBackground.Current?.ComponentGetter?.PumpTool?.ToggleSaveLoadPanel();
    }

    private static void SelectAllAction()
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

    private static void MinimapToggleAction()
    {
        PUMPBackground.Current?.ComponentGetter?.PumpTool?.ToggleMinimap();
    }

    private static void OpenConsoleAction()
    {
        if (ConsoleWindow.IsOpen)
        {
            ConsoleWindow.SetFocus(false);
            ConsoleWindow.IsOpen = false;
            return;
        }

        ConsoleWindow.IsOpen = true;
        ConsoleWindow.SetFocus(true);
    }
    #endregion
}