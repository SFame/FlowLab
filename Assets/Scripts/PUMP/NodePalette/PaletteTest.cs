using System;
using System.Collections.Generic;
using UnityEngine;

public class PaletteTest : NodePalette
{
    public override Dictionary<Type, string> NodeTypes { get; set; } = new()
    {
        { typeof(AND), "AND" },
        { typeof(OR), "OR" },
        { typeof(NAND), "NAND" },
        { typeof(NOR), "NOR" },
        { typeof(NOT), "NOT" },
        { typeof(XNOR), "XNOR" },
        { typeof(XOR), "XOR" },
        { typeof(ClickSwitch), "ClickSwitch" },
        { typeof(Splitter), "Split" },
        { typeof(Comparator), "Compartor" },
    };

    private void Awake()
    {
        SetContent();
        OnNodeAdded += Debug.Log;
    }
}
