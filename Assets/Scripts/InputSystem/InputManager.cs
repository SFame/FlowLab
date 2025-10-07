using System;
using System.Collections.Generic;
using UnityEngine;

public static class InputManager
{
    public static List<InputKeyMap> inputKeyMap { get; } = new();
    private static void Init()
    {
        //keyDetector
        _allKeys = (InputKeyCode[])Enum.GetValues(typeof(InputKeyCode));
        //Subscribe(Setting.CurrentKeyMap);

        Setting.OnSettingUpdated += () =>
        {
            UnSubscribe(inputKeyMap);
            //Subscribe(Setting.CurrentKeyMap);
        };
    }

    public static void SortKeyMap()
    {
        inputKeyMap.Sort((a, b) =>
        {
            if (a.ModifierKeys == null && b.ModifierKeys == null)
                return 0;

            if (a.ModifierKeys == null)
                return 1;

            if (b.ModifierKeys == null)
                return -1;

            if (a.ModifierKeys.Count < b.ModifierKeys.Count)
                return 1;

            if (a.ModifierKeys.Count > b.ModifierKeys.Count)
                return -1;

            return 0;
        });
    }


    private static bool Subscribe(List<InputKeyMap> inputKeyMaps)
    {
        // 중복된 KeyMap이 있을 경우, 구독하지 않음
        foreach (var keyMap in inputKeyMaps)
        {
            if (inputKeyMaps.Contains(keyMap))
            {
                Debug.Log($"[InputManager] KeyMap {keyMap} is already subscribed.");
                return false;
            }
        }
        inputKeyMap.AddRange(inputKeyMaps);
        SortKeyMap();
        return true;
    }
    private static bool UnSubscribe(List<InputKeyMap> inputKeyMaps)
    {
        foreach (var keyMap in inputKeyMaps)
        {
            if (!inputKeyMaps.Contains(keyMap))
            {
                Debug.Log($"[InputManager] KeyMap {keyMap} is not subscribed.");
                return false;
            }
        }
        foreach (var keyMap in inputKeyMaps)
        {
            inputKeyMap.Remove(keyMap);
        }
        return true;
    }

    #region KeyMapDetect
    private static InputKeyCode[] _allKeys;
    #endregion
}
