using System.Collections.Generic;
using UnityEngine;

public readonly struct InputKeyMap
{
    public readonly List<InputKeyCode> ActionKeys;
    public readonly List<InputKeyCode> ModifierKeys;

    public InputKeyMap(List<InputKeyCode> actionKeys, List<InputKeyCode> modifierKeys = null)
    {
        if (actionKeys == null || actionKeys.Count == 0)
        {
            ActionKeys = new List<InputKeyCode>();
        }
        else
        {
            ActionKeys = actionKeys;
        }
        ActionKeys = actionKeys;
        ModifierKeys = modifierKeys;
    }
}
