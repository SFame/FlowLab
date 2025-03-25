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

    #region Privates
    private Action<bool[]> _classedOnInputUpdateCache;
    private Action _exOutOnStateUpdateCache;

    private Dictionary<IClassedNode, PUMPBackground> ClassedPair { get; set; } = new();
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

    private void OpenBackground(IClassedNode classedNode)
    {
        if (ClassedPair.TryGetValue(classedNode, out PUMPBackground background))
        {
            background.gameObject.SetActive(true);

            return;
        }

        Debug.LogError($"{GetType().Name}: Can't find key on Dictionary");
    }

    private PUMPBackground AddNewPumpBackground()
    {
        GameObject backgroundObject = Instantiate(BackgroundPrefab, pumpBackgroundParent);
        PUMPBackground background = backgroundObject.GetComponent<PUMPBackground>();
        background.initializeOnAwake = false;
        background.Initialize();
        backgroundObject.SetActive(false);
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

        ClassedPair.Add(classedNode, newBackground);
    }

    /// <summary>
    /// Classed와 External 연결
    /// </summary>
    /// <param name="classed"></param>
    /// <param name="exIn"></param>
    /// <param name="exOut"></param>
    private void LinkClassedToExternal(IClassedNode classed, IExternalInput exIn, IExternalOutput exOut)
    {
        classed.InputCount = exIn.GateCount;
        classed.OutputCount = exOut.GateCount;

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
    public static void SetPanel(IClassedNode classedNode)
    {
        if (TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            panel.AddClassedNode(classedNode).Forget();
            return;
        }

        RectTransform parent = classedNode.GetNode().Background.Rect.GetRootCanvasRect();
        GameObject classedNodePanelObject = Instantiate(PanelPrefab, parent);
        ClassedNodePanel classedNodePanel = classedNodePanelObject.GetComponent<ClassedNodePanel>();
        classedNodePanel.AddClassedNode(classedNode).Forget();
        classedNodePanelObject.SetActive(false);
    }

    public static void OpenPanel(IClassedNode classedNode)
    {
        if (TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            panel.gameObject.SetActive(true);
            panel.OpenBackground(classedNode);

            return;
        }

        Debug.LogError("Static - ClassedNodePanel: SetPanel first");
    }
    #endregion
}
