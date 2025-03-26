using System;
using TMPro;
using UnityEngine;
using Utils;

public class ClassedNodePanel : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private RectTransform pumpBackgroundParent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextGetter textGetter;
    [SerializeField] private string defaultSaveName = string.Empty;
    [SerializeField] private TextMeshProUGUI tmp;
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

    private void Update()
    {
        if (DataManager.HasCurrent())
        {
            var id = DataManager.GetCurrent().ClassedNode.Id;
            tmp.text = id;
        }
    }

    #region Static Interface
    public static ClassedNodePanel JoinPanel(IClassedNode classedNode)
    {
        if (!TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            RectTransform parent = classedNode.GetNode().Background.Rect.GetRootCanvasRect();
            panel = Instantiate(PanelPrefab, parent).GetComponent<ClassedNodePanel>();
        }

        classedNode.OpenPanel += panel.OpenPanel;
        classedNode.OnDestroy += panel.DataManager.DestroyClassed;
        panel.DataManager.AddNew(classedNode);
        panel.SetActive(false);
        return panel;
    }

    public static ClassedNodePanel GetInstance(RectTransform findStartRect)
    {
        if (TryFindPanel(findStartRect, out ClassedNodePanel panel))
            return panel;

        Debug.LogError("Static - ClassedNodePanel: SetPanel first");
        return null;
    }
    #endregion

    #region Instance Interface
    public void OpenPanel(IClassedNode classedNode)
    {
        if (classedNode == null)
            return;

        SetActive(true);
        OpenBackground(classedNode);
    }

    public void ClosePanel()
    {
        DataManager.DiscardCurrent();
        SetActive(false);
    }

    public void OpenSaveOption()
    {
        textGetter.Set("Save Name", defaultSaveName, DataManager.Push);
    }

    public IClassedNodeDataManager DataManager
    {
        get
        {
            if (_dataManager == null)
            {
                _dataManager = GetComponent<IClassedNodeDataManager>();
                _dataManager.BackgroundGetter = () =>
                {
                    GameObject backgroundObject = Instantiate(BackgroundPrefab, pumpBackgroundParent);
                    PUMPBackground background = backgroundObject.GetComponent<PUMPBackground>();
                    background.initializeOnAwake = false;
                    background.Initialize();
                    backgroundObject.SetActiveDelay(false).Forget();
                    return background;
                };
            }
            return _dataManager;
        }
    }
    #endregion

    #region Privates
    private IClassedNodeDataManager _dataManager;

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
        DataManager.SetCurrent(classedNode);
    }

    private void CloseBackground()
    {
        DataManager.DiscardCurrent();
    }
    #endregion
}

public interface IClassedNodeDataManager
{
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public bool HasCurrent();
    public void SetCurrent(IClassedNode classedNode);
    public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent();
    public void OverrideToCurrent(PUMPSaveDataStructure structure);
    public void DestroyClassed(IClassedNode classedNode);
    public void DiscardCurrent();
    public void AddNew(IClassedNode classedNode);
    public void Push(string name);
}
