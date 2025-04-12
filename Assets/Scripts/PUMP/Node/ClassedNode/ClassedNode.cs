using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[ResourceGetter("PUMP/Sprite/ingame/classed_node_palette")]
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
            _mouseListener ??= transform.GetOrAddComponent<UiMouseListener>();
            return _mouseListener;
        }
    }

    private void OnDestroyAdapter(Node node)
    {
        foreach (Action<IClassedNode> action in _onDeleteActions.ToList())  // 순회 도중 Enumerable 변경 예외처리
            action?.Invoke(this);
    }

    private async UniTaskVoid UpdateOutputStateNextFrame(bool[] outputs)
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

    public override string NodePrefebPath => "PUMP/Prefab/Node/CLASSED";

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override string NodeDisplayName => "Classed";

    protected override float TextSize => 24;

    protected override float InEnumeratorXPos => -70f;

    protected override float OutEnumeratorXPos => 70f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(180f, 100f);

    protected override float EnumeratorTPMargin => 10f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            contexts.Add(new ContextElement("Edit Classed Node", () => OpenPanel?.Invoke(this)));
            return contexts;
        }
    }

    protected override void OnLoad_BeforeStateUpdate()
    {
        base.OnLoad_BeforeStateUpdate();
        OnDestroy += OnDestroyAdapter;
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

    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        OnInputUpdate?.Invoke(InputToken.Select(tp => tp.State).ToArray());
    }

    #region Classed Node Interface
    public string Name
    {
        get => _name;
        set
        {
            NodeNameText.text = value;
            name = value;
        }
    }
    public string Id { get; set; } = string.Empty;

    int IClassedNode.InputCount { get => InputCount; set => InputCount = value; }
    int IClassedNode.OutputCount { get => OutputCount; set => OutputCount = value; }

    public event Action<bool[]> OnInputUpdate;
    public event Action<IClassedNode> OpenPanel;

    event Action<IClassedNode> IClassedNode.OnDestroy
    {
        add => _onDeleteActions.Add(value);
        remove => _onDeleteActions.Remove(value);
    }

    public void OutputStateUpdate(bool[] outputs)
    {
        if (outputs.Length != OutputToken.Count)
        {
            UpdateOutputStateNextFrame(outputs).Forget();
            return;
        }

        for (int i = 0; i < OutputToken.Count; i++)
            OutputToken[i].State = outputs[i];
    }

    public Node GetNode() => this;
    #endregion

    #region SerializeData
    public ClassedNodeSerializeInfo AdditionalTArgs
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
    public object AdditionalArgs { get => AdditionalTArgs; set => AdditionalTArgs = (ClassedNodeSerializeInfo)value; }
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