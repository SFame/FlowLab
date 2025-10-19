using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ClassedNodeExtractor : SaveLoadStructureExtractor, IClassedNodeDataManager
{
    #region OnInspector
    [SerializeField] private ClassedNodePanel classedNodePanel;
    [SerializeField] private RectTransform captureTargetBackground;
    [SerializeField] private string savePath = "classed_node_data.bin";
    #endregion

    #region Interface
    // -- Extractor --
    public override void ApplyData(PUMPSaveDataStructure structure)
    {
        if (!HasCurrent())
        {
            Debug.LogError($"{GetType().Name}: ApplyData() but has not Current");
            return;
        }

        PUMPBackground pairBackground = CurrentPair.GetCurrent().PairBackground;
        classedNodePanel.TerminateSlider(pairBackground);
        OverrideToCurrent(structure);
        classedNodePanel.SetSlider(pairBackground);
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        if (HasCurrent())
        {
            return GetCurrent().PairBackground.GetInfos();
        }

        return null;
    }

    public override object GetTag() => null;

    public override bool ValidateBeforeSerialization(PUMPSaveDataStructure structure)
    {
        if (HasCurrent()) // PUMPSaveDataStructure가 Valid할 시, 현재 Classed에 적용 후 true return
        {
            var current = GetCurrent();
            current.ClassedNode.Name = structure.Name;
            current.ClassedNode.ModuleStructure = structure;
            current.ClearChangeFlag();
            return true;
        }

        return false;
    }

    // -- DataManager --
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public PUMPBackground BaseBackground { get; set; }

    public void Push(string name, bool pushDb)
    {
        if (!HasCurrent())
        {
            return;
        }

        PUMPSaveDataStructure newStructure = new()
        {
            Name = name,
            NodeInfos = GetNodeInfos(),
        };

        IClassedNode currentNode = GetCurrent().ClassedNode;
        currentNode.ModuleStructure = newStructure;
        currentNode.Name = name;

        if (pushDb)
        {
            PushToDatabaseAsync(newStructure).Forget();
        }
    }

    public CurrentClassedPairManagerToken GetCurrent()
    {
        return CurrentPair.GetCurrent();
    }

    public void SetCurrent(IClassedNode classedNode)
    {
        if (ClassedDict.TryGetValue(classedNode, out PUMPBackground pairBackground))
        {
            CurrentPair.Set(classedNode, pairBackground, BaseBackground);
            return;
        }

        Debug.LogError($"{GetType().Name}: Can't find key on Dictionary");
    }

    /// <summary>
    /// 현재 Current Background에 오버라이딩, 
    /// </summary>
    public void OverrideToCurrent(PUMPSaveDataStructure structure)
    {
        if (structure == null)
        {
            Debug.LogError($"{GetType().Name}: OverrideCurrent param is null");
            return;
        }
        if (!HasCurrent())
        {
            Debug.LogError($"{GetType().Name}: CurrentPair elements are null");
            return;
        }
        CurrentClassedPairManagerToken current = GetCurrent();
        current.PairBackground.SetInfos(structure.NodeInfos, true);
        current.ClassedNode.ModuleStructure = structure;
        current.ClassedNode.Name = structure.Name;
        SyncType(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
        AttachTypeApplier(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
        current.ClassedNode.OutputApplyAll(current.PairBackground.ExternalOutput.Select(tp => tp.State).ToArray());
        current.ClearChangeFlag();
        structure.NotifyDataChanged();
    }

    public void ApplyCurrentByStructure(PUMPSaveDataStructure structure)
    {
        if (!HasCurrent())
        {
            Debug.LogWarning("Has not Current");
            return;
        }

        CurrentClassedPairManagerToken current = GetCurrent();

        if (structure != null)
        {
            current.PairBackground.SetInfos(structure.NodeInfos, true);
            current.ClassedNode.ModuleStructure = structure;
            current.ClassedNode.Name = structure.Name;
            SyncType(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
            AttachTypeApplier(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
            current.ClassedNode.OutputApplyAll(current.PairBackground.ExternalOutput.Select(tp => tp.State).ToArray());
            current.ClearChangeFlag();
            structure.NotifyDataChanged();
            return;
        }

        current.PairBackground.SetInfos(new(), true);
        current.ClassedNode.ModuleStructure = null;
        current.ClassedNode.Name = classedNodePanel.defaultSaveName;
        SyncType(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
        AttachTypeApplier(current.ClassedNode, current.PairBackground.ExternalInput, current.PairBackground.ExternalOutput);
        current.ClassedNode.OutputApplyAll(current.PairBackground.ExternalOutput.Select(tp => tp.State).ToArray());
        current.ClearChangeFlag();
    }

    public void DestroyClassed(IClassedNode classedNode)
    {
        if (ClassedDict.TryGetValue(classedNode, out PUMPBackground background))
        {
            var current = CurrentPair.GetCurrent();
            if (current.ClassedNode == classedNode || current.PairBackground == background)
                DiscardCurrent();

            background.Destroy();
            // Destroy이벤트로 호출되기 때문에 classedNode는 파괴하지 않음.

            ClassedDict.Remove(classedNode);
        }
    }

    public void DiscardCurrent()
    {
        CurrentPair.Discard();
    }

    public UniTask AddNew(IClassedNode classedNode)
    {
        return AddNewAsync(classedNode);
    }

    public bool HasCurrent()
    {
        return GetCurrent().HasCurrent;
    }
    #endregion

    #region Privates
    private Dictionary<IClassedNode, PUMPBackground> ClassedDict { get; set; } = new();
    private CurrentClassedPairManager CurrentPair { get; set; } = new();

    private Action<TransitionEventArgs> _classedOnInputUpdateCache;
    private Action<TransitionEventArgs> _exOutOnStateUpdateCache;

    private List<Action<TransitionType>> _inputTypeApplierCache;
    private List<Action<TransitionType>> _outputTypeApplierCache;

    private async UniTask AddNewAsync(IClassedNode classedNode)
    {
        Loading.Progress prog = Loading.GetProgress();

        prog.SetProgress(20);

        await Loading.AddTask(classedNode.WaitForDeserializationComplete());

        prog.SetProgress(60);

        PUMPSaveDataStructure matchedStructure = classedNode.ModuleStructure;
        PUMPBackground newBackground = BackgroundGetter?.Invoke();
        if (matchedStructure != null)
        {
            newBackground.SetInfos(matchedStructure.NodeInfos, true);
            classedNode.Name = matchedStructure.Name;
        }

        prog.SetProgress(80);

        LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);

        newBackground.ExternalInput.OnCountUpdate += _ => OnExternalCountUpdateHandler(classedNode, newBackground);
        newBackground.ExternalOutput.OnCountUpdate += _ => OnExternalCountUpdateHandler(classedNode, newBackground);

        ClassedDict.Add(classedNode, newBackground);

        prog.SetComplete();
    }

    /// <summary>
    /// External의 Count 변경 시 실행
    /// </summary>
    private void OnExternalCountUpdateHandler(IClassedNode classedNode, PUMPBackground pairBackground)
    {
        classedNode.OnInputUpdate -= _classedOnInputUpdateCache;
        pairBackground.ExternalOutput.OnStateUpdate -= _exOutOnStateUpdateCache;
        LinkClassedToExternal(classedNode, pairBackground.ExternalInput, pairBackground.ExternalOutput);
    }

    /// <summary>
    /// Gateway의 각각의 Adapter의 Type Update 이벤트에 ClassedNode 의 TP의 타입 업데이트 액션 등록
    /// </summary>
    private void SubscribeBeforeTypeUpdate(List<Action<TransitionType>> applier, IExternalGateway gateway)
    {
        if (applier.Count != gateway.Count())
        {
            Debug.LogError($"Adapter({gateway.Count()})와 ClassedNode의 TP({applier.Count}) 개수 불일치");
            return;
        }

        for (int i = 0; i < applier.Count; i++)
        {
            gateway[i].OnBeforeTypeChange += applier[i];
        }
    }

    private void SubscribeTypeUpdate(List<Action<TransitionType>> applier, IExternalGateway gateway)
    {
        if (applier.Count != gateway.Count())
        {
            Debug.LogError($"Adapter({gateway.Count()})와 ClassedNode의 TP({applier.Count}) 개수 불일치");
            return;
        }

        for (int i = 0; i < applier.Count; i++)
        {
            gateway[i].OnTypeChanged += applier[i];
        }
    }

    private void UnsubscribeBeforeTypeUpdate(List<Action<TransitionType>> applier, IExternalGateway gateway)
    {
        if (applier == null || applier.Count != gateway.Count())
            return;

        for (int i = 0; i < applier.Count; i++)
        {
            gateway[i].OnBeforeTypeChange -= applier[i];
        }
    }

    private void UnsubscribeTypeUpdate(List<Action<TransitionType>> applier, IExternalGateway gateway)
    {
        if (applier == null || applier.Count != gateway.Count())
            return;

        for (int i = 0; i < applier.Count; i++)
        {
            gateway[i].OnTypeChanged -= applier[i];
        }
    }

    /// <summary>
    /// Classed와 Background 타입 이벤트 동기화
    /// </summary>
    private void AttachTypeApplier(IClassedNode classed, IExternalInput exIn, IExternalOutput exOut)
    {
        UnsubscribeBeforeTypeUpdate(_inputTypeApplierCache, exIn);
        UnsubscribeTypeUpdate(_outputTypeApplierCache, exOut);

        _inputTypeApplierCache = classed.GetInputTypeApplier();
        _outputTypeApplierCache = classed.GetOutputTypeApplier();

        SubscribeBeforeTypeUpdate(_inputTypeApplierCache, exIn);
        SubscribeTypeUpdate(_outputTypeApplierCache, exOut);
    }

    /// <summary>
    /// Classed와 Background 타입 동기화
    /// </summary>
    private void SyncType(IClassedNode classed, IExternalInput exIn, IExternalOutput exOut)
    {
        List<Action<TransitionType>> inputTypeApplier = classed.GetInputTypeApplier();
        List<Action<TransitionType>> outputTypeApplier = classed.GetOutputTypeApplier();

        if (inputTypeApplier.Count != exIn.Count())
        {
            Debug.Log($"입력 개수 불일치: inputTypeApplier: ({inputTypeApplier.Count})가 exIn: ({exIn.Count()})");
            return;
        }
        if (outputTypeApplier.Count != exOut.Count())
        {
            Debug.Log($"출력 개수 불일치: outputTypeApplier: ({outputTypeApplier.Count})가 exOut: ({exOut.Count()})");
            return;
        }

        for (int i = 0; i < inputTypeApplier.Count; i++)
        {
            inputTypeApplier[i]?.Invoke(exIn[i].Type);
        }

        for (int i = 0; i < outputTypeApplier.Count; i++)
        {
            outputTypeApplier[i]?.Invoke(exOut[i].Type);
        }
    }

    /// <summary>
    /// Classed와 External 연결
    /// </summary>
    private void LinkClassedToExternal(IClassedNode classed, IExternalInput exIn, IExternalOutput exOut)
    {
        if (classed.InputCount != exIn.GateCount || classed.OutputCount != exOut.GateCount)
        {
            classed.InputCount = exIn.GateCount;
            classed.OutputCount = exOut.GateCount;
        }

        // Type sync & applier attach
        SyncType(classed, exIn, exOut);
        AttachTypeApplier(classed, exIn, exOut);

        // On Classed Node Input Update
        _classedOnInputUpdateCache = args =>
        {
            if (args.Index < 0 || args.Index >= exIn.GateCount)
            {
                Debug.LogError($"ClassedNodeExtractor.LinkClassedToExternal: args.Index out of range: {args.Index}");
                return;
            }

            exIn[args.Index].State = args.State;
        };
        classed.OnInputUpdate += _classedOnInputUpdateCache;

        // On External Output Updated
        _exOutOnStateUpdateCache = args =>
        {
            classed.OutputApply(args);
        };
        exOut.OnStateUpdate += _exOutOnStateUpdateCache;

        classed.OutputStateValidate(exOut.Select(tp => tp.State).ToArray());
        classed.InputStateValidate(exIn.Select(tp => tp.State).ToArray());
    }

    private async UniTask PushToDatabaseAsync(PUMPSaveDataStructure structure)
    {
        if (ValidateBeforeSerialization(structure))
        {
            await SerializeManagerCatalog.AddData(DataDirectory.PumpAppData, savePath, structure);
        }
    }
    #endregion

    #region Private Class
    private class CurrentClassedPairManager
    {
        private IClassedNode _currentClassed;
        private PUMPBackground _pairBackground;
        private PUMPBackground _baseBackground;
        private CurrentClassedPairManagerToken _currentToken = new();
        private ICurrentClassedPairManagerTokenSetter _tokenSetter;
        private ICurrentClassedPairManagerTokenSetter TokenSetter
        {
            get
            {
                _tokenSetter ??= _currentToken;
                return _tokenSetter;
            }
        }

        public void Set(IClassedNode classedNode, PUMPBackground pairBackground, PUMPBackground baseBackground)
        {
            if (_baseBackground == null && baseBackground == null)  // 베이스 백그라운드 우선 확인
            {
                Debug.LogError($"{GetType().Name}: ParentBackground is null");
                return;
            }

            _baseBackground = baseBackground;

            if (_pairBackground != null || classedNode != null)
            {
                Discard();
            }

            if (classedNode == null || pairBackground == null)
            {
                Debug.LogError($"{GetType().Name}: Param is null");
                return;
            }

            _currentClassed = classedNode;
            _pairBackground = pairBackground;

            TokenSetter.Set(_currentClassed, _pairBackground);

            _pairBackground.Open();
        }

        public void Discard()
        {
            TokenSetter.Terminate();
            _pairBackground?.Close();
            _currentClassed = null;
            _pairBackground = null;
            _baseBackground?.Open();
        }

        public CurrentClassedPairManagerToken GetCurrent()
        {
            return _currentToken;
        }
    }
    #endregion
}

public class CurrentClassedPairManagerToken : ICurrentClassedPairManagerTokenSetter
{
    public IClassedNode ClassedNode { get; private set; }
    public PUMPBackground PairBackground { get; private set; }
    public bool IsChanged { get; private set; }
    public bool HasCurrent => (!ClassedNode.IsUnityNull()) && PairBackground != null;

    public void ClearChangeFlag()
    {
        IsChanged = false;
        SetClassedIsChange(false);
    }

    void ICurrentClassedPairManagerTokenSetter.Set(IClassedNode classedNode, PUMPBackground background)
    {
        ClassedNode = classedNode;
        PairBackground = background;

        if (!HasCurrent)
        {
            Debug.LogError($"{GetType().Name}: Field is Null => {ClassedNode} / {PairBackground}");
            return;
        }

        PairBackground.OnChanged += OnChangedHandler;
        IsChanged = classedNode.IsChanged;
    }

    void ICurrentClassedPairManagerTokenSetter.Terminate()
    {
        if (PairBackground != null)
        { 
            PairBackground.OnChanged -= OnChangedHandler;
        }
            
        ClassedNode = null;
        PairBackground = null;
        IsChanged = false;
    }

    private void OnChangedHandler()
    {
        IsChanged = true;
        SetClassedIsChange(true);
    }

    private void SetClassedIsChange(bool isChange)
    {
        if (ClassedNode != null)
        {
            ClassedNode.IsChanged = isChange;
        }
    }
}

public interface ICurrentClassedPairManagerTokenSetter // 외부 클래스에서의 명시적 Setter 접근 방지
{
    public void Set(IClassedNode classedNode, PUMPBackground background);
    public void Terminate();
}