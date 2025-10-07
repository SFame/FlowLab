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

    public static KeyCode AsUnityKeyCode(this InputKeyCode target) => (KeyCode)(int)target;
    #endregion

    #region Privates
    private static bool GetKeyState(InputKeyCode target, Predicate<InputKeyCode> predicate) => predicate?.Invoke(target) ?? false;

    private static Predicate<InputKeyCode> GetKeyPredicate(InputKeyCode target, Predicate<KeyCode> defaultInputPredicate, bool wheelDetect = true)
    {
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

        switch (target)
        {
            case InputKeyCode.MouseWheelUp:
            case InputKeyCode.MouseWheelDown:
                return wheelChecker;
            default:
                return inputKeyCode => defaultInputPredicate?.Invoke(AsUnityKeyCode(inputKeyCode)) ?? false;
        }

        throw new NotSupportedException($"InputKeyCode.{target} is not supported by InputKeyCode.");
    }
    #endregion
}