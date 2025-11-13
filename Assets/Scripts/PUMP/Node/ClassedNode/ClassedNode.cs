using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
using static TPEnumeratorToken;

[ResourceGetter("PUMP/Sprite/PaletteImage/classed_node_palette")]
public class ClassedNode : DynamicIONode, IClassedNode, INodeAdditionalArgs<ClassedNodeSerializeInfo>
{
    #region Privates
    private List<Action<IClassedNode>> _onDeleteActions = new();
    private SafetyCancellationTokenSource _cts;
    private string _name = "Classed";
    private bool _isChange = false;
    private UiMouseListener _mouseListener;
    private ClassedSupport _classedSupport;

    private UiMouseListener MouseListener
    {
        get
        {
            _mouseListener ??= Support.transform.GetOrAddComponent<UiMouseListener>();
            return _mouseListener;
        }
    }

    private ClassedSupport ClassedSupport
    {
        get
        {
            if (_classedSupport == null)
            {
                _classedSupport = Support.GetComponent<ClassedSupport>();
            }

            return _classedSupport;
        }
    }

    private void OnRemoveAdapter(Node node)
    {
        foreach (Action<IClassedNode> action in _onDeleteActions.ToList()) // 순회 도중 Enumerable 변경 예외처리
        {
            action?.Invoke(this);
        }
    }

    [Obsolete("문제 발생 시 전환")]
    private async UniTaskVoid UpdateOutputStateNextFrame(Transition[] outputs)
    {
        try
        {
            await UniTask.WaitForEndOfFrame(cancellationToken: _cts.SafeGetToken(out _cts));

            if (outputs.Length != OutputToken.Count)
            {
                Debug.Log($"{Name}: 출력 설정 무시됨 - 토큰 개수 변경됨 (토큰: {OutputToken.Count}, 출력: {outputs.Length})");
                return;
            }

            for (int i = 0; i < OutputToken.Count; i++)
            {
                OutputToken[i].State = outputs[i];
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogError($"{Name}: 출력 상태 업데이트 중 예외 발생 - {e.Message}");
        }
    }

    private async UniTaskVoid InputStateValidateAsync(Transition[] exInStates)
    {
        try
        {
            await UniTask.WaitWhile(() => OnDeserializing, cancellationToken: _cts.SafeGetToken(out _cts));

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
        catch (OperationCanceledException) { }
    }

    private void UpdateOutputState(Transition[] outputs)
    {
        try
        {
            if (outputs.Length != OutputToken.Count)
            {
                Debug.Log($"{Name}: 출력 설정 무시됨 - 토큰 개수 변경됨 (토큰: {OutputToken.Count}, 출력: {outputs.Length})");
                return;
            }

            for (int i = 0; i < OutputToken.Count; i++)
            {
                OutputToken[i].State = outputs[i];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{Name}: 출력 상태 업데이트 중 예외 발생 - {e.Message}");
        }
    }

    private void SetName()
    {
        object blocker = new();
        InputManager.AddBlocker(blocker);

        TextGetterManager.Set
        (
            rootCanvas: PUMPUiManager.RootCanvas,
            callback: result => Name = result,
            titleString: "Node Name",
            inputString: Name,
            onExit: () => InputManager.RemoveBlocker(blocker)
        );
    }
    #endregion

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 2;

    public override string NodePrefabPath => "PUMP/Prefab/Node/CLASSED";

    protected override string NodeDisplayName => Name;

    protected override float NameTextSize => 24;

    protected override float InEnumeratorXPos => -72f;

    protected override float OutEnumeratorXPos => 72f;

    protected override Vector2 DefaultNodeSize => new Vector2(180f, 50f);

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            contexts.Add(new ContextElement("Edit", () => OpenPanel?.Invoke(this)));
            contexts.Add(new ContextElement("Rename", SetName));

            return contexts;
        }
    }

    protected override void OnAfterInit()
    {
        OnRemove += OnRemoveAdapter;
        ClassedNodePanel.JoinPanel(this);
        MouseListener.OnDoubleClick += _ => OpenPanel?.Invoke(this);
    }

    protected override void OnAddFromPalette()
    {
        ClassedSupport.IsChange = _isChange;
    }

    protected override string DefineInputName(int tpIndex)
    {
        return "in" + tpIndex;
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return "out" + tpIndex;
    }

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return Enumerable.Repeat(TransitionType.Bool.Null(), outputCount).ToArray();
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return Enumerable.Repeat(TransitionType.Bool.Null(), outputCount).ToArray();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OnInputUpdate?.Invoke(args);
    }

