using System;
using System.Collections.Generic;

public class ClassedPalette : NodePalette
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
        { typeof(Comparator), "Comparator" },
        { typeof(Timer), "Timer" },
    };

    private void Awake()
    {
        SetContent();
    }
}