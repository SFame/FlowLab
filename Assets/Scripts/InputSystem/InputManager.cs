using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public static class InputManager
{
    private static readonly Dictionary<InputKeyMap, InputKeyMapArgs> _keyMaps = new();
    private static readonly HashSet<object> _blocker = new();
    private static KeyValuePair<InputKeyMap, InputKeyMapArgs>[] _sortedKeyMaps;
    private static SafetyCancellationTokenSource _loopCts = new();
    private static UniTask _loopTask = UniTask.CompletedTask;

    public static bool Subscribe(InputKeyMap keyMap, InputKeyMapArgs args)
    {
        if (_keyMaps.TryAdd(keyMap, args))
        {
            SortKeyMap();
            LoopCheck();
            return true;
        }

        KeyValuePair<InputKeyMap, InputKeyMapArgs> actualKvp = _keyMaps.FirstOrDefault(pair => pair.Key.Equals(keyMap));
        if (actualKvp.Value.Immutable)
        {
            return false;
        }

        Unsubscribe(actualKvp.Key);
        bool result = _keyMaps.TryAdd(keyMap, args);
        SortKeyMap();
        LoopCheck();
        return result;
    }

    public static bool Unsubscribe(InputKeyMap keyMap)
    {
        if (!_keyMaps.ContainsKey(keyMap))
        {
            return false;
        }

        KeyValuePair<InputKeyMap, InputKeyMapArgs> removeKvp = _keyMaps.FirstOrDefault(pair => pair.Key.Equals(keyMap));
        removeKvp.Value.OnRemove?.Invoke(removeKvp.Key);
        _keyMaps.Remove(removeKvp.Key);
        SortKeyMap();
        LoopCheck();
        return true;
    }

    public static bool TryFind(InputKeyMap targetKeyMap, out InputKeyMapArgs findArgs)
    {
        findArgs = default;

        if (_keyMaps.TryGetValue(targetKeyMap, out InputKeyMapArgs args))
        {
            findArgs = args;
            return true;
        }

        return false;
    }

    public static bool AddBlocker(object blocker)
    {
        return _blocker.Add(blocker);
    }

    public static bool RemoveBlocker(object blocker)
    {
        return _blocker.Remove(blocker);
    }

    private static void SortKeyMap()
    {
        _sortedKeyMaps = _keyMaps.OrderByDescending(kvp => kvp.Key.Modifiers.Count).ToArray();
    }

    private static void LoopCheck()
    {
        if (_keyMaps.Count <= 0)
        {
            _loopCts.CancelAndDispose();
            return;
        }

        if (_loopTask.Status != UniTaskStatus.Pending)
        {
            _loopTask = KeyCheckLoopAsync(_loopCts.CancelAndDisposeAndGetNewToken(out _loopCts));
        }
    }

    private static async UniTask KeyCheckLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_blocker.Count <= 0)
                {
                    KeyCheck();
                }

                await UniTask.Yield(token, cancelImmediately: true);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void KeyCheck()
    {
        if (_sortedKeyMaps == null)
        {
            SortKeyMap();
        }

        foreach ((InputKeyMap keyMap, InputKeyMapArgs args) in _sortedKeyMaps!)
        {
            bool matched = false;

            if (keyMap.Modifiers.Count == 0)
            {
                bool noModifiersPressed = InputManagerUtil.GetAllModifiers().All(mod => !mod.GetKey());
                bool actionKeyPress = args.ActionHold ? keyMap.ActionKey.GetKey() : keyMap.ActionKey.GetKeyDown();

                if (noModifiersPressed && actionKeyPress)
                {
                    args.Callback?.Invoke(keyMap);
                    matched = true;
                }
            }
            else
            {
                bool allRequiredPressed = keyMap.Modifiers.All(mod => mod.GetKey());
                bool actionKeyPressed = args.ActionHold ? keyMap.ActionKey.GetKey() : keyMap.ActionKey.GetKeyDown();

                IEnumerable<ModifierKeyCode> otherModifiers = InputManagerUtil.GetAllModifiers().Except(keyMap.Modifiers);
                bool noOtherModifiers = otherModifiers.All(mod => !mod.GetKey());

                if (allRequiredPressed && actionKeyPressed && noOtherModifiers)
                {
                    args.Callback?.Invoke(keyMap);
                    matched = true;
                }
            }

            if (matched)
            {
                break;
            }
        }
    }
}