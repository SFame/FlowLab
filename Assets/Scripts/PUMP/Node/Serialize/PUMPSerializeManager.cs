using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using static Utils.Serializer;

public class PUMPSerializeManager : MonoBehaviour
{
    #region Privates
    private const string FILE_NAME = "node_data.bin";
    private static PUMPSerializeManager _instance;
    private static readonly object _lock = new();
    private bool _initialized = false;
    private List<PUMPSaveDataStructure> _saveDatas = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private async UniTask Initialize()
    {
        if (_initialized)
            return;
        
        _saveDatas = await LoadDataAsync<List<PUMPSaveDataStructure>>(FILE_NAME);
        _saveDatas ??= new();

        foreach (PUMPSaveDataStructure saveData in _saveDatas)
        {
            saveData.SubscribeDeleteRequest(DeleteData);
            saveData.SubscribeDeleteRequest(_ => OnDataUpdated?.Invoke());
            saveData.SubscribeUpdateNotification(_ => OnDataUpdated?.Invoke());
            saveData.SubscribeUpdateNotification(data => data.LastUpdate = DateTime.Now);
        }

        OnDataUpdated += WriteData;
        _initialized = true;
    }

    private void WriteData()
    {
        _ = SaveDataAsync(FILE_NAME, _saveDatas);
    }
    private void DeleteData(PUMPSaveDataStructure data)
    {
        _saveDatas.Remove(data);
    }
    #endregion

    #region Interface
    public static PUMPSerializeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock(_lock)
                {
                    GameObject go = new GameObject("NodeSerializeManager");
                    _instance = go.AddComponent<PUMPSerializeManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public async UniTask AddData(List<SerializeNodeInfo> nodeInfos, string name, string imagePath)
    {
        await Initialize();
        
        PUMPSaveDataStructure saveData = new() { NodeInfos = nodeInfos, Name = name, ImagePath = imagePath };
        saveData.SubscribeDeleteRequest(DeleteData);
        saveData.SubscribeDeleteRequest(_ => OnDataUpdated?.Invoke());
        saveData.SubscribeUpdateNotification(_ => OnDataUpdated?.Invoke());
        saveData.SubscribeUpdateNotification(data => data.LastUpdate = DateTime.Now);
        saveData.LastUpdate = DateTime.Now;
        
        _saveDatas.Add(saveData);
        
        OnDataUpdated?.Invoke();
    }

    public async UniTask<List<PUMPSaveDataStructure>> GetDatas()
    {
        await Initialize();
        return _saveDatas.ToList();
    }
    
    public event Action OnDataUpdated;
    #endregion
}

public class PUMPSaveDataStructure
{
    [OdinSerialize] public List<SerializeNodeInfo> NodeInfos { get; set; }
    [OdinSerialize] public string Name { get; set; }
    [OdinSerialize] public DateTime LastUpdate { get; set; }
    [OdinSerialize] public string ImagePath { get; set; }
    
    [field: NonSerialized] private event Action<PUMPSaveDataStructure> DeleteRequest;
    [field: NonSerialized] private event Action<PUMPSaveDataStructure> UpdateNotification;
    
    public void SubscribeDeleteRequest(Action<PUMPSaveDataStructure> action) => DeleteRequest += action;
    public void SubscribeUpdateNotification(Action<PUMPSaveDataStructure> action) => UpdateNotification += action;
    public void Delete() => DeleteRequest?.Invoke(this);
    public void NotifyDataChanged() => UpdateNotification?.Invoke(this);
}