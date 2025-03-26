using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class ClassedNodePanel : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private RectTransform pumpBackgroundParent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private string savePath;
    #endregion

    #region Const/Static Fields
    private const string PANEL_PREFAB_PATH = "PUMP/Prefab/ClassedNode/ClassedNodePanel";
    private const string BACKGROUND_PREFAB_PATH = "PUMP/Prefab/PUMP_Background";
    private static GameObject _panelPrefab;
    private static GameObject _backgroundPrefab;

    private static GameObject PanelPrefab
    {
        get
        {
            _panelPrefab ??= Resources.Load<GameObject>(PANEL_PREFAB_PATH);
            return _panelPrefab;
        }
    }

    private static GameObject BackgroundPrefab
    {
        get
        {
            _backgroundPrefab ??= Resources.Load<GameObject>(BACKGROUND_PREFAB_PATH);
            return _backgroundPrefab;
        }
    }
    #endregion

    #region Instance Interface
    public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent()
    {
        return CurrentPair.GetCurrent();
    }

    public void SetCurrent(PUMPSaveDataStructure dataStructure)
    {
        var tuple = GetCurrent();

        if (dataStructure == null)
        {
            Debug.LogError($"{GetType().Name}: SetCurrent param is null");
            return;
        }
        if (tuple.ClassedNode == null || tuple.PairBackground == null)
        {
            Debug.LogError($"{GetType().Name}: CurrentPair elements are null");
            return;
        }

        tuple.PairBackground.SetSerializeNodeInfos(dataStructure.NodeInfos);
        dataStructure.Tag = tuple.ClassedNode.GetNewId();
        dataStructure.NotifyDataChanged();
    }

    public void ClosePanel()
    {
        SetActive(false);
    }
    #endregion

    #region Privates
    private Action<bool[]> _classedOnInputUpdateCache;
    private Action _exOutOnStateUpdateCache;

    private Dictionary<IClassedNode, PUMPBackground> ClassedDict { get; set; } = new();
    private ClassedPairManagedStruct CurrentPair { get; set; } = new();

    private void SetActive(bool active)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = active ? 1f : 0f;
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;
    }

    private static bool TryFindPanel(IClassedNode classedNode, out ClassedNodePanel panel)
    {
        RectTransform parent = classedNode.GetNode().Background.Rect.GetRootCanvasRect();
        foreach (RectTransform child in parent)
        {
            if (child.TryGetComponent(out ClassedNodePanel findPanel))
            {
                panel = findPanel;
                return true;
            }
        }
        panel = null;
        return false;
    }

    private static bool TryFindPanel(RectTransform rect, out ClassedNodePanel panel)
    {
        RectTransform parent = rect.GetRootCanvasRect();
        foreach (RectTransform child in parent)
        {
            if (child.TryGetComponent(out ClassedNodePanel findPanel))
            {
                panel = findPanel;
                return true;
            }
        }
        panel = null;
        return false;
    }

    private void OpenBackground(IClassedNode classedNode)
    {
        if (ClassedDict.TryGetValue(classedNode, out PUMPBackground background))
        {
            CurrentPair.Set(classedNode, background);

            return;
        }

        Debug.LogError($"{GetType().Name}: Can't find key on Dictionary");
    }

    private void CloseBackground()
    {
        CurrentPair.Discard();
    }

    private PUMPBackground AddNewPumpBackground()
    {
        GameObject backgroundObject = Instantiate(BackgroundPrefab, pumpBackgroundParent);
        PUMPBackground background = backgroundObject.GetComponent<PUMPBackground>();
        background.initializeOnAwake = false;
        background.Initialize();
        backgroundObject.SetActiveDelay(false).Forget();
        return background;
    }

    private async UniTask AddClassedNode(IClassedNode classedNode)
    {
        List<PUMPSaveDataStructure> pumpData = await PUMPSerializeManager.GetDatas(savePath);
        PUMPSaveDataStructure matchedStructure = pumpData.FirstOrDefault(structure => structure.Tag.Equals(classedNode.Id));

        PUMPBackground newBackground = AddNewPumpBackground();
        if (matchedStructure != null)
        {
            newBackground.SetSerializeNodeInfos(matchedStructure.NodeInfos);
        }

        LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);

        newBackground.ExternalInput.OnCountUpdate += () =>
        {
            classedNode.OnInputUpdate -= _classedOnInputUpdateCache;
            LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);
        };
        newBackground.ExternalOutput.OnCountUpdate += () =>
        {
            newBackground.ExternalOutput.OnStateUpdate -= _exOutOnStateUpdateCache;
            LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);
        };

        ClassedDict.Add(classedNode, newBackground);
    }

    /// <summary>
    /// Classed와 External 연결
    /// </summary>
    /// <param name="classed"></param>
    /// <param name="exIn"></param>
    /// <param name="exOut"></param>
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
            classed.OutputUpdate(exOut.Select(tp => tp.State).ToArray());
        };
        exOut.OnStateUpdate += _exOutOnStateUpdateCache;
    }
    #endregion

    #region Interface
    public static ClassedNodePanel JoinPanel(IClassedNode classedNode)
    {
        if (TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            panel.AddClassedNode(classedNode).Forget();
            return panel;
        }

        RectTransform parent = classedNode.GetNode().Background.Rect.GetRootCanvasRect();
        ClassedNodePanel classedNodePanel = Instantiate(PanelPrefab, parent).GetComponent<ClassedNodePanel>();
        classedNodePanel.AddClassedNode(classedNode).Forget();
        classedNodePanel.SetActive(false);
        return classedNodePanel;
    }

    public static void OpenPanel(IClassedNode classedNode)
    {
        if (TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            panel.SetActive(true);
            panel.OpenBackground(classedNode);

            return;
        }

        Debug.LogError("Static - ClassedNodePanel: SetPanel first");
    }

    public static ClassedNodePanel GetInstance(RectTransform findStartRect)
    {
        if (TryFindPanel(findStartRect, out ClassedNodePanel panel))
            return panel;

        Debug.LogError("Static - ClassedNodePanel: SetPanel first");
        return null;
    }
    #endregion

    private class ClassedPairManagedStruct
    {
        private IClassedNode _currentClassed;
        private PUMPBackground _pairBackground;

        public void Set(IClassedNode classedNode, PUMPBackground background)
        {
            if (_pairBackground != null || classedNode != null)
                Discard();

            if (classedNode == null || background == null)
            {
                Debug.LogError($"{GetType().Name}: Param is null");
                return;
            }

            _currentClassed = classedNode;
            _pairBackground = background;

            _pairBackground.gameObject.SetActive(true);
        }

        public void Discard()
        {
            _pairBackground?.gameObject?.SetActive(false);
            _currentClassed = null;
            _pairBackground = null;
        }

        public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent()
        {
            return (_currentClassed, _pairBackground);
        }
    }
}
