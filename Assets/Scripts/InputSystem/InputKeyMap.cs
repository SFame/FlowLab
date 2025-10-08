using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        bool RemoveModifier(InputKeyCode target);
    }

    void IKeyMapRemovable.Remove()
    {
        _onRemove?.Invoke();
    }

    bool IModifierRemovable.RemoveModifier(InputKeyCode target)
    {
        return _modifiers.Remove(target);
    }

    [SerializeField, OdinSerialize]
    private readonly InputKeyCode _actionKey;

    [SerializeField, OdinSerialize]
    private readonly List<InputKeyCode> _modifiers;

    [SerializeField, OdinSerialize]
    private readonly bool _immutable;

    [NonSerialized]
    private readonly Action _onRemove;

    private InputKeyMap(InputKeyCode actionKey, List<InputKeyCode> modifierKeys, bool immutable, Action onRemove)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<InputKeyCode>();
        _immutable = immutable;
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
        return new InputKeyMap(_actionKey, _modifiers, Immutable, _onRemove);
    }
    #endregion

    #region Interface
    public InputKeyMap(InputKeyCode actionKey, HashSet<InputKeyCode> modifierKeys = null, bool immutable = false, Action onRemove = null)
    {
        _actionKey = actionKey;
        _modifiers = modifierKeys != null ? modifierKeys.ToList() : new List<InputKeyCode>();
        _immutable = immutable;
        _onRemove = onRemove;
    }

    private InputKeyMap(InputKeyCode actionKey, bool immutable)
    {
        _actionKey = actionKey;
        _modifiers = new List<InputKeyCode>();
        _immutable = immutable;
        _onRemove = null;
    }

    public InputKeyCode ActionKey => _actionKey;

    public IReadOnlyList<InputKeyCode> Modifiers => _modifiers;

    public bool Immutable => _immutable;
    #endregion
}