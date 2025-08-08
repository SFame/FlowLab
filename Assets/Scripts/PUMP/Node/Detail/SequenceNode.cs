using UnityEngine;

public class SequenceNode : DynamicIONode
{
    private bool _includeOffState = true;
    private int _currentIndex = -1;

    protected override string NodeDisplayName => "Seq";

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override float NameTextSize => 20f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 4;

    protected override string DefineInputName(int tpIndex) => tpIndex == 0 ? "exec" : "rst";

    protected override string DefineOutputName(int tpIndex) => $"t{tpIndex}";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        _currentIndex = _includeOffState ? -1 : 0;
        if (InputToken[0].State.IsNull)
        {
            return TransitionUtil.GetNullArray(outputTypes);
        }

        Transition[] result = TransitionUtil.GetDefaultArray(outputTypes);

        if (!_includeOffState)
        {
            result[0] = true;
        }

        return result;
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}