using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerPalette : NodePalette
{
    private Dictionary<string, Dictionary<Type, string>> _cachedNodeTypes;

    public override Dictionary<string, Dictionary<Type, string>> NodeTypes
    {
        get
        {
            if (_cachedNodeTypes == null)
            {
                RefreshPalette();
            }
            return _cachedNodeTypes;
        }
        set
        {
            _cachedNodeTypes = value;
        }
    }

    private void Awake()
    {
        PlayerNodeInventory.Initialize();
        PlayerNodeInventory.OnNodeUnlocked += HandleNodeUnlocked;
        SetContent();
    }
    private void OnDestroy()
    {
        PlayerNodeInventory.OnNodeUnlocked -= HandleNodeUnlocked;
    }

    // Method to refresh the palette after unlocking new nodes
    public void RefreshPalette()
    {
        _cachedNodeTypes = PlayerNodeInventory.GetAvailableNodesByCategory();
        SetContent();
    }
    private void HandleNodeUnlocked(Type nodeType)
    {
        RefreshPalette();
    }
}
