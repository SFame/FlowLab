using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class InputManager
{
    private static readonly Dictionary<InputKeyMap, InputKeyMapArgs> _keyMaps = new();
    private static readonly HashSet<IInputExclusionTargetValidator> _exclusionValidators = new();
    private static readonly HashSet<InputKeyMap> _excludingTargets = new();
    private static readonly Dictionary<string, int> _allowingNames = new();
    private static readonly HashSet<object> _blocker = new();

    private static KeyValuePair<InputKeyMap, InputKeyMapArgs>[] _sortedKeyMaps;
    private static SafetyCancellationTokenSource _loopCts = new();
    private static UniTask _loopTask = UniTask.CompletedTask;

    public static bool Subscribe(InputKeyMap keyMap, InputKeyMapArgs args)
    {
        if (_keyMaps.TryAdd(keyMap, args))
        {
            SetExclusionTargets();
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
        if (_keyMaps.TryAdd(keyMap, args))
        {
            SetExclusionTargets();
            SortKeyMap();
            LoopCheck();
            return true;
        }

        return false;
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
        SetExclusionTargets();
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

    public static InputKeyMap[] FindKeymapByName(string name)
    {
        return _keyMaps.Where(kvp => kvp.Value.Name == name).Select(kvp => kvp.Key).ToArray();
    }

    public static bool AddBlocker(object blocker)
    {
        if (blocker == null)
        {
            return false;
        }

        return _blocker.Add(blocker);
    }

    public static bool RemoveBlocker(object blocker)
    {
        if (blocker == null)
        {
            return false;
        }

        return _blocker.Remove(blocker);
    }

    public static bool AddInputExclusion(InputExclusionTarget exclusion)
    {
        if (exclusion == null)
        {
            return false;
        }

        bool result = _exclusionValidators.Add(exclusion);
        SetExclusionTargets();
        return result;
    }

    public static bool RemoveInputExclusion(InputExclusionTarget exclusion)
    {
        if (exclusion == null)
        {
            return false;
        }

        bool result = _exclusionValidators.Remove(exclusion);
        SetExclusionTargets();
        return result;
    }

    public static bool AddAllowingName(string name)
    {
        if (name == null)
        {
            return false;
        }

        if (_allowingNames.TryAdd(name, 1))
        {
            return true;
        }

        _allowingNames[name]++;
        return true;
    }

    public static bool RemoveAllowingName(string name)
    {
        if (name == null)
        {
            return false;
        }

        if (!_allowingNames.ContainsKey(name))
        {
            return false;
        }

        if (_allowingNames[name] > 1)
        {
            _allowingNames[name]--;
            return true;
        }

        return _allowingNames.Remove(name);
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

    private static void SetExclusionTargets()
    {
        _excludingTargets.Clear();

        foreach ((InputKeyMap keyMap, InputKeyMapArgs _) in _keyMaps)
        {
            foreach (IInputExclusionTargetValidator validator in _exclusionValidators)
            {
                if (validator.Validate(keyMap))
                {
                    _excludingTargets.Add(keyMap);
                    break;
                }
            }
        }
    }

    private static void KeyCheck()
    {
        if (_sortedKeyMaps == null)
        {
            SortKeyMap();
        }

        foreach ((InputKeyMap keyMap, InputKeyMapArgs args) in _sortedKeyMaps!)
        {
            if (_excludingTargets.Contains(keyMap))
            {
                continue;
            }

            if (_allowingNames.Count > 0 && !_allowingNames.ContainsKey(args.Name))
            {
                continue;
            }

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

public class InputExclusionTarget : IInputExclusionTargetValidator
{
    private readonly HashSet<ModifierKeyCode> _modifiers;
    private readonly ActionKeyCode _actionKey;
    private readonly bool _useActionKey;

    public InputExclusionTarget(ActionKeyCode actionKey)
    {
        _useActionKey = true;
        _actionKey = actionKey;
        _modifiers = null;
    }

    public InputExclusionTarget(params ModifierKeyCode[] modifiers)
    {
        _useActionKey = false;
        _actionKey = ActionKeyCode.None;
        if (modifiers == null || modifiers.Length <= 0)
        {
            _modifiers = null;
            return;
        }
        _modifiers = modifiers.ToHashSet();
    }

    public InputExclusionTarget(ActionKeyCode actionKey, params ModifierKeyCode[] modifiers)
    {
        _useActionKey = true;
        _actionKey = actionKey;
        if (modifiers == null || modifiers.Length <= 0)
        {
            _modifiers = null;
            return;
        }
        _modifiers = modifiers.ToHashSet();
    }

    public InputExclusionTarget(InputKeyMap keyMap)
    {
        _useActionKey = keyMap.ActionKey != ActionKeyCode.None;
        _actionKey = keyMap.ActionKey;

        if (keyMap.Modifiers == null || keyMap.Modifiers.Count <= 0)
        {
            _modifiers = null;
            return;
        }

        _modifiers = keyMap.Modifiers.ToHashSet();
    }

    bool IInputExclusionTargetValidator.Validate(InputKeyMap keyMap)
    {
        if (_useActionKey && keyMap.ActionKey != _actionKey)
        {
            return false;
        }

        if (_modifiers != null)
        {
            foreach (var modifier in _modifiers)
            {
                if (!keyMap.Modifiers.Contains(modifier))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public interface IInputExclusionTargetValidator
{
    bool Validate(InputKeyMap keyMap);
}