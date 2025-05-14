using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

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

    public override object GetTag()
    {
        string newId = GetNewId();
        return newId;
    }

    public override bool ValidateBeforeSerialization(PUMPSaveDataStructure structure)
    {
        string id;
        string name;

        try
        {
            id = structure.Tag.AsString();
            name = structure.Name;
        }
        catch (InvalidCastException e)
        {
            Debug.LogError("Tag convert Error: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }

        if (HasCurrent()) // PUMPSaveDataStructure가 Valid할 시, 현재 Classed에 적용 후 true return
        {
            var current = GetCurrent();
            current.ClassedNode.Name = name;
            current.ClassedNode.Id = id;
            current.ClearChangeFlag();
            return true;
        }

        return false;
    }

    // -- DataManager --
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public PUMPBackground BaseBackground { get; set; }

    public void Push(string name)
    {
        PushAsync(name).Forget();
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
        var current = GetCurrent();
        current.PairBackground.SetInfos(structure.NodeInfos, true);
        current.ClassedNode.Id = structure.Tag.ToString();
        current.ClassedNode.Name = structure.Name;
        current.ClassedNode.OutputStateUpdate(current.PairBackground.ExternalOutput.Select(tp => (bool)tp.State).ToArray());
        current.ClearChangeFlag();
        structure.NotifyDataChanged();
    }

    public async UniTask ApplyCurrentById(string id)
    {
        if (!HasCurrent())
        {
            Debug.LogWarning("Has not Current");
            return;
        }

        List<PUMPSaveDataStructure> pumpData = await SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(DataDirectory.PumpAppData, savePath);
        PUMPSaveDataStructure matchedStructure = string.IsNullOrEmpty(id) ?
            null : pumpData.FirstOrDefault(structure => structure.Tag.Equals(id));

        var current = GetCurrent();

        if (matchedStructure != null)
        {
            current.PairBackground.SetInfos(matchedStructure.NodeInfos, true);
            current.ClassedNode.Name = matchedStructure.Name;
            current.ClassedNode.Id = matchedStructure.Tag.ToString();
            current.ClassedNode.OutputStateUpdate(current.PairBackground.ExternalOutput.Select(tp => (bool)tp.State).ToArray());
            current.ClearChangeFlag();
            matchedStructure.NotifyDataChanged();
            return;
        }

        current.PairBackground.SetInfos(new(), true);
        current.ClassedNode.Name = classedNodePanel.defaultSaveName;
        current.ClassedNode.Id = string.Empty;
        current.ClassedNode.OutputStateUpdate(current.PairBackground.ExternalOutput.Select(tp => (bool)tp.State).ToArray());
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

    private Action<bool[]> _classedOnInputUpdateCache;
    private Action _exOutOnStateUpdateCache;

    private async UniTask AddNewAsync(IClassedNode classedNode)
    {
        Loading.Progress prog = Loading.GetProgress();

        prog.SetProgress(20);

        List<PUMPSaveDataStructure> pumpData = await SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(DataDirectory.PumpAppData, savePath);

        prog.SetProgress(60);

        PUMPSaveDataStructure matchedStructure = string.IsNullOrEmpty(classedNode.Id) ?
            null : pumpData.FirstOrDefault(structure => structure.Tag.Equals(classedNode.Id));
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
    /// Classed와 External 연결
    /// </summary>
    private void LinkClassedToExternal(IClassedNode classed, IExternalInput exIn, IExternalOutput exOut)
    {
        if (classed.InputCount != exIn.GateCount || classed.OutputCount != exOut.GateCount)
        {
            classed.InputCount = exIn.GateCount;
            classed.OutputCount = exOut.GateCount;
        }

        _classedOnInputUpdateCache = classedInputStates =>
        {
            for (int i = 0; i < classedInputStates.Length; i++)
            {
                if (exIn[i].State != classedInputStates[i])  // 다를 때만 업데이트 (까먹지마)
                {
                    exIn[i].State = classedInputStates[i];
                }
            }
        };
        classed.OnInputUpdate += _classedOnInputUpdateCache;

        _exOutOnStateUpdateCache = () =>
        {
            classed.OutputStateUpdate(exOut.Select(tp => (bool)tp.State).ToArray());
        };
        exOut.OnStateUpdate += _exOutOnStateUpdateCache;

        classed.InputStateValidate(exIn.Select(tp => (bool)tp.State).ToArray());
    }

    private async UniTask PushAsync(string name)
    {
        PUMPSaveDataStructure newStructure = new()
        {
            Name = name,
            NodeInfos = GetNodeInfos(),
            Tag = GetTag(),
        };

        if (ValidateBeforeSerialization(newStructure))
        {
            await SerializeManagerCatalog.AddData(DataDirectory.PumpAppData, savePath, newStructure);
        }
    }

    private string GetNewId() => Guid.NewGuid().ToString();
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
                Discard();

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
        IsChanged = false;
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
    }
}

public interface ICurrentClassedPairManagerTokenSetter // 외부 클래스에서의 명시적 Setter 접근 방지
{
    public void Set(IClassedNode classedNode, PUMPBackground background);
    public void Terminate();
}