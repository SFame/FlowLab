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
                { typeof(Any), "Any" },
                { typeof(All), "All" },
                { typeof(Comparator), "Comparator" },
            }
        },
        {
            "Flow", new Dictionary<Type, string>
            {
                { typeof(IfNode), "If" },
            }
        },
        {
            "I/O", new Dictionary<Type, string>
            {
                { typeof(Trigger), "Trigger" },
                { typeof(OnOffSwitch), "On/Off Switch" },
                { typeof(InputSwitch), "Input Switch" },
                { typeof(InputField), "Input Field" },
                { typeof(KeyInput), "Key Input" },
                { typeof(Display), "Display" },
                { typeof(SegmentDisplay), "7-Segment Display" },
                { typeof(BinaryDisplay), "Binary Display" },
            }
        },
        {
            "Signal", new Dictionary<Type, string>
            {
                { typeof(IsNull), "IsNull" },
                { typeof(Splitter), "Split" },
                { typeof(Merger), "Merger" },
                { typeof(Switch), "Switch" },
                { typeof(OneHot), "One Hot" },
                { typeof(EdgeDetector), "Edge Detector" },
                { typeof(TypeConverter), "Type Converter" },
                { typeof(Debouncer), "Debouncer" },
                { typeof(Multiplexer), "Multiplexer" },
                { typeof(Blink), "Blink" },
                { typeof(OneShot), "One Shot" },
                { typeof(Delay), "Delay" },
                { typeof(Counter), "Counter" },
                { typeof(Sender), "Sender" },
                { typeof(SignalDetector), "Signal Detector" },
                { typeof(TFlipFlop), "T Flip-Flop" },
                { typeof(Timer), "Timer" },
                { typeof(FrequencyMeter), "Frequency Meter" },
            }
        },
        {
            "Math", new Dictionary<Type, string>
            {
                { typeof(Add), "Add" },
                { typeof(Subtract), "Sub" },
                { typeof(Multiply), "Mul" },
                { typeof(Divide), "Div" },
                { typeof(Pow), "Pow" },
                { typeof(SquareRoot), "Square Root" },
                { typeof(Lerp), "Lerp" },
                { typeof(Clamp), "Clamp" },
                { typeof(Absolute), "Absolute" },
                { typeof(MinMax), "MinMax" },
                { typeof(Average), "Average" },
                { typeof(StandardDeviation), "Standard Deviation" },
                { typeof(NumericComparator), "Numeric Comparator" },
                { typeof(Equal), "Equal" },
                { typeof(Round), "Round" },
                { typeof(Sin), "Sin" },
                { typeof(Asin), "Asin" },
                { typeof(Sinh), "Sinh" },
                { typeof(Cos), "Cos" },
                { typeof(Acos), "Acos" },
                { typeof(Cosh), "Cosh" },
                { typeof(Tan), "Tan" },
                { typeof(Atan), "Atan" },
                { typeof(Atan2), "Atan2" },
                { typeof(Tanh), "Tanh" },
                { typeof(BinaryDecoder), "Binary Decoder" },
                { typeof(BinaryEncoder), "Binary Encoder" },
                { typeof(RandomNumber), "Random" },
            }
        },
        {
            "Util", new Dictionary<Type, string>
            {
                { typeof(StringLength), "String Length" },
                { typeof(StringReplace), "String Replace" },
                { typeof(StringContain), "String Contain" },
                { typeof(StringSplit), "String Split" },
                { typeof(StringConcat), "String Concat" },
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