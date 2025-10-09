using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class KeyMapDetector
{
    #region Static
    private static bool _initialized = false;

    private static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
    }
    #endregion

    #region Interface
    public KeyMapDetector()
    {
        Initialize();
    }

    public UniTask<InputKeyMap?> GetKeyMapAsync(CancellationToken token = default, params KeyCode[] cancelKeyCodes)
    {
        _cancelKeyCodes = cancelKeyCodes;
        return Detect(token);
    }
    #endregion

    #region Privates
    private KeyCode[] _cancelKeyCodes;
    private readonly object _blocker = new();

    private async UniTask<InputKeyMap?> Detect(CancellationToken token)
    {
        HashSet<ModifierKeyCode> modifiers = new();
        ActionKeyCode actionKey = ActionKeyCode.None;
        InputManager.AddBlocker(_blocker);

        try
        {
            while (!token.IsCancellationRequested && !IsCancelKeyPressed())
            {
                foreach (ModifierKeyCode modifier in InputManagerUtil.GetAllModifiers())
                {
                    if (modifier.GetKey())
                    {
                        modifiers.Add(modifier);
                        continue;
                    }

                    modifiers.Remove(modifier);
                }

                foreach (ActionKeyCode action in InputManagerUtil.GetAllActionKeys())
                {
                    if (action.GetKeyDown())
                    {
                        actionKey = action;
                    }
                }

                if (actionKey == ActionKeyCode.None)
                {
                    await UniTask.Yield(token);
                    continue;
                }

                return new InputKeyMap(actionKey, modifiers);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            InputManager.RemoveBlocker(_blocker);
        }
    }

    private bool IsCancelKeyPressed()
    {
        if (_cancelKeyCodes == null || _cancelKeyCodes.Length == 0)
        {
            return false;
        }

        foreach (KeyCode keyCode in _cancelKeyCodes)
        {
            if (Input.GetKeyDown(keyCode))
            {
                return true;
            }
        }

        return false;
    }
    #endregion
}