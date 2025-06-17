using System;
using System.Linq;
using UnityEngine;

public class StringSplit : DynamicIONode, INodeAdditionalArgs<int>
{
    private SplitterSupport _splitterSupport;

    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
                _splitterSupport = Support.GetComponent<SplitterSupport>();

            return _splitterSupport;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";


    protected override Vector2 NameTextOffset => new Vector2(0f, 15f);

    protected override string NodeDisplayName => "Str\nSplt";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 18f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 4;

    protected override string DefineInputName(int tpIndex)
    {
        return tpIndex switch
        {
            0 => "text",
            1 => "sep",
            _ => tpIndex.ToString()
        };
    }

    protected override string DefineOutputName(int tpIndex) => $"O{tpIndex}";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.String;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.String;

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        Transition[] result = SplitOperation();

        if (outputCount == result.Length)
        {
            return result;
        }

        if (outputCount < result.Length)
        {
            Transition[] newResult = new Transition[outputCount];
            Array.Copy(result, newResult, outputCount);
            return newResult;
        }
        else
        {
            Transition[] newResult = new Transition[outputCount];

            Array.Copy(result, newResult, result.Length);

            for (int i = result.Length; i < outputCount; i++)
            {
                newResult[i] = TransitionType.String.Null();
            }

            return newResult;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(OutputCount, value =>
        {
            OutputCount = value;
            ReportChanges();
        });
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OutputToken.PushAllSafety(SplitOperation());
    }

    private Transition[] SplitOperation()
    {
        if (InputToken[0].State.IsNull)
        {
            return Enumerable.Repeat(OutputToken.FirstState.Type.Null(), OutputToken.Count).ToArray();
        }

        if (InputToken[1].State.IsNull)
        {
            return new [] { InputToken[0].State };
        }

        string value = InputToken[0].State;
        string splitter = InputToken[1].State;

        string[] split = value.Split(splitter);

        return split.Select(str => (Transition)str).ToArray();
    }

    public int AdditionalArgs
    {
        get => OutputCount;
        set => OutputCount = value;
    }
}