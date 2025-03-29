using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
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
        classedNodePanel.TerminateSlider(CurrentPair.GetCurrent().PairBackground);
        OverrideToCurrent(structure);
        classedNodePanel.SetSlider(CurrentPair.GetCurrent().PairBackground);
    }

    public override string GetImagePath()
    {
        return captureTargetBackground.CaptureToFile();
    }

    public override List<SerializeNodeInfo> GetNodeInfos()
    {
        return GetCurrent().PairBackground.GetSerializeNodeInfos();
    }

    public override object GetTag()
    {
        string newId = GetNewId();
        GetCurrent().ClassedNode.Id = newId;
        return newId;
    }

    // -- DataManager --
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public PUMPBackground BaseBackground { get; set; }

    public void Push(string name)
    {
        PushAsync(name).Forget();
    }

    public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent()
    {
        return CurrentPair.GetCurrent();
    }

    public void SetCurrent(IClassedNode classedNode)
    {
        if (ClassedDict.TryGetValue(classedNode, out PUMPBackground background))
        {
            CurrentPair.Set(classedNode, background, BaseBackground);
            return;
        }

        Debug.LogError($"{GetType().Name}: Can't find key on Dictionary");
    }

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
        var tuple = GetCurrent();
        tuple.PairBackground.SetSerializeNodeInfos(structure.NodeInfos);
        tuple.ClassedNode.Id = structure.Tag.ToString();
        tuple.ClassedNode.Name = structure.Name;
        structure.NotifyDataChanged();
    }

    public void DestroyClassed(IClassedNode classedNode)
    {
        if (ClassedDict.TryGetValue(classedNode, out PUMPBackground background))
        {
            var current = CurrentPair.GetCurrent();
            if (current.ClassedNode == classedNode || current.PairBackground == background)
                CurrentPair.Discard();

            Destroy(background.gameObject);
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
        var tuple = GetCurrent();
        return tuple.ClassedNode != null && tuple.PairBackground != null;
    }
    #endregion

    #region Privates
    private Dictionary<IClassedNode, PUMPBackground> ClassedDict { get; set; } = new();
    private ClassedPairManagedStruct CurrentPair { get; set; } = new();

    private Action<bool[]> _classedOnInputUpdateCache;
    private Action _exOutOnStateUpdateCache;

    private async UniTask AddNewAsync(IClassedNode classedNode)
    {
        List<PUMPSaveDataStructure> pumpData = await PUMPSerializeManager.GetDatas(savePath);
        PUMPSaveDataStructure matchedStructure = string.IsNullOrEmpty(classedNode.Id) ?
            null : pumpData.FirstOrDefault(structure => structure.Tag.Equals(classedNode.Id));

        PUMPBackground newBackground = BackgroundGetter?.Invoke();
        if (matchedStructure != null)
        {
            newBackground.SetSerializeNodeInfos(matchedStructure.NodeInfos);
            classedNode.Name = matchedStructure.Name;
        }

        LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);

        newBackground.ExternalInput.OnCountUpdate += _ =>
        {
            classedNode.OnInputUpdate -= _classedOnInputUpdateCache;
            newBackground.ExternalOutput.OnStateUpdate -= _exOutOnStateUpdateCache;
            LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);
        };
        newBackground.ExternalOutput.OnCountUpdate += _ =>
        {
            classedNode.OnInputUpdate -= _classedOnInputUpdateCache;
            newBackground.ExternalOutput.OnStateUpdate -= _exOutOnStateUpdateCache;
            LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);
        };

        ClassedDict.Add(classedNode, newBackground);
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

        _classedOnInputUpdateCache = states =>
        {
            for (int i = 0; i < states.Length; i++)
            {
                exIn[i].State = states[i];
            }
        };
        classed.OnInputUpdate += _classedOnInputUpdateCache;

        _exOutOnStateUpdateCache = () =>
        {
            classed.OutputStateUpdate(exOut.Select(tp => tp.State).ToArray());
        };
        exOut.OnStateUpdate += _exOutOnStateUpdateCache;
    }

    private async UniTask PushAsync(string name)
    {
        PUMPSaveDataStructure newStructure = new()
        {
            Name = name,
            NodeInfos = GetNodeInfos(),
            ImagePath = GetImagePath(),
            Tag = GetTag(),
        };

        GetCurrent().ClassedNode.Name = name;

        await PUMPSerializeManager.AddData(savePath, newStructure);
    }

    private string GetNewId() => Guid.NewGuid().ToString();
    #endregion

    #region Private Class
    private class ClassedPairManagedStruct
    {
        private IClassedNode _currentClassed;
        private PUMPBackground _pairBackground;
        private PUMPBackground _baseBackground;

        public void Set(IClassedNode classedNode, PUMPBackground background, PUMPBackground baseBackground)
        {
            if (_baseBackground == null && baseBackground == null)
            {
                Debug.LogError($"{GetType().Name}: ParentBackground is null");
                return;
            }

            _baseBackground = baseBackground;

            if (_pairBackground != null || classedNode != null)
                Discard();

            if (classedNode == null || background == null)
            {
                Debug.LogError($"{GetType().Name}: Param is null");
                return;
            }

            _currentClassed = classedNode;
            _pairBackground = background;

            _pairBackground.Open();
        }

        public void Discard()
        {
            _pairBackground?.Close();
            _currentClassed = null;
            _pairBackground = null;
            _baseBackground?.Open();
        }

        public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent()
        {
            return (_currentClassed, _pairBackground);
        }
    }
    #endregion
}
