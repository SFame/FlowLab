using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(IClassedNodeDataManager))]
public class ClassedNodePanel : MonoBehaviour, ISeparatorSectorable, ISetVisibleTarget, IDestroyTarget
{
    #region On Inspector
    [SerializeField] private RectTransform pumpBackgroundParent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] public string defaultSaveName = string.Empty;

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
            panel = Instantiate(PanelPrefab).GetComponent<ClassedNodePanel>();

            PUMPBackground background = classedNode.GetNode().Background;
            PUMPSeparator separator = ((ISeparatorSectorable)background).GetSeparator();

            separator.SetOverFull(panel.GetComponent<RectTransform>(), panel);
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
        _baseBackground.OnDestroyed += DestroyPanel;
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

    public void DestroyPanel()
    {
        Destroy(gameObject);
    }

    public void ClosePanelWithAskSave()  // 버튼 인스펙터에서 참조중
    {
        if (DataManager.GetCurrent().IsChanged)
        {
            MessageBoxManager.ShowYesNo(RootCanvas, "Save before exiting?", OpenSaveOptionWithExit, () => CloseWithoutSave().Forget());
            return;
        }

        ClosePanel();
    }

    /// <summary>
    /// 세이브 옵션 띄우고 세이브 된 후 나가지 않음
    /// </summary>
    public void OpenSaveOption()
    {
        TextGetterManager.Set(RootCanvas, DataManager.Push, "Save Name", defaultSaveName);
    }

    /// <summary>
    /// 세이브 옵션 띄우고 세이브 된 후 나감
    /// </summary>
    public void OpenSaveOptionWithExit()
    {
        TextGetterManager.Set(
        RootCanvas,
        saveName =>
        {
            DataManager.Push(saveName);
            ClosePanel();
        },
        "Save Name",
        defaultSaveName);
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
                if (_isInputSliderValueChangingBySystem)
                    return;

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
                if (_isOutputSliderValueChangingBySystem)
                    return;

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

        if (background != null)
        {
            background.ExternalInput.OnCountUpdate -= SetInputSliderValue;
            background.ExternalOutput.OnCountUpdate -= SetOutputSliderValue;
        }
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
                    backgroundObject.name = "ClassedPairBackground";
                    PUMPBackground background = backgroundObject.GetComponent<PUMPBackground>();
                    background.RecordOnInitialize = false;
                    ((ISeparatorSectorable)background).SetSeparator(((ISeparatorSectorable)this).GetSeparator());
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
    private Canvas _rootCanvas;
    private bool _initialized = false;
    private bool _isInputSliderValueChangingBySystem = false;
    private bool _isOutputSliderValueChangingBySystem = false;
    private PUMPSeparator _separator;

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= ((RectTransform)transform).GetRootCanvas();
            return _rootCanvas;
        }
    }

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
        PUMPSeparator separator = ((ISeparatorSectorable)classedNode.GetNode().Background).GetSeparator();
        ClassedNodePanel classedNodePanel = separator.GetComponentInOver<ClassedNodePanel>();

        if (classedNodePanel != null)
        {
            panel = classedNodePanel;
            return true;
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

    private async UniTask CloseWithoutSave()
    {
        if (DataManager.HasCurrent())
        {
            await DataManager.ApplyCurrentById(DataManager.GetCurrent().ClassedNode.Id);
        }

        ClosePanel();
    }

    private void SetInputSliderValue(int value)
    {
        _isInputSliderValueChangingBySystem = true;
        inputCountSlider.value = value;
        _isInputSliderValueChangingBySystem = false;
    }

    private void SetOutputSliderValue(int value)
    {
        _isOutputSliderValueChangingBySystem = true;
        outputCountSlider.value = value;
        _isOutputSliderValueChangingBySystem= false;
    }

    void ISeparatorSectorable.SetSeparator(PUMPSeparator separator)
    {
        _separator = separator;
    }

    PUMPSeparator ISeparatorSectorable.GetSeparator()
    {
        return _separator;
    }

    void ISeparatorSectorable.SetVisible(bool visible)
    {
        if (!visible)
        {
            CloseWithoutSave().Forget();
        }
    }

    void ISetVisibleTarget.SetVisible(bool visible) { } // 하위 Pair Background의 요청을 고의적으로 무시

    void IDestroyTarget.Destroy(object sender)
    {
        if (sender is PUMPBackground background)
        {
            Destroy(background.gameObject);
        }
    }
    #endregion
}