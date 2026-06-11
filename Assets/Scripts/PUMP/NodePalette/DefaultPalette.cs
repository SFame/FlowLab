using System;
using System.Collections.Generic;

public class DefaultPalette : NodePalette
{
    public override Dictionary<string, Dictionary<Type, string>> NodeTypes { get; set; } = new()
    {
        {
            "Logic", new Dictionary<Type, string>
            {
                { typeof(All), "All" },
                { typeof(AND), "AND" },
                { typeof(Any), "Any" },
                { typeof(Comparator), "Comparator" },
                { typeof(NAND), "NAND" },
                { typeof(NOR), "NOR" },
                { typeof(NOT), "NOT" },
                { typeof(OR), "OR" },
                { typeof(XNOR), "XNOR" },
                { typeof(XOR), "XOR" },
            }
        },
        {
            "Memory", new Dictionary<Type, string>
            {
                { typeof(Counter), "Counter" },
                { typeof(DFlipFlop), "D Flip-Flop" },
                { typeof(JKFlipFlop), "JK Flip-Flop" },
                { typeof(SRLatch), "SR Latch" },
                { typeof(TFlipFlop), "T Flip-Flop" },
            }
        },
        {
            "Flow", new Dictionary<Type, string>
            {
                { typeof(Branch), "Branch" },
                { typeof(IfNode), "If" },
                { typeof(Select), "Select" },
                { typeof(SequenceNode), "Sequence" },
                { typeof(WhileNode), "While" },
            }
        },
        {
            "I/O", new Dictionary<Type, string>
            {
                { typeof(SegmentDisplay), "7-Segment Display" },
                { typeof(BinaryDisplay), "Binary Display" },
                { typeof(Display), "Display" },
                { typeof(InputField), "Input Field" },
                { typeof(InputSwitch), "Input Switch" },
                { typeof(KeyInput), "Key Input" },
                { typeof(OnOffSwitch), "On/Off Switch" },
                { typeof(Trigger), "Trigger" },
            }
        },
        {
            "Signal", new Dictionary<Type, string>
            {
                { typeof(Blink), "Blink" },
                { typeof(Debouncer), "Debouncer" },
                { typeof(Delay), "Delay" },
                { typeof(EdgeDetector), "Edge Detector" },
                { typeof(FrequencyMeter), "Frequency Meter" },
                { typeof(Merger), "Merger" },
                { typeof(OneHot), "One Hot" },
                { typeof(OneShot), "One Shot" },
                { typeof(Sender), "Sender" },
                { typeof(SignalDetector), "Signal Detector" },
                { typeof(Splitter), "Split" },
                { typeof(Switch), "Switch" },
                { typeof(Timer), "Timer" },
            }
        },
        {
            "Math", new Dictionary<Type, string>
            {
                { typeof(Absolute), "Absolute" },
                { typeof(Acos), "Acos" },
                { typeof(Add), "Add" },
                { typeof(Asin), "Asin" },
                { typeof(Atan), "Atan" },
                { typeof(Atan2), "Atan2" },
                { typeof(Average), "Average" },
                { typeof(BinaryDecoder), "Binary Decoder" },
                { typeof(BinaryEncoder), "Binary Encoder" },
                { typeof(Clamp), "Clamp" },
                { typeof(Cos), "Cos" },
                { typeof(Cosh), "Cosh" },
                { typeof(Divide), "Divide" },
                { typeof(Equal), "Equal" },
                { typeof(FormulaNode), "Formula" },
                { typeof(Lerp), "Lerp" },
                { typeof(MinMax), "MinMax" },
                { typeof(Modulo), "Modulo" },
                { typeof(Multiply), "Multiply" },
                { typeof(NumericComparator), "Numeric Comparator" },
                { typeof(Pow), "Pow" },
                { typeof(RandomNumber), "Random" },
                { typeof(Round), "Round" },
                { typeof(Sin), "Sin" },
                { typeof(Sinh), "Sinh" },
                { typeof(SquareRoot), "Square Root" },
                { typeof(StandardDeviation), "Standard Deviation" },
                { typeof(Subtract), "Subtract" },
                { typeof(Tan), "Tan" },
                { typeof(Tanh), "Tanh" },
                { typeof(TrueCount), "True Count" },
            }
        },
        {
            "Util", new Dictionary<Type, string>
            {
                { typeof(IsNull), "Is Null" },
                { typeof(NullFilter), "Null Filter" },
                { typeof(StringConcat), "String Concat" },
                { typeof(StringContain), "String Contain" },
                { typeof(StringLength), "String Length" },
                { typeof(StringReplace), "String Replace" },
                { typeof(StringSplit), "String Split" },
                { typeof(ToLower), "To Lower" },
                { typeof(ToUpper), "To Upper" },
                { typeof(Trim), "Trim" },
                { typeof(TypeConverter), "Type Converter" },
            }
        },
        {
            "Advanced", new Dictionary<Type, string>
            {
                { typeof(ClassedNode), "Classed" },
                { typeof(ConsoleNode), "Console" },
                { typeof(ScriptingNode), "Scripting" },
            }
        },
    };

    private void Awake()
    {
        SetContent();
    }
}