using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

public class PUMPSaveLoadPanel : MonoBehaviour, IRecyclableScrollRectDataSource, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private Button saveButton;
    #endregion
    
    #region Privates
    private const string SAVE_PATH = "node_data.bin";
    private RecyclableScrollRect _scrollRect;
    private PUMPBackground _background;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private TextGetter _textGetter;
    private PUMPSerializeManager _serializer;
    private List<PUMPSaveDataStructure> _saveDatas;
    private bool _initialized = false;
    
    private Vector2 ScreenSize => new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

    private PUMPBackground Background
    {
        get
        {
            _background ??= transform.GetComponentInSibling<PUMPBackground>();
            return _background;
        }
    }

    private CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    private TextGetter TextGetter
    {
        get
        {
            _textGetter ??= GetComponentInChildren<TextGetter>(true);
            return _textGetter;
        }
    }

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= transform.GetComponentInParent<Canvas>().rootCanvas;
            return _rootCanvas;
        }
    }

    private void Awake()
    {
        Initialize().Forget();
    }

    private void OnEnable()
    {
        UpdateAndApply().Forget();
    }

    private async UniTaskVoid Initialize()
    {
        async UniTaskVoid AddNewSave(string saveName)
        {
            List<SerializeNodeInfo> nodeInfos = Background.GetSerializeNodeInfos();
            string capturePath = ((RectTransform)Background.Rect.parent).CaptureToFile(ScreenSize);
            await _serializer.AddData(SAVE_PATH, new(nodeInfos, saveName, capturePath));
            UpdateAndApply().Forget();
        }
        
        if (_initialized)
            return;
        
        _serializer = PUMPSerializeManager.Instance;
        await GetDatasFromManager();
        ScrollRect.Initialize(this);
        
        saveButton?.onClick.AddListener(() =>
        {
            TextGetter.GetInputText("Board name", "", newName => AddNewSave(newName).Forget());
        });
        
        _initialized = true;
    }

    private async UniTask GetDatasFromManager()
    {
        if (_initialized)
            _saveDatas = await _serializer.GetDatas(SAVE_PATH);
    }

    private async UniTaskVoid UpdateAndApply()
    {
        if (!_initialized)
            return;
        
        await GetDatasFromManager();
        if (_saveDatas != null)
            ScrollRect.ReloadData();
    }

    private async UniTaskVoid DeleteData(PUMPSaveDataStructure data)
    {
        Capture.DeleteCaptureFile(data.ImagePath);
        data.Delete();
        await GetDatasFromManager();
        ScrollRect.ReloadData();
    }
    #endregion
    
    #region RecyclableScrollRect
    private RecyclableScrollRect ScrollRect
    {
        get
        {
            _scrollRect ??= GetComponentInChildren<RecyclableScrollRect>();
            return _scrollRect;
        }
    }
    
    public int GetItemCount()
    {
        return _saveDatas?.Count ?? 0;
    }

    public void SetCell(ICell cell, int index)
    {
        SaveScrollElem elem = cell as SaveScrollElem;
        if (elem == null)
            return;
        
        elem.Initialize(_saveDatas[index]);
        elem.OnDoubleClick += data =>
        {
            Background.SetSerializeNodeInfos(data.NodeInfos);
            Background.RecordHistoryOncePerFrame();
            SetActive(false, 0.2f).Forget();
        };
        elem.OnRightClick += (data, eventData) =>
        {
            ContextElement[] contextElements = 
            {
                new
                (
                    text: "Delete",
                    clickAction: () => DeleteData(data).Forget()
                ),
                
                new
                (
                    text: "Overwrite this board",
                    clickAction: () =>
                    {
                        UniTask.Create(async () =>
                        {
                            await UniTask.Yield();
                            string capturePath = Background.Rect.CaptureToFile(ScreenSize);
                            Capture.DeleteCaptureFile(data.ImagePath);
                            data.NodeInfos = Background.GetSerializeNodeInfos();
                            data.ImagePath = capturePath;
                            data.NotifyDataChanged();
                            elem.DataUpdate();
                        });
                    }
                ),
                
                new
                (
                    text: "Rename",
                    clickAction: () =>
                    {
                        TextGetter.GetInputText("New name", data.Name, newName =>
                        {
                            if (data.Name != newName)
                            {
                                data.Name = newName;
                                data.NotifyDataChanged();
                                elem.DataUpdate();
                            }
                        });
                    }
                )
            };
            ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, contextElements);
        };
    }
    #endregion

    public async UniTaskVoid SetActive(bool active, float fadeDuration = 0.4f)
    {
        if (active)
        {
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(true);
        }
        else
            CanvasGroup.alpha = 1f;
        
        float targetAlpha = active ? 1f : 0f;
        await Fade(targetAlpha, fadeDuration);
        
        if (!active)
            gameObject.SetActive(false);
    }

    public async UniTask Fade(float targetAlpha, float duration)
    {
        float startAlpha = CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            
            float t = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        
        CanvasGroup.alpha = targetAlpha;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);
        
        if (result.Count <= 0)
            return;
        
        if (result[0].gameObject == gameObject)
            SetActive(false, 0.2f).Forget();
    }
}
