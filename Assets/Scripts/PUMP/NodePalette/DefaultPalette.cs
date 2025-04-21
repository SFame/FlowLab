using System;
using System.Collections.Generic;

public class DefaultPalette : NodePalette
{
    public override Dictionary<Type, string> NodeTypes { get; set; } = new()
    {
        { typeof(ClassedNode), "Classed" },
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
        { typeof(EdgeDetector), "Edge Detector" },
        { typeof(Switch), "Switch" },
    };

    private void Awake()
    {
        SetContent();
    }
}