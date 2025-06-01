using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class KeyMapDetector
{
    #region Static
    private static KeyCode[] _allKeys;
    private static bool _initialized = false;

    private static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    }
    #endregion

    #region Interface
    public KeyMapDetector(BackgroundActionType actionType)
    {
        _actionType = actionType;
    }

    public UniTask<BackgroundActionKeyMap> GetKeyMapAsync(CancellationToken token = default)
    {
        return Detect(token);
    }
    #endregion

    #region Privates
    private readonly BackgroundActionType _actionType;

    private async UniTask<BackgroundActionKeyMap> Detect(CancellationToken token)
    {
        Initialize();

        HashSet<KeyCode> modifiers = new();
        HashSet<KeyCode> actions = new();

        bool detectStart = false;
        try
        {
            while (!token.IsCancellationRequested)
            {
                bool isAnyKeyPressed = IsAnyKeyboardKeyPressed();

                if (!isAnyKeyPressed && detectStart)
                {
                    if (actions.Count <= 0)
                    {
                        return null;
                    }

                    return new BackgroundActionKeyMap()
                        { m_Modifiers = modifiers.ToList(), m_ActionKeys = actions.ToList(), m_ActionType = _actionType };
                }

                if (isAnyKeyPressed && !detectStart)
                {
                    detectStart = true;
                }

                foreach (KeyCode key in _allKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        if (IsModifierKey(key))
                        {
                            modifiers.Add(key);
                            continue;
                        }

                        if (IsActionKey(key))
                        {
                            actions.Add(key);
                        }
                    }
                }

                await UniTask.Yield(token);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private bool IsActionKey(KeyCode keyCode)
    {
        if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            return true;

        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            return true;

        if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F15)
            return true;

        if (keyCode == KeyCode.UpArrow || keyCode == KeyCode.DownArrow ||
            keyCode == KeyCode.LeftArrow || keyCode == KeyCode.RightArrow)
            return true;

        switch (keyCode)
        {
            case KeyCode.Space:
            case KeyCode.Return:
            case KeyCode.Escape:
            case KeyCode.Tab:
            case KeyCode.Backspace:
            case KeyCode.Delete:
            case KeyCode.Insert:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.Print:
                return true;
        }

        return false;
    }

    private bool IsModifierKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.LeftShift:
            case KeyCode.RightShift:

            case KeyCode.LeftControl:
            case KeyCode.RightControl:

            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
            case KeyCode.AltGr:

            case KeyCode.LeftMeta:
            case KeyCode.RightMeta:
            case KeyCode.LeftWindows:
            case KeyCode.RightWindows:
                return true;

            default:
                return false;
        }
    }

    private bool IsAnyKeyboardKeyPressed()
    {
        foreach (KeyCode keyCode in _allKeys)
        {
            if (IsMouseKey(keyCode))
                continue;

            if (Input.GetKey(keyCode))
                return true;
        }
        return false;
    }

    private bool IsMouseKey(KeyCode keyCode)
    {
        return keyCode is >= KeyCode.Mouse0 and <= KeyCode.Mouse6;
    }
    #endregion
}