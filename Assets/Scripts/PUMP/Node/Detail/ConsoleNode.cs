using System.Collections.Generic;
using UnityEngine;

[ResourceGetter("PUMP/Sprite/PaletteImage/palette_elem", "#383838", "#00FF00")]
public class ConsoleNode : Node
{
    private List<ContextElement> _contexts;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetTypeAll(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetTypeAll(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetTypeAll(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetTypeAll(TransitionType.String)));
            }

            return _contexts;
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        ReportChanges();
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/CONSOLE";
    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumIn";
    protected override string NodeDisplayName => "";
    protected override List<string> InputNames => new() { "" };
    protected override List<string> OutputNames => new();
    protected override List<TransitionType> InputTypes => new() { TransitionType.String };
    protected override List<TransitionType> OutputTypes => new();
    protected override float InEnumeratorXPos => -65f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorSpacing => 3f;
    protected override Vector2 DefaultNodeSize => new Vector2(90f, 60f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            return;
        }

        ConsoleWindow.Input(args.State.GetValueString());
    }
}