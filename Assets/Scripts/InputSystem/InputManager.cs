using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

public static class InputManager
{
    private static readonly Dictionary<InputKeyMap, Action> _keyMaps = new();
    private static SafetyCancellationTokenSource _loopCts = new();
    private static UniTask _loopTask = UniTask.CompletedTask;

    public static bool Subscribe(InputKeyMap keyMap, Action callback)
    {
        if (_keyMaps.TryAdd(keyMap, callback))
        {
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
        InputKeyMap.IKeyMapRemovable removable = removeKvp.Key;
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
        // 구현 예정
    }
}