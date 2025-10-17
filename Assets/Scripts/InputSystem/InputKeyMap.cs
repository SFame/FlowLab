using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public readonly struct InputKeyMap : IEquatable<InputKeyMap>
{
    #region Non Interfece
    [SerializeField, OdinSerialize]
    private readonly ActionKeyCode _actionKey;

    [SerializeField, OdinSerialize]
    private readonly List<ModifierKeyCode> _modifiers;

    private InputKeyMap(ActionKeyCode actionKey, List<ModifierKeyCode> modifierKeys)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<ModifierKeyCode>();

    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            _actionKey.GetHashCode(),
            _modifiers.Aggregate(0, (hash, key) => hash ^ key.GetHashCode())
        );
    }

    public override bool Equals(object other)
    {
        if (other is InputKeyMap keyMap)
        {
            return Equals(keyMap);
        }

        return false;
    }

    public bool Equals(InputKeyMap other)
    {
        return _actionKey == other._actionKey &&
               _modifiers.Count == other._modifiers.Count &&
               _modifiers.OrderBy(x => x).SequenceEqual(other._modifiers.OrderBy(x => x));
    }

    public InputKeyMap Copy()
    {
        return new InputKeyMap(_actionKey, _modifiers);
    }
    #endregion

    #region Interface
    public InputKeyMap(ActionKeyCode actionKey, HashSet<ModifierKeyCode> modifierKeys = null)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<ModifierKeyCode>();
    }

    public ActionKeyCode ActionKey => _actionKey;

    public IReadOnlyList<ModifierKeyCode> Modifiers => _modifiers;
    #endregion
}

[Serializable]
public struct InputKeyMapArgs
{
    public InputKeyMapArgs(string name, Action<InputKeyMap> callback, Action<InputKeyMap> onRemove, bool immutable, bool actionHold)
    {
        Name = name;
        Callback = callback;
        OnRemove = onRemove;
        Immutable = immutable;
        ActionHold = actionHold;
    }

    public InputKeyMapArgs(string name, Action<InputKeyMap> callback)
    {
        Name = name;
        Callback = callback;
        OnRemove = null;
        Immutable = false;
        ActionHold = false;
    }

    [SerializeField] public string Name;

    [SerializeField] public bool Immutable;

    [SerializeField] public bool ActionHold;

    public Action<InputKeyMap> Callback;

    public Action<InputKeyMap> OnRemove;
}