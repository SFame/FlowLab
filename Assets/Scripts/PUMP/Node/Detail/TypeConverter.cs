using System.Collections.Generic;
using UnityEngine;

public class TypeConverter : Node
{
    private List<ContextElement> _contexts;
    private TypeConverterSupport _typeConverterSupport;

    private TypeConverterSupport TypeConverterSupport
    {
        get
        {
            _typeConverterSupport ??= Support.GetComponent<TypeConverterSupport>();
            return _typeConverterSupport;
        }
    }
    public override string NodePrefabPath => "PUMP/Prefab/Node/TYPE_CONVERTER";

    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "Conv";

    protected override float TextSize => 25f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Input: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => InputTypeChange(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Input: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => InputTypeChange(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Input: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => InputTypeChange(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Input: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => InputTypeChange(TransitionType.String)));
                _contexts.Add(new ContextElement($"Output: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => OutputTypeChange(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Output: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => OutputTypeChange(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Output: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => OutputTypeChange(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Output: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => OutputTypeChange(TransitionType.String)));
            }

            return _contexts;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            OutputToken.ApplyAllAsNull();
            return;
        }

        ConvertAndApply(args.State);
    }

    private void InputTypeChange(TransitionType transitionType)
    {
        InputToken.SetType(0, transitionType);
        ConvertAndApply(InputToken[0].State);
        ReportChanges();
    }

    private void OutputTypeChange(TransitionType transitionType)
    {
        OutputToken.SetType(0, transitionType);
        ConvertAndApply(InputToken[0].State);
        ReportChanges();
    }

    private void ConvertAndApply(Transition initState)
    {
        bool convertSuccess = initState.TryConvert(OutputToken[0].Type, out Transition converted);
        if (convertSuccess)
        {
            OutputToken[0].State = converted;
            return;
        }

        OutputToken.ApplyAllAsNull();
        TypeConverterSupport.ShowFail();
    }
}