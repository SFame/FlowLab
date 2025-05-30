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
                { typeof(ClickSwitch), "On/Off Switch" },
                { typeof(InputSwitch), "Input Switch" },
                { typeof(InputField), "Input Field"},
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
                { typeof(FourbitALU), "4-bit ALU" },
                { typeof(Round), "Round" },
                { typeof(BinaryDecoder), "Binary Decoder" },
                { typeof(BinaryEncoder), "Binary Encoder" },
                { typeof(Lerp), "Lerp" },
                { typeof(Clamp), "Clamp" },
                { typeof(Absolute), "Absolute" },
                { typeof(Multiplexer), "Multiplexer" },
                { typeof(MinMax), "MinMax" },
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
    };

    private void Awake()
    {
        SetContent();
    }
}