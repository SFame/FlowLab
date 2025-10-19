using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Debug = UnityEngine.Debug;

public class PUMPSaveLoadPanel : MonoBehaviour, IRecyclableScrollRectDataSource, IClassedImportTarget
{
    #region On Inspector
    [SerializeField] private RecyclableScrollRect m_ScrollRect;
    [SerializeField] private Button saveButton;
    [SerializeField] private DataDirectory m_TargetDirectory = DataDirectory.PumpAppData;
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
    private List<PUMPSaveDataStructure> _saveDatas;
    private bool _initialized = false;

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
        var progress = Loading.GetProgress();
        await Initialize();
        await ReloadDataAsync();
        progress.SetComplete();
    }

    private void OnDestroy()
    {
        SerializeManagerCatalog.GetOnDataUpdatedEvent(m_TargetDirectory).RemoveEvent(savePath, ReloadData, GetType().Name);

        foreach (PUMPSaveDataStructure data in _saveDatas)
        {
            data.UnsubscribeUpdateNotification(RefreshEventAdapter);
        }
    }

    async Task IClassedImportTarget.Import(PUMPSaveDataStructure structure)
    {
        structure.SubscribeUpdateNotification(RefreshEventAdapter);
        await SerializeManagerCatalog.AddData(m_TargetDirectory, savePath, structure);

        ReloadDataAsync().Forget();
    }

    private async UniTaskVoid AddNewSave(string saveName)
    {
        if (Extract(saveName, out PUMPSaveDataStructure newStructure))
        {
            newStructure.SubscribeUpdateNotification(RefreshEventAdapter);
            await SerializeManagerCatalog.AddData(m_TargetDirectory, savePath, newStructure);

            ReloadDataAsync().Forget();
        }
    }

    private bool Extract(string saveName, out PUMPSaveDataStructure structure)
    {
        List<SerializeNodeInfo> nodeInfos = extractor.GetNodeInfos();

        PUMPSaveDataStructure newStructure = new(nodeInfos, saveName);
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
            object blocker = new();
            PUMPInputManager inputManager = PUMPInputManager.Current;
            inputManager?.AddBlocker(blocker);

            TextGetterManager.Set
            (
                rootCanvas: PUMPUiManager.RootCanvas,
                callback: newName =>
                {
                    AddNewSave(newName).Forget();
                },
                titleString: "Save name",
                inputString: defaultSaveName,
                onExit: () => inputManager?.RemoveBlocker(blocker)
            );
        });

        SerializeManagerCatalog.GetOnDataUpdatedEvent(m_TargetDirectory).AddEvent(savePath, ReloadData, GetType().Name);
        _initialized = true;
    }


    private async UniTask GetDatasFromManager()
    {
        _saveDatas = await SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(m_TargetDirectory, savePath);

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
        data.Delete();
        await GetDatasFromManager();
        ScrollRect.ReloadData();
    }
    #endregion
    
    #region RecyclableScrollRect
    private RecyclableScrollRect ScrollRect => m_ScrollRect;

    public int GetItemCount()
    {
        return _saveDatas?.Count ?? 0;
    }

    public void SetCell(ICell cell, int index)
    {
        ISaveScrollElem elem = cell as ISaveScrollElem;
        if (elem == null)
            return;

        PUMPSaveDataStructure currentData = _saveDatas[_saveDatas.Count - 1 - index];
        elem.Initialize(currentData);
        elem.OnDoubleClick += data =>
        {
            extractor.ApplyData(data);
            UiController.SetActive(false);
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
                            if (Extract(data.Name, out PUMPSaveDataStructure newStructure))
                            {
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
                        object blocker = new();
                        PUMPInputManager inputManager = PUMPInputManager.Current;
                        inputManager?.AddBlocker(blocker);

                        TextGetterManager.Set
                        (
                            rootCanvas: PUMPUiManager.RootCanvas,
                            callback: newName =>
                            {
                                if (data.Name != newName)
                                {
                                    data.Name = newName;
                                    data.NotifyDataChanged();
                                    elem.Refresh();
                                }
                            },
                            titleString: "New name",
                            inputString: data.Name,
                            onExit: () => inputManager?.RemoveBlocker(blocker)
                        );
                    }
                )
            };
            ContextMenuManager.ShowContextMenu(PUMPUiManager.RootCanvas, eventData.position, contextElements);
        };
    }
    #endregion
}