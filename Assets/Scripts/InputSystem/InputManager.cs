using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static InputKeyMap;

public static class InputManager
{
    private static readonly Dictionary<InputKeyMap, Action> _keyMaps = new();
    private static SafetyCancellationTokenSource _loopCts = new();
    private static UniTask _loopTask = UniTask.CompletedTask;

    public static bool Subscribe(InputKeyMap keyMap, Action callback)
    {
        if (_keyMaps.TryAdd(keyMap, callback))
        {
            LoopCheck();
            return true;
        }

        KeyValuePair<InputKeyMap, Action> actualKvp = _keyMaps.FirstOrDefault(pair => pair.Key.Equals(keyMap));
        if (actualKvp.Key.Immutable)
        {
            return false;
        }

        Unsubscribe(actualKvp.Key);
        bool result = _keyMaps.TryAdd(keyMap, callback);
        LoopCheck();
        return result;
    }

    public static bool Unsubscribe(InputKeyMap keyMap)
    {
        if (!_keyMaps.ContainsKey(keyMap))
        {
            return false;
        }

        KeyValuePair<InputKeyMap, Action> removeKvp = _keyMaps.FirstOrDefault(pair => pair.Key.Equals(keyMap));
        IKeyMapRemovable removable = removeKvp.Key;
        removable.Remove();
        _keyMaps.Remove(removeKvp.Key);
        LoopCheck();
        return true;
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
                KeyCheck();
                await UniTask.Yield(token, cancelImmediately: true);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void KeyCheck()
    {
        KeyValuePair<InputKeyMap, Action>[] sortedKeyMaps = _keyMaps.OrderByDescending(kvp => kvp.Key.Modifiers.Count).ToArray();

        foreach (var kvp in sortedKeyMaps)
        {
            InputKeyMap keyMap = kvp.Key;
            Action action = kvp.Value;

            bool matched = false;

            if (keyMap.Modifiers.Count == 0)
            {
                bool noModifiersPressed = InputManagerUtil.GetAllModifier().All(mod => !mod.GetKey());

                if (noModifiersPressed && keyMap.ActionKey.GetKeyDown())
                {
                    action?.Invoke();
                    matched = true;
                }
            }
            else
            {
                bool allRequiredPressed = keyMap.Modifiers.All(mod => mod.GetKey());
                bool actionKeyPressed = keyMap.ActionKey.GetKeyDown();

                IEnumerable<InputKeyCode> otherModifiers = InputManagerUtil.GetAllModifier().Except(keyMap.Modifiers);
                bool noOtherModifiers = otherModifiers.All(mod => !mod.GetKey());

                if (allRequiredPressed && actionKeyPressed && noOtherModifiers)
                {
                    action?.Invoke();
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