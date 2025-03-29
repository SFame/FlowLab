using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(IClassedNodeDataManager))]
public class ClassedNodePanel : MonoBehaviour
{
    #region On Inspector
    [SerializeField] private RectTransform pumpBackgroundParent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextGetter textGetter;
    [SerializeField] private string defaultSaveName = string.Empty;

    [Space, Header("ExternalCountSlider")]
    [SerializeField] private Slider inputCountSlider;
    [SerializeField] private Slider outputCountSlider;
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

    #region Static Interface
    public static ClassedNodePanel JoinPanel(IClassedNode classedNode)
    {
        if (!TryFindPanel(classedNode, out ClassedNodePanel panel))
        {
            RectTransform parent = classedNode.GetNode().Background.Rect.GetRootCanvasRect();
            panel = Instantiate(PanelPrefab, parent).GetComponent<ClassedNodePanel>();
        }

        panel.Initialize(classedNode);

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
    public void Initialize(IClassedNode classedNode)
    {
        _baseBackground = classedNode.GetNode().Background;
        DataManager.AddNew(classedNode);

        classedNode.OpenPanel += OpenPanel;
        classedNode.OnDestroy += DataManager.DestroyClassed;

        SetActive(false);
    }

    public void OpenPanel(IClassedNode classedNode)
    {
        if (classedNode == null)
            return;

        SetActive(true);
        OpenBackground(classedNode);
    }

    public void ClosePanel()
    {
        CloseBackground();
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
                _dataManager.BaseBackground = _baseBackground;
                _dataManager.BackgroundGetter = () =>
                {
                    GameObject backgroundObject = Instantiate(BackgroundPrefab, pumpBackgroundParent);
                    PUMPBackground background = backgroundObject.GetComponent<PUMPBackground>();
                    background.initializeOnAwake = false;
                    background.Open();
                    Other.InvokeActionDelay(_baseBackground.Open).Forget();
                    return background;
                };
            }
            return _dataManager;
        }
    }
    #endregion

    #region Privates
    private PUMPBackground _baseBackground;
    private IClassedNodeDataManager _dataManager;
    private bool _initialized = false;

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
        SetSlider(DataManager.GetCurrent().PairBackground);
    }

    private void CloseBackground()
    {
        TerminateSlider(DataManager.GetCurrent().PairBackground);
        DataManager.DiscardCurrent();
    }

    public void SetSlider(PUMPBackground background)
    {
        TerminateSlider(background);

        if (background == null)
            return;

        if (inputCountSlider != null)
        {
            inputCountSlider.value = background.ExternalInput.GateCount;
            inputCountSlider.onValueChanged.AddListener(value =>
            {
                int intValue = Mathf.RoundToInt(value);
                background.ExternalInput.GateCount = intValue;
                ((IChangeObserver)background).ReportChanges();
            });
        }

        if (outputCountSlider != null)
        {
            outputCountSlider.value = background.ExternalOutput.GateCount;
            outputCountSlider.onValueChanged.AddListener(value =>
            {
                int intValue = Mathf.RoundToInt(value);
                background.ExternalOutput.GateCount = intValue;
                ((IChangeObserver)background).ReportChanges();
            });
        }

        background.ExternalInput.OnCountUpdate += SetInputSliderValue;
        background.ExternalOutput.OnCountUpdate += SetOutputSliderValue;
    }

    public void TerminateSlider(PUMPBackground background)
    {
        if (inputCountSlider != null)
        {
            inputCountSlider.onValueChanged.RemoveAllListeners();
            inputCountSlider.value = 0f;
        }

        if (outputCountSlider != null)
        {
            outputCountSlider.onValueChanged.RemoveAllListeners();
            outputCountSlider.value = 0f;
        }

        background.ExternalInput.OnCountUpdate -= SetInputSliderValue;
        background.ExternalOutput.OnCountUpdate -= SetOutputSliderValue;
    }

    private void SetInputSliderValue(int value)
    {
        inputCountSlider.value = value;
    }

    private void SetOutputSliderValue(int value)
    {
        outputCountSlider.value = value;
    }
    #endregion
}

public interface IClassedNodeDataManager
{
    public Func<PUMPBackground> BackgroundGetter { get; set; }
    public PUMPBackground BaseBackground { get; set; }
    public bool HasCurrent();
    public void SetCurrent(IClassedNode classedNode);
    public (IClassedNode ClassedNode, PUMPBackground PairBackground) GetCurrent();
    public void OverrideToCurrent(PUMPSaveDataStructure structure);
    public void DestroyClassed(IClassedNode classedNode);
    public void DiscardCurrent();
    public UniTask AddNew(IClassedNode classedNode);
    public void Push(string name);
}
