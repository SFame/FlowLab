using System;
using UnityEngine;

public static class InputManagerUtil
{
    #region Interface
    public static bool GetKeyDown(this InputKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyDown));
    }

    public static bool GetKeyUp(this InputKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKeyUp, false));
    }

    public static bool GetKey(this InputKeyCode find)
    {
        return GetKeyState(find, GetKeyPredicate(find, Input.GetKey));
    }

    public static bool AnyKeyDown()
    {
        bool anyKeyDown = Input.anyKeyDown;

        if (!anyKeyDown)
        {
            anyKeyDown = InputKeyCode.MouseWheelDown.GetKeyDown() || InputKeyCode.MouseWheelUp.GetKeyDown();
        }

        return anyKeyDown;
    }

    public static bool AnyKey()
    {
        bool anyKey = Input.anyKey;

        if (!anyKey)
        {
            anyKey = InputKeyCode.MouseWheelDown.GetKey() || InputKeyCode.MouseWheelUp.GetKey();
        }

        return anyKey;
    }

    public static bool InModifierKey(this InputKeyCode target) => target switch
    {
        InputKeyCode.LeftShift or
        InputKeyCode.RightShift or
        InputKeyCode.LeftControl or
        InputKeyCode.RightControl or
        InputKeyCode.LeftAlt or
        InputKeyCode.RightAlt or
        InputKeyCode.LeftWindows or
        InputKeyCode.RightWindows => true,
        _ => false
    };

    public static InputKeyCode[] GetAllModifier()
    {
        return _allModifier ??= new[]
        {
            InputKeyCode.LeftShift, InputKeyCode.RightShift,
            InputKeyCode.LeftControl, InputKeyCode.RightControl,
            InputKeyCode.LeftAlt, InputKeyCode.RightAlt,
            InputKeyCode.LeftWindows, InputKeyCode.RightWindows
        };
    }

    public static KeyCode AsUnityKeyCode(this InputKeyCode target) => (KeyCode)(int)target;
    #endregion

    #region Privates
    private static bool GetKeyState(InputKeyCode target, Predicate<InputKeyCode> predicate) => predicate?.Invoke(target) ?? false;

    private static Predicate<InputKeyCode> GetKeyPredicate(InputKeyCode target, Predicate<KeyCode> defaultInputPredicate, bool wheelDetect = true)
    {
        return target switch
        {
            InputKeyCode.MouseWheelUp or InputKeyCode.MouseWheelDown => wheelChecker,
            _ => inputKeyCode => defaultInputPredicate?.Invoke(AsUnityKeyCode(inputKeyCode)) ?? false
        };

        bool wheelChecker(InputKeyCode inputKeyCode)
        {
            if (!wheelDetect)
            {
                return false;
            }

            float scrollY = Input.mouseScrollDelta.y;
            return inputKeyCode switch
            {
                InputKeyCode.MouseWheelDown => scrollY < 0,
                InputKeyCode.MouseWheelUp => scrollY > 0,
                _ => false
            };
        }
    }
    #endregion

    private static InputKeyCode[] _allModifier;
}