using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InputKeyMap;

[Serializable]
public readonly struct InputKeyMap : IEquatable<InputKeyMap>, IKeyMapRemovable, IModifierRemovable
{
    #region Non Interfece
    public interface IKeyMapRemovable
    {
        void Remove();
    }

    public interface IModifierRemovable
    {
        bool RemoveModifier(ModifierKeyCode target);
    }

    void IKeyMapRemovable.Remove()
    {
        _onRemove?.Invoke();
    }

    bool IModifierRemovable.RemoveModifier(ModifierKeyCode target)
    {
        return _modifiers.Remove(target);
    }

    [SerializeField, OdinSerialize]
    private readonly ActionKeyCode _actionKey;

    [SerializeField, OdinSerialize]
    private readonly List<ModifierKeyCode> _modifiers;

    [SerializeField, OdinSerialize]
    private readonly bool _immutable;

    [SerializeField, OdinSerialize]
    private readonly bool _actionHold;

    [NonSerialized]
    private readonly Action _onRemove;


    private InputKeyMap(ActionKeyCode actionKey, List<ModifierKeyCode> modifierKeys, bool immutable, bool actionHold, Action onRemove)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<ModifierKeyCode>();
        _immutable = immutable;
        _actionHold = actionHold;
        _onRemove = onRemove;
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
        return new InputKeyMap(_actionKey, _modifiers, _immutable, _actionHold, _onRemove);
    }
    #endregion

    #region Interface
    public InputKeyMap(ActionKeyCode actionKey, HashSet<ModifierKeyCode> modifierKeys = null, bool immutable = false, bool actionHold = false, Action onRemove = null)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<ModifierKeyCode>();
        _immutable = immutable;
        _actionHold = actionHold;
        _onRemove = onRemove;
    }

    private InputKeyMap(ActionKeyCode actionKey, bool immutable, bool actionHold)
    {
        _actionKey = actionKey;
        _modifiers = new List<ModifierKeyCode>();
        _immutable = immutable;
        _actionHold = actionHold;
        _onRemove = null;
    }

    public ActionKeyCode ActionKey => _actionKey;

    public IReadOnlyList<ModifierKeyCode> Modifiers => _modifiers;

    public bool ActionHold => _actionHold;

    public bool Immutable => _immutable;
    #endregion
}