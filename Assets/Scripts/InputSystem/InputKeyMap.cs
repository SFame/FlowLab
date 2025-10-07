using System;
using System.Collections.Generic;
using System.Linq;
using static InputKeyMap;

public readonly struct InputKeyMap : IEquatable<InputKeyMap>, IKeyMapRemovable
{
    #region Non Interfece
    public interface IKeyMapRemovable
    {
        void Remove();
    }

    void IKeyMapRemovable.Remove()
    {
        _onRemove?.Invoke();

        for (int i = 0; i < _actionKeys.Length; i++)
        {
            _actionKeys[i] = InputKeyCode.None;
        }

        for (int i = 0; i < _modifierKeys.Length; i++)
        {
            _modifierKeys[i] = InputKeyCode.None;
        }
    }

    private readonly InputKeyCode[] _actionKeys;
    private readonly InputKeyCode[] _modifierKeys;
    private readonly Action _onRemove;

    public InputKeyMap(HashSet<InputKeyCode> actionKeys, HashSet<InputKeyCode> modifierKeys = null, bool immutable = false, Action onRemove = null)
    {
        _actionKeys = actionKeys != null ? actionKeys.ToArray() : Array.Empty<InputKeyCode>();
        _modifierKeys = modifierKeys != null ? modifierKeys.ToArray() : Array.Empty<InputKeyCode>();
        Immutable = immutable;
        _onRemove = onRemove;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            _actionKeys.Aggregate(0, (hash, key) => hash ^ key.GetHashCode()),
            _modifierKeys.Aggregate(0, (hash, key) => hash ^ key.GetHashCode())
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
        return _actionKeys.Length == other._actionKeys.Length &&
               _modifierKeys.Length == other._modifierKeys.Length &&
               _actionKeys.OrderBy(x => x).SequenceEqual(other._actionKeys.OrderBy(x => x)) &&
               _modifierKeys.OrderBy(x => x).SequenceEqual(other._modifierKeys.OrderBy(x => x));
    }
    #endregion

    #region Interface
    public IReadOnlyList<InputKeyCode> ActionKeys => _actionKeys;

    public IReadOnlyList<InputKeyCode> ModifierKeys => _modifierKeys;

    public bool Immutable { get; }
    #endregion
}