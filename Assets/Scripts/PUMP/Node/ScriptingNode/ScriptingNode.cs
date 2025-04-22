using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ScriptingNode;

[ResourceGetter("PUMP/Sprite/PaletteImage/scripting_node_palette")]
public class ScriptingNode : DynamicIONode, INodeAdditionalArgs<ScriptingNodeSerializeInfo>
{
    #region Privates
    private ScriptingSupport _scriptingSupport;
    private bool _isDeserialized;

    private string Script { get; set; }

    private ScriptCommunicator Communicator { get; set; }

    private ScriptingSupport ScriptingSupport
    {
        get
        {
            _scriptingSupport ??= Support.GetComponent<ScriptingSupport>();
            return _scriptingSupport;
        }
    }

    private void ClickAction()
    {
        string script = @"# Defines the node's name
name: str = ""AND Gate""
# Specifies number of input ports
input_counts: int = 2
# Specifies number of output ports
output_counts: int = 1
# Object responsible for applying output signals to the node
output_applier: OutputApplier = None

def init() -> None:
    """"""
    Initialization function called when the node is freshly created or during Undo/Redo operations.
    Keep it clean, keep it lean.
    """"""
    # 초기 상태에서 출력 업데이트 수행
    outputs = [False]
    output_applier.apply(outputs)

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    """"""
    The nerve center - triggered whenever any signal is detected on input ports.
    
    Parameters:
        inputs (list): Boolean list representing the state of each input port
        index (int): Index of the input port that just changed. -1 when state_update is triggered by system
        state (bool): The new state value (True/False) of the modified port
        is_changed (bool): Flag indicating if the value actually changed from previous state
    """"""
    # AND 게이트 로직 구현
    # 두 입력이 모두 True인 경우에만 출력도 True
    result = all(inputs)
    
    # 출력 포트 수에 맞게 출력값 리스트 생성
    outputs = [result]
    
    # 출력 적용
    output_applier.apply(outputs)";

        AddScript(script);
    }
    #endregion

    #region Override
    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> baseList = base.ContextElements;
            baseList.Add(new ContextElement("Add New Script", ClickAction));
            baseList.Add(new ContextElement("Remove Script", DisposeScript));
            return baseList;
        }
    }

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefebPath => "PUMP/Prefab/Node/SCRIPTING";

    protected override float InEnumeratorXPos => -70f;

    protected override float OutEnumeratorXPos => 70f;

    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(170f, 100f);

    protected override string NodeDisplayName => "Script";

    protected override float TextSize => 25f;

    protected override int DefaultInputCount => 0;

    protected override int DefaultOutputCount => 0;

    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";

    protected override string DefineOutputName(int tpNumber) => $"out {tpNumber}";

    protected override void OnAfterSetAdditionalArgs() => _isDeserialized = true;

    protected override void OnAfterInit()
    {
        base.OnAfterInit();

        if (!_isDeserialized)
            return;

        if (string.IsNullOrEmpty(Script))
        {
            InternalDisposeScript(false);
            return;
        }

        InternalAddScript(Script);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (Communicator == null)
            return;

        Communicator.InvokeStateUpdate(args, InputToken.Select(tp => tp.State).ToList());
    }

    protected override void OnBeforeRemove()
    {
        InternalDisposeScript();
    }
    #endregion

    #region Scripting
    private void InternalAddScript(string script)
    {
        try
        {
            InternalDisposeScript();
            Script = script;

            if (string.IsNullOrEmpty(Script))
            {
                return;
            }

            Communicator = new ScriptCommunicator(ScriptingSupport.Log, ScriptingSupport.LogException);

            if (Communicator.SetScript(Script))
            {
                Support.SetName(Communicator.ScriptFieldInfo.Name);
                InputCount = Communicator.ScriptFieldInfo.InputCount;
                OutputCount = Communicator.ScriptFieldInfo.OutputCount;
                Communicator.OnOutputApply += OutputToken.ApplyStatesAll;
                Communicator.InvokeInit();
                return;
            }

            InternalDisposeScript();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void InternalDisposeScript(bool countReset = true)
    {
        Script = string.Empty;
        Communicator?.Dispose();
        Communicator = null;
        if (countReset)
        {
            InputCount = DefaultInputCount;
            OutputCount = DefaultOutputCount;
        }
        Support.SetName(NodeDisplayName);
    }

    public void AddScript(string script)
    {
        if (string.IsNullOrEmpty(script))
        {
            ScriptingSupport.Log("스크립트가 비어있습니다");
            InternalDisposeScript();
            return;
        }

        InternalAddScript(script);
        ReportChanges();
    }

    public void DisposeScript()
    {
        InternalDisposeScript(true);
        ReportChanges();
    }
    #endregion

    #region Serialization
    public ScriptingNodeSerializeInfo AdditionalTArgs
    {
        get => new()
        {
            _inputCount = InputCount,
            _outputCount = OutputCount,
            _script = Script
        };
        set
        {
            InputCount = value._inputCount;
            OutputCount = value._outputCount;
            Script = value._script;
        }
    }

    public object AdditionalArgs
    {
        get => AdditionalTArgs;
        set => AdditionalTArgs = (ScriptingNodeSerializeInfo)value;
    }

    [Serializable]
    public struct ScriptingNodeSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public int _outputCount;
        [OdinSerialize] public string _script;

        public override string ToString()
        {
            return $"input: {_inputCount} / output: {_outputCount} / script: {_script}";
        }
    }
    #endregion
}