    protected override void OnBeforeRemove()
    {
        _cts.SafeCancelAndDispose();
    }

    #region Classed Node Interface
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            Support.NameText.text = value;
            Support.name = value;
        }
    }

    bool IClassedNode.IsChanged
    {
        get => _isChange;
        set
        {
            _isChange = value;
            ClassedSupport.IsChange = _isChange;
        }
    }

    PUMPSaveDataStructure IClassedNode.ModuleStructure { get; set; } = null;
    int IClassedNode.InputCount { get => InputCount; set => InputCount = value; }
    int IClassedNode.OutputCount { get => OutputCount; set => OutputCount = value; }

    public event Action<TransitionEventArgs> OnInputUpdate;
    public event Action<IClassedNode> OpenPanel;

    event Action<IClassedNode> IClassedNode.OnDestroy
    {
        add => _onDeleteActions.Add(value);
        remove => _onDeleteActions.Remove(value);
    }

    public void OutputApplyAll(Transition[] outputs)
    {
        if (outputs.Length != OutputToken.Count)
        {
            UpdateOutputState(outputs);
            return;
        }

        for (int i = 0; i < OutputToken.Count; i++)
        {
            OutputToken[i].State = outputs[i];
        }
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
        InputStateValidateAsync(exInStates).Forget();
    }

    public void OutputStateValidate(Transition[] exOutStates)
    {
        if (exOutStates.Length != OutputToken.Count)
        {
            throw new ArgumentOutOfRangeException($"Expected {OutputToken.Count} elements, but received {exOutStates.Length}.");
        }

        if (!OutputToken.Select(sf => sf.State).SequenceEqual(exOutStates))
        {
            int i = 0;
            foreach (ITypeListenStateful stateful in OutputToken)
            {
                stateful.State = exOutStates[i++];
            }
        }
    }

    public List<Action<TransitionType>> GetInputTypeApplier()
    {
        IPolymorphicStateful[] inputPolymorphic = InputToken.GetPolymorphics();
        return inputPolymorphic.Select(ip => new Action<TransitionType>(ip.SetType)).ToList();
    }

    public List<Action<TransitionType>> GetOutputTypeApplier()
    {
        IPolymorphicStateful[] outputPolymorphic = OutputToken.GetPolymorphics();
        return outputPolymorphic.Select(op => new Action<TransitionType>(op.SetType)).ToList();
    }

    public Node GetNode() => this;
    public Task WaitForDeserializationComplete() => UniTask.WaitUntil(() => !OnDeserializing).AsTask();
    #endregion

    #region SerializeData
    public ClassedNodeSerializeInfo AdditionalArgs
    {
        get
        {
            IClassedNode thisClass = this;
            return new(thisClass.ModuleStructure, thisClass.InputCount, thisClass.OutputCount, thisClass.IsChanged);
        }

        set
        {
            IClassedNode thisClass = this;
            thisClass.InputCount = value._inputCount;
            thisClass.OutputCount = value._outputCount;
            thisClass.ModuleStructure = value._structure;
            thisClass.IsChanged = value._isChange;
        }
    }
    #endregion
}

[Serializable]
public struct ClassedNodeSerializeInfo
{
    public ClassedNodeSerializeInfo (PUMPSaveDataStructure structure, int inputCount, int outputCount, bool isChange)
    {
        _structure = structure;
        _inputCount = inputCount;
        _outputCount = outputCount;
        _isChange = isChange;
    }

    [OdinSerialize] public PUMPSaveDataStructure _structure;
    [OdinSerialize] public bool _isChange;
    [OdinSerialize] public int _inputCount;
    [OdinSerialize] public int _outputCount;
}