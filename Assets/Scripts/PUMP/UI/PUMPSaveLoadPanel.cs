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
    private PUMPAppdataSerializeManager _serializer;
    private List<PUMPSaveDataStructure> _saveDatas;
    private bool _initialized = false;

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= ((RectTransform)transform).GetRootCanvas();
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
        SerializeManagerCatalog.GetOnDataUpdatedEvent(DataDirectory.PumpAppData).RemoveEvent(savePath, ReloadData);
    }

    private async UniTaskVoid AddNewSave(string saveName)
    {
        if (Extract(saveName, out PUMPSaveDataStructure newStructure))
        {
            newStructure.SubscribeUpdateNotification(RefreshEventAdapter);
            await SerializeManagerCatalog.AddData(DataDirectory.PumpAppData, savePath, newStructure);

            ReloadDataAsync().Forget();
        }
    }

    private bool Extract(string saveName, out PUMPSaveDataStructure structure)
    {
        List<SerializeNodeInfo> nodeInfos = extractor.GetNodeInfos();
        string imagePath = extractor.GetImagePath();
        object tag = extractor.GetTag();

        PUMPSaveDataStructure newStructure = new(nodeInfos, saveName, imagePath, tag);
        if (extractor.ValidateBeforeSerialization(newStructure))
        {
            structure = newStructure;
            return true;
        }

        structure = null;
        return false;
    }

    private async UniTask Initialize()
    {
        if (_initialized)
            return;
        
        await GetDatasFromManager();
        ScrollRect.Initialize(this);
        
        saveButton?.onClick.AddListener(() =>
        {
            TextGetterManager.Set(RootCanvas, newName => AddNewSave(newName).Forget(), "Save name", defaultSaveName);
        });

        SerializeManagerCatalog.GetOnDataUpdatedEvent(DataDirectory.PumpAppData).AddEvent(savePath, ReloadData);
        _initialized = true;
    }

    private async UniTask GetDatasFromManager()
    {
        _saveDatas = await SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(DataDirectory.PumpAppData, savePath);

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
                            string beforeImagePath = data.ImagePath;
                            if (Extract(data.Name, out PUMPSaveDataStructure newStructure))
                            {
                                Capture.DeleteCaptureFile(beforeImagePath);
                                data.Paste(newStructure);
                                data.NotifyDataChanged();
                                elem.Refresh();
                                return;
                            }
                            Debug.LogError("Extract error");
                        });
                    }
                ),
                
                new
                (
                    text: "Rename",
                    clickAction: () =>
                    {
                        TextGetterManager.Set(RootCanvas, newName =>
                        {
                            if (data.Name != newName)
                            {
                                data.Name = newName;
                                data.NotifyDataChanged();
                                elem.Refresh();
                            }
                        },
                        "New name",
                        data.Name);
                    }
                )
            };
            ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, contextElements);
        };
    }
    #endregion
}