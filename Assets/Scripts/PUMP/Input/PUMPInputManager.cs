using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OdinSerializer;
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

    public void AddBlocker(object blocker) => InputManager.AddBlocker(blocker);

    public void RemoveBlocker(object blocker) => InputManager.RemoveBlocker(blocker);


    #endregion

    #region Privates / Protected
    protected virtual void Initialize() { }
    protected virtual void Terminate() { }

    private static PUMPInputManager _current;

    

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
    OpenPalette,
    OpenSaveLoadPanel,
    SelectAll,
    SelectDelete,
    SelectDisconnect,
    MinimapToggle,
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
        BackgroundActionType.OpenPalette => OpenPaletteAction,
        BackgroundActionType.OpenSaveLoadPanel => OpenSaveLoadPanelAction,
        BackgroundActionType.SelectAll => SelectAllAction,
        BackgroundActionType.SelectDelete => SelectDeleteAction,
        BackgroundActionType.SelectDisconnect => SelectDisconnectAction,
        BackgroundActionType.MinimapToggle => MinimapToggleAction,
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
    #endregion
}