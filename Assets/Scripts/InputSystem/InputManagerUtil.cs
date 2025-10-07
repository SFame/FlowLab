using System;
using UnityEngine;

public static class InputManagerUtil
{
    public static bool GetKeyDown(this InputKeyCode find)
    {
        throw new Exception();
    }

    public static bool GetKeyUp(this InputKeyCode find)
    {
        throw new Exception();
    }

    public static bool GetKey(this InputKeyCode find)
    {
        throw new Exception();
    }

    public static bool InModifierKey(this InputKeyCode target)
    {
        throw new Exception();
    }

    public static KeyCode AsUnityKeyCode(this InputKeyCode target) => (KeyCode)(int)target;

    private static bool GetKeyState(InputKeyCode target, Predicate<InputKeyCode> predicate) => predicate?.Invoke(target) ?? false;

    private static Predicate<InputKeyCode> GetKeyPredicate(InputKeyCode target, Predicate<InputKeyCode> defaultPredicate)
    {
        bool WheelChecker(InputKeyCode inputKeyCode)
        {
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
                return WheelChecker;
                break;
            default:
                return inputKeyCode => Input.GetKeyDown(AsUnityKeyCode(inputKeyCode));
        }

        throw new NotSupportedException($"InputKeyCode.{target} is not supported by InputKeyCode.");
    }

    private static Func<bool> GetKeyUpPredicate(InputKeyCode target)
    {
        throw new Exception();
    }
}
