using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class PUMPSaveLoadPanel : MonoBehaviour, IRecyclableScrollRectDataSource
{
    #region On Inspector
    [SerializeField] private Button saveButton;
    [SerializeField] private string savePath;
    [SerializeField] private SaveLoadStructureExtractor extractor;
    [SerializeField] private TextGetter textGetter;
    [SerializeField] private string defaultSaveName = string.Empty;
    #endregion

    #region Interface
    public void ReloadData()
    {
        ReloadDataAsync().Forget();
    }
    #endregion

    #region Privates
    private SaveLoadUiController _uiController;
    private RecyclableScrollRect _scrollRect;
    private Canvas _rootCanvas;
    private PUMPSerializeManager _serializer;
    private List<PUMPSaveDataStructure> _saveDatas;
    private bool _initialized = false;

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= transform.GetComponentInParent<Canvas>().rootCanvas;
            return _rootCanvas;
        }
    }

    private SaveLoadUiController UiController
    {
        get
        {
            _uiController ??= GetComponent<SaveLoadUiController>();
            return _uiController;
        }
    }

    private async void Awake()
    {
        await Initialize();
        ReloadDataAsync().Forget();
    }

    private void OnDestroy()
    {
        PUMPSerializeManager.OnDataUpdated.RemoveEvent(savePath, ReloadData);
    }

    private async UniTaskVoid AddNewSave(string saveName)
    {
        List<SerializeNodeInfo> nodeInfos = extractor.GetNodeInfos();
        string imagePath = extractor.GetImagePath();
        object tag = extractor.GetTag();

        PUMPSaveDataStructure newStructure = new(nodeInfos, saveName, imagePath, tag);
        newStructure.SubscribeUpdateNotification(RefreshEventAdapter);
        await PUMPSerializeManager.AddData(savePath, newStructure);

        ReloadDataAsync().Forget();
    }

    private async UniTask Initialize()
    {
        if (_initialized)
            return;
        
        await GetDatasFromManager();
        ScrollRect.Initialize(this);
        
        saveButton?.onClick.AddListener(() =>
        {
            textGetter.Set("Save name", defaultSaveName, newName => AddNewSave(newName).Forget());
        });

        PUMPSerializeManager.OnDataUpdated.AddEvent(savePath, ReloadData);
        _initialized = true;
    }

    private async UniTask GetDatasFromManager()
    {
        _saveDatas = await PUMPSerializeManager.GetDatas(savePath);

        foreach (PUMPSaveDataStructure data in _saveDatas)
            data.SubscribeUpdateNotification(RefreshEventAdapter);
    }

    private void RefreshEventAdapter(PUMPSaveDataStructure structure)
    {
        ScrollRect.ReloadData();
    }

    private async UniTask ReloadDataAsync()
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
        ISaveScrollElem elem = cell as ISaveScrollElem;
        if (elem == null)
            return;
        
        elem.Initialize(_saveDatas[index]);
        elem.OnDoubleClick += data =>
        {
            extractor.ApplyData(data);
            UiController.SetActive(false, 0.2f).Forget();
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
                            Capture.DeleteCaptureFile(data.ImagePath);
                            data.NodeInfos = extractor.GetNodeInfos();
                            data.ImagePath = extractor.GetImagePath();
                            data.Tag = extractor.GetTag();
                            data.NotifyDataChanged();
                            elem.Refresh();
                        });
                    }
                ),
                
                new
                (
                    text: "Rename",
                    clickAction: () =>
                    {
                        textGetter.Set("New name", data.Name, newName =>
                        {
                            if (data.Name != newName)
                            {
                                data.Name = newName;
                                data.NotifyDataChanged();
                                elem.Refresh();
                            }
                        });
                    }
                )
            };
            ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, contextElements);
        };
    }
    #endregion
}