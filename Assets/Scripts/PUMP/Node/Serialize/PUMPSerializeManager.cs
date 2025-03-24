using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using static Utils.Serializer;

/// <summary>
/// 기본 저장경로는 node_data.bin
/// </summary>
public class PUMPSerializeManager : MonoBehaviour
{
    #region Privates
    private static PUMPSerializeManager _instance;
    private static readonly object _lock = new();
    private readonly Dictionary<string, List<PUMPSaveDataStructure>> _saveDatas = new();
    private readonly DataUpdateEvent _onDataUpdated = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private async UniTask GetDataInDictionaryFromFile(string path)
    {
        if (_saveDatas.ContainsKey(path) && _saveDatas[path] != null)
            return;
        
        List<PUMPSaveDataStructure> datas = await LoadDataAsync<List<PUMPSaveDataStructure>>(path);
        datas ??= new();
        
        foreach (PUMPSaveDataStructure data in datas)
        {
            data.SubscribeDeleteRequest(saveStructure => DeleteData(path, saveStructure));
            data.SubscribeUpdateNotification(saveStructure => saveStructure.LastUpdate = DateTime.Now);
            data.SubscribeDeleteRequest(_ => _onDataUpdated.Invoke(path));
            data.SubscribeUpdateNotification(_ => _onDataUpdated.Invoke(path));
        }
        
        _onDataUpdated.AddEvent(path, () => WriteData(path));
        _saveDatas.Add(path, datas);
    }

    private void WriteData(string path)
    {
        _ = SaveDataAsync(path, _saveDatas[path]);
    }
    private void DeleteData(string path, PUMPSaveDataStructure data)
    {
        _saveDatas[path].Remove(data);
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

    public async UniTask AddData(string path, PUMPSaveDataStructure structure)
    {
        await GetDataInDictionaryFromFile(path);
        structure.SubscribeDeleteRequest(saveStructure => DeleteData(path, saveStructure));
        structure.SubscribeUpdateNotification(saveStructure => saveStructure.LastUpdate = DateTime.Now);
        structure.SubscribeDeleteRequest(_ => _onDataUpdated.Invoke(path));
        structure.SubscribeUpdateNotification(_ => _onDataUpdated.Invoke(path));
        structure.LastUpdate = DateTime.Now;
        
        _saveDatas[path].Add(structure);
        
        _onDataUpdated.Invoke(path);
    }

    public async UniTask<List<PUMPSaveDataStructure>> GetDatas(string path)
    {
        await GetDataInDictionaryFromFile(path);
        return _saveDatas[path].ToList();
    }
    #endregion
}

public class PUMPSaveDataStructure
{
    public PUMPSaveDataStructure(List<SerializeNodeInfo> nodeInfos, string name, string imagePath, object tag = null)
    {
        NodeInfos = nodeInfos;
        Name = name;
        ImagePath = imagePath;
        Tag = tag;
    }
    
    #region Serialize Data
    [OdinSerialize] public List<SerializeNodeInfo> NodeInfos { get; set; }
    [OdinSerialize] public string Name { get; set; }
    [OdinSerialize] public string ImagePath { get; set; } // Optional
    [OdinSerialize] public object Tag { get; set; } // Optional
    #endregion
    
    #region Automatic generation
    [OdinSerialize] public DateTime LastUpdate { get; set; }
    #endregion
    
    [field: NonSerialized] private event Action<PUMPSaveDataStructure> DeleteRequest;
    [field: NonSerialized] private event Action<PUMPSaveDataStructure> UpdateNotification;
    
    public void SubscribeDeleteRequest(Action<PUMPSaveDataStructure> action) => DeleteRequest += action;
    public void SubscribeUpdateNotification(Action<PUMPSaveDataStructure> action) => UpdateNotification += action;
    public void Delete() => DeleteRequest?.Invoke(this);
    public void NotifyDataChanged() => UpdateNotification?.Invoke(this);
}

public class DataUpdateEvent
{
    private Dictionary<string, Action> Events { get; } = new();

    public void Invoke(string key)
    {
        if (Events.TryGetValue(key, out Action action))
        {
            action?.Invoke();
        }
    }

    public void AddEvent(string key, Action action)
    {
        if (!Events.TryAdd(key, action))
        {
            Events[key] += action;
        }
    }
}