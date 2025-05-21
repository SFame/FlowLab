using System;
using System.Collections.Generic;

public class ClassedPalette : NodePalette
{
    public override Dictionary<string, Dictionary<Type, string>> NodeTypes { get; set; } = new()
    {
        {
            "Logic", new Dictionary<Type, string>
            {
                { typeof(AND), "AND" },
                { typeof(OR), "OR" },
                { typeof(NAND), "NAND" },
                { typeof(NOR), "NOR" },
                { typeof(NOT), "NOT" },
                { typeof(XNOR), "XNOR" },
                { typeof(XOR), "XOR" },
            }
        },
        {
            "I/O", new Dictionary<Type, string>
            {
                { typeof(ClickSwitch), "ClickSwitch" },
                { typeof(SegmentDisplay), "7-Segment Display" },
                { typeof(BinaryDisplay), "Binary Display" },
            }
        },
        {
            "Signal", new Dictionary<Type, string>
            {
                { typeof(Splitter), "Split" },
                { typeof(Merger), "Merger" },
                { typeof(Switch), "Switch" },
                { typeof(EdgeDetector), "Edge Detector" },
                { typeof(Debouncer), "Debouncer" },
                { typeof(Timer), "Timer" },
            }
        },
        {
            "Math", new Dictionary<Type, string>
            {
                { typeof(Comparator), "Comparator" },
                { typeof(FourbitALU), "FourbitALU" },
            }
        },
        {
            "Advanced", new Dictionary<Type, string>
            {
                { typeof(ScriptingNode), "Scripting" },
            }
        },
        {
            "Debug", new Dictionary<Type, string>
            {
                { typeof(DN), "Debug" },
                { typeof(IntSwitch), "IntSw" },
                { typeof(FloatSwitch), "FloatSw" },
                { typeof(StringSwitch), "StringSw" },
            }
        }
    };

    private void Awake()
    {
        SetContent();
    }
}