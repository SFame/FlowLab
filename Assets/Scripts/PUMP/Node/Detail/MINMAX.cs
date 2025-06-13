using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinMax : DynamicIONode, INodeAdditionalArgs<MinMax.MinMaxSerializeInfo>
{
    private List<ContextElement> _contexts;

    public override string NodePrefabPath => "PUMP/Prefab/Node/MIN_MAX";

    protected override string NodeDisplayName => "";

    protected override float InEnumeratorXPos => -64f;

    protected override float OutEnumeratorXPos => 64f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(160f, 50f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override void StateUpdate(TransitionEventArgs args) => PushResult();

    protected override string DefineInputName(int tpIndex) => $"in {tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "out";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Int;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Int;

    private MinMaxSupport MinMaxSupport
    {
        get
        {
            _minMaxSupport ??= Support.GetComponent<MinMaxSupport>();
            return _minMaxSupport;
        }
    }


    private void SetTypeAll(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }
    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetTypeAll(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetTypeAll(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }
    private int IntOperating(string @operator)
    {

        int[] values = InputToken.Where(tp => tp.State != tp.State.Type.Null())
                           .Select(tp => (int)tp.State)
                           .ToArray();

        return @operator switch
        {
            "Min" => values.Min(),
            "Max" => values.Max(),
            _ => throw new NotSupportedException($"Operator '{@operator}' is not supported for Int type.")
        };
    }
    private float FloatOperating(string @operator)
    {

        float[] values = InputToken.Where(tp => tp.State != tp.State.Type.Null())
                                   .Select(tp => (float)tp.State)
                                   .ToArray();
        return @operator switch
        {
            "Min" => values.Min(),
            "Max" => values.Max(),
            _ => throw new NotSupportedException($"Operator '{@operator}' is not supported for Float type.")
        };
    }

    private void PushResult()
    {
        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = OutputToken[0].Type.Null();
            return;
        }
        if (OutputToken[0].Type == TransitionType.Int)
        {
            int result = IntOperating(Operator);
            OutputToken[0].State = result;
            return;
        }
        if (OutputToken[0].Type == TransitionType.Float)
        {
            float result = FloatOperating(Operator);
            OutputToken[0].State = result;
            return;
        }
 
    }

    protected override void OnAfterInit()
    {
        MinMaxSupport.Initialize(InputCount, Operator);

        MinMaxSupport.OnInputCountUpdated += inputCount =>
        {
            InputCount = inputCount;
            ReportChanges();
        };

        MinMaxSupport.OnOperatorUpdated += @operator =>
        {
            Operator = @operator;
            NodeName = @operator;
            PushResult();
            ReportChanges();
        };
        
    }

    #region Privates

    private MinMaxSupport _minMaxSupport;
    #endregion

    #region Serialize target
    // InputCount 포함
    private string Operator { get; set; } = "Min";
    private string NodeName { get; set; } = "Min";
    #endregion

    public MinMaxSerializeInfo AdditionalArgs
    {
        get => new() { _inputCount = InputCount, _operator = Operator, _nodeName = NodeName };

        set
        {
            InputCount = value._inputCount;
            Operator = value._operator;
            NodeName = value._nodeName;
        }
    }

    [Serializable]
    public struct MinMaxSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public string _operator;
        [OdinSerialize] public string _nodeName;

        public override string ToString()
        {
            return $"Input Count: {_inputCount}, Operator: {_operator}";
        }
    }
}