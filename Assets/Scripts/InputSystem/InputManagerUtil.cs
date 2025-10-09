using System;
using UnityEngine;

public static class InputManagerUtil
{
    #region Interface
    public static bool GetKeyDown(this ActionKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyDown));
    }

    public static bool GetKeyUp(this ActionKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyUp, false));
    }

    public static bool GetKey(this ActionKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKey));
    }

    public static bool GetKeyDown(this ModifierKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyDown));
    }

    public static bool GetKeyUp(this ModifierKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyUp));
    }

    public static bool GetKey(this ModifierKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKey));
    }

    public static bool AnyKeyDown()
    {
        bool anyKeyDown = Input.anyKeyDown;

        if (!anyKeyDown)
        {
            anyKeyDown = ActionKeyCode.MouseWheelDown.GetKeyDown() || ActionKeyCode.MouseWheelUp.GetKeyDown();
        }

        return anyKeyDown;
    }

    public static bool AnyKey()
    {
        bool anyKey = Input.anyKey;

        if (!anyKey)
        {
            anyKey = ActionKeyCode.MouseWheelDown.GetKey() || ActionKeyCode.MouseWheelUp.GetKey();
        }

        return anyKey;
    }

    public static ModifierKeyCode[] GetAllModifier()
    {
        return _allModifier ??= new[]
        {
            ModifierKeyCode.LeftShift, ModifierKeyCode.RightShift,
            ModifierKeyCode.LeftControl, ModifierKeyCode.RightControl,
            ModifierKeyCode.LeftAlt, ModifierKeyCode.RightAlt,
            ModifierKeyCode.LeftWindows, ModifierKeyCode.RightWindows
        };
    }

    public static KeyCode AsUnityKeyCode(this ActionKeyCode target) => (KeyCode)(int)target;
    public static KeyCode AsUnityKeyCode(this ModifierKeyCode target) => (KeyCode)(int)target;
    #endregion

    #region Privates
    private static bool GetKeyState(ActionKeyCode target, Predicate<ActionKeyCode> predicate) => predicate?.Invoke(target) ?? false;

    private static bool GetKeyState(ModifierKeyCode target, Predicate<ModifierKeyCode> predicate) => predicate?.Invoke(target) ?? false;

    private static Predicate<ActionKeyCode> GetKeyPredicate(ActionKeyCode target, Predicate<KeyCode> defaultInputPredicate, bool wheelDetect = true)
    {
        return target switch
        {
            ActionKeyCode.MouseWheelUp or ActionKeyCode.MouseWheelDown => wheelChecker,
            _ => inputKeyCode => defaultInputPredicate?.Invoke(AsUnityKeyCode(inputKeyCode)) ?? false
        };

        bool wheelChecker(ActionKeyCode inputKeyCode)
        {
            if (!wheelDetect)
            {
                return false;
            }

            float scrollY = Input.mouseScrollDelta.y;
            return inputKeyCode switch
            {
                ActionKeyCode.MouseWheelDown => scrollY < 0,
                ActionKeyCode.MouseWheelUp => scrollY > 0,
                _ => false
            };
        }
    }

    private static Predicate<ModifierKeyCode> GetKeyPredicate(ModifierKeyCode target, Predicate<KeyCode> defaultInputPredicate)
    {
        return inputKeyCode => defaultInputPredicate?.Invoke(AsUnityKeyCode(inputKeyCode)) ?? false;
    }
    #endregion

    private static ModifierKeyCode[] _allModifier;
}