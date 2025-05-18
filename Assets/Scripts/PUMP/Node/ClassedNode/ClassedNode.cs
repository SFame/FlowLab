using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static TPEnumeratorToken;

[ResourceGetter("PUMP/Sprite/PaletteImage/classed_node_palette")]
public class ClassedNode : DynamicIONode, IClassedNode, INodeAdditionalArgs<ClassedNodeSerializeInfo>
{
    #region Privates
    private List<Action<IClassedNode>> _onDeleteActions = new();
    private string _name;
    private UiMouseListener _mouseListener;

    private UiMouseListener MouseListener
    {
        get
        {
            _mouseListener ??= Support.transform.GetOrAddComponent<UiMouseListener>();
            return _mouseListener;
        }
    }

    private void OnRemoveAdapter(Node node)
    {
        foreach (Action<IClassedNode> action in _onDeleteActions.ToList())  // 순회 도중 Enumerable 변경 예외처리
            action?.Invoke(this);
    }

    private async UniTaskVoid UpdateOutputStateNextFrame(Transition[] outputs)
    {
        try
        {
            await UniTask.WaitForEndOfFrame();

            if (outputs.Length != OutputToken.Count)
            {
                Debug.Log($"{Name}: 출력 설정 무시됨 - 토큰 개수 변경됨 (토큰: {OutputToken.Count}, 출력: {outputs.Length})");
                return;
            }

            for (int i = 0; i < OutputToken.Count; i++)
                OutputToken[i].State = outputs[i];
        }
        catch (Exception e)
        {
            Debug.LogError($"{Name}: 출력 상태 업데이트 중 예외 발생 - {e.Message}");
        }
    }
    #endregion

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 2;

    public override string NodePrefabPath => "PUMP/Prefab/Node/CLASSED";

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override string NodeDisplayName => "Classed";

    protected override float TextSize => 24;

    protected override float InEnumeratorXPos => -72f;

    protected override float OutEnumeratorXPos => 72f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(180f, 80f);

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            contexts.Add(new ContextElement("Edit", () => OpenPanel?.Invoke(this)));
            return contexts;
        }
    }

    protected override void OnAfterInit()
    {
        OnRemove += OnRemoveAdapter;
        ClassedNodePanel.JoinPanel(this);
        MouseListener.OnDoubleClick += _ => OpenPanel?.Invoke(this);
    }

    protected override string DefineInputName(int tpNumber)
    {
        return "in" + tpNumber;
    }

    protected override string DefineOutputName(int tpNumber)
    {
        return "out" + tpNumber;
    }

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return Enumerable.Repeat((Transition)false, outputCount).ToArray();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args == null)
            return;

        OnInputUpdate?.Invoke(args);
    }

    #region Classed Node Interface
    public string Name
    {
        get => _name;
        set
        {
            Support.NameText.text = value;
            Support.name = value;
        }
    }
    public string Id { get; set; } = string.Empty;

    int IClassedNode.InputCount { get => InputCount; set => InputCount = value; }
    int IClassedNode.OutputCount { get => OutputCount; set => OutputCount = value; }

    public event Action<TransitionEventArgs> OnInputUpdate;
    public event Action<IClassedNode> OpenPanel;

    event Action<IClassedNode> IClassedNode.OnDestroy
    {
        add => _onDeleteActions.Add(value);
        remove => _onDeleteActions.Remove(value);
    }

    public void OutputsApplyAll(Transition[] outputs)
    {
        if (outputs.Length != OutputToken.Count)
        {
            UpdateOutputStateNextFrame(outputs).Forget();
            return;
        }

        for (int i = 0; i < OutputToken.Count; i++)
            OutputToken[i].State = outputs[i];
    }

    public void OutputApply(TransitionEventArgs args)
    {
        if (args.Index < 0 || args.Index >= OutputToken.Count)
        {
            Debug.LogError($"ClassedNode.OutputApply: Index out of range: {args.Index}");
            return;
        }

        OutputToken[args.Index].State = args.State;
    }

    public void InputStateValidate(Transition[] exInStates)
    {
        if (exInStates.Length != InputToken.Count)
        {
            throw new ArgumentOutOfRangeException($"Expected {InputToken.Count} elements, but received {exInStates.Length}.");
        }

        if (!InputToken.Select(sf => sf.State).SequenceEqual(exInStates))
        {
            ((IReadonlyToken)InputToken).IsReadonly = false;
            foreach (ITypeListenStateful stateful in InputToken)
            {
                stateful.State = stateful.State;
            }
            ((IReadonlyToken)InputToken).IsReadonly = true;
        }
    }

    public List<Action<TransitionType>> GetInputTypeApplier()
    {
        ITransitionPoint[] inputTps = Support.InputEnumerator.GetTPs();
        return inputTps.Select(tp => new Action<TransitionType>(tp.SetType)).ToList();
    }

    public List<Action<TransitionType>> GetOutputTypeApplier()
    {
        ITransitionPoint[] outTps = Support.OutputEnumerator.GetTPs();
        return outTps.Select(tp => new Action<TransitionType>(tp.SetType)).ToList();
    }

    public Node GetNode() => this;
    #endregion

    #region SerializeData
    public ClassedNodeSerializeInfo AdditionalArgs
    {
        get
        {
            IClassedNode thisClass = this;
            return new(Id, thisClass.InputCount, thisClass.OutputCount);
        }

        set
        {
            IClassedNode thisClass = this;
            thisClass.InputCount = value._inputCount;
            thisClass.OutputCount = value._outputCount;
            Id = value._id;
        }
    }
    #endregion
}

[Serializable]
public struct ClassedNodeSerializeInfo
{
    public ClassedNodeSerializeInfo (string id, int inputCount, int outputCount)
    {
        _id = id;
        _inputCount = inputCount;
        _outputCount = outputCount;
    }

    [OdinSerialize] public string _id;
    [OdinSerialize] public int _inputCount;
    [OdinSerialize] public int _outputCount;
}