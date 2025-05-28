using System;
using System.Collections.Generic;

public class DefaultPalette : NodePalette
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
                { typeof(InputSwitch), "InputSwitch" },
                { typeof(Display), "Display"},
                { typeof(SegmentDisplay), "7-Segment Display" },
                { typeof(BinaryDisplay), "Binary Display" },
            }
        },
        {
            "Signal", new Dictionary<Type, string>
            {
                { typeof(Splitter), "Split" },
                { typeof(Switch), "Switch" },
                { typeof(EdgeDetector), "Edge Detector" },
                { typeof(TypeConverter), "Type Converter"},
                { typeof(Debouncer), "Debouncer" },
                { typeof(Timer), "Timer" },
            }
        },
        {
            "Math", new Dictionary<Type, string>
            {
                { typeof(Comparator), "Comparator" },
                { typeof(Equal), "Equal" },
                { typeof(FourbitALU), "FourbitALU" },
                { typeof(Round), "Round" },
                { typeof(BinaryDecoder), "BinaryDecoder" },
                { typeof(BinaryEncoder), "BinaryEncoder" },
                { typeof(Lerp), "Lerp" },
                { typeof(Clamp), "Clamp" },
                { typeof(Absolute), "Absolute" },
                { typeof(Multiplexer), "Multiplexer" },
            }
        },
        {
            "Util", new Dictionary<Type, string>
            {
                { typeof(StringCounter), "String Counter" },
                { typeof(StringReplace), "String Replace" },
                { typeof(StringContain), "String Contain" },
            }
        },
        {
            "Advanced", new Dictionary<Type, string>
            {
                { typeof(ScriptingNode), "Scripting" },
                { typeof(ClassedNode), "Classed" },
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