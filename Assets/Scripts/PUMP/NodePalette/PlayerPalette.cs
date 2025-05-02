using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerPalette : NodePalette
{
    private Dictionary<Type, string> _cachedNodeTypes;

    public override Dictionary<Type, string> NodeTypes
    {
        get
        {
            if (_cachedNodeTypes == null)
            {
                PlayerNodeInventory.Initialize();
                _cachedNodeTypes = PlayerNodeInventory.GetAvailableNodeTypes();
            }
            return _cachedNodeTypes;
        }
        set { _cachedNodeTypes = value; }
    }

    private void Awake()
    {
        PlayerNodeInventory.Initialize();
        SetContent();
    }

    // Method to refresh the palette after unlocking new nodes
    public void RefreshPalette()
    {
        _cachedNodeTypes = PlayerNodeInventory.GetAvailableNodeTypes();
        SetContent();
    }
}
