using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InputManagerUtil
{
    #region Interface
    public static bool GetKeyDown(this ActionKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKeyDown));

    public static bool GetKeyUp(this ActionKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKeyUp, false));

    public static bool GetKey(this ActionKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKey));

    public static bool GetKeyDown(this ModifierKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKeyDown));

    public static bool GetKeyUp(this ModifierKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKeyUp));

    public static bool GetKey(this ModifierKeyCode find) => GetKeyState(find, GetKeyPredicate(find, Input.GetKey));

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

    public static ModifierKeyCode[] GetAllModifiers() => _allModifiers ??= (ModifierKeyCode[])Enum.GetValues(typeof(ModifierKeyCode));
    public static ActionKeyCode[] GetAllActionKeys() => _allActionKeys ??= (ActionKeyCode[])Enum.GetValues(typeof(ActionKeyCode));
    public static KeyCode[] GetAllKeys() => _allKeys ??= GetAllModifiers().Select(key => (int)key).Concat(GetAllActionKeys().Select(key => (int)key)).Select(intKey => (KeyCode)intKey).ToArray();

    public static int[] GetAllModifiersInt() => _allModifiersInt ??= GetAllModifiers().Select(key => (int)key).ToArray();
    public static int[] GetAllActionKeysInt() => _allActionKeysInt ??= GetAllActionKeys().Select(key => (int)key).ToArray();
    public static int[] GetAllKeysInt() => _allKeysInt ??= GetAllKeys().Select(key => (int)key).ToArray();

    public static KeyCode AsUnityKeyCode(this ActionKeyCode target) => (KeyCode)(int)target;
    public static KeyCode AsUnityKeyCode(this ModifierKeyCode target) => (KeyCode)(int)target;

    public static bool TryConvertActionKey(this KeyCode original, out ActionKeyCode converted)
    {
        converted = default;
        if (GetAllActionKeysInt().Contains((int)original))
        {
            converted = (ActionKeyCode)original;
            return true;
        }
        return false;
    }

    public static bool TryConvertModifier(this KeyCode original, out ModifierKeyCode converted)
    {
        converted = default;
        if (GetAllModifiersInt().Contains((int)original))
        {
            converted = (ModifierKeyCode)original;
            return true;
        }
        return false;
    }

    public static bool IsActionKey(this KeyCode key) => GetAllActionKeysInt().Contains((int)key);
    public static bool IsModifier(this KeyCode key) => GetAllModifiersInt().Contains((int)key);
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

    private static ModifierKeyCode[] _allModifiers;
    private static ActionKeyCode[] _allActionKeys;
    private static KeyCode[] _allKeys;
    private static int[] _allModifiersInt;
    private static int[] _allActionKeysInt;
    private static int[] _allKeysInt;
}