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

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "Conv";

    protected override float NameTextSize => 17f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color> > In", () => InputTypeChange(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> > In", () => InputTypeChange(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>  > In", () => InputTypeChange(TransitionType.Float)));
                _contexts.Add(new ContextElement($"<color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>  > In", () => InputTypeChange(TransitionType.String)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => OutputTypeChange(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => OutputTypeChange(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => OutputTypeChange(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => OutputTypeChange(TransitionType.String)));
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
            OutputToken.PushAllAsNull();
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
        bool convertSuccess = initState.TryConvert(OutputToken[0].Type, out Transition converted, true);
        if (convertSuccess)
        {
            OutputToken[0].State = converted;
            TypeConverterSupport.HideFail();
            return;
        }

        OutputToken.PushAllAsNull();
        TypeConverterSupport.ShowFail();
    }
}