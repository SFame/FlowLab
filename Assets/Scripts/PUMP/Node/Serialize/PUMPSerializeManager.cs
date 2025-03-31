using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using static Utils.Serializer;

/// <summary>
/// 기본 저장경로는 node_data.bin
/// </summary>
public class PUMPSerializeManager
{
    #region Privates
    private static PUMPSerializeManager _instance;
    private readonly Dictionary<string, List<PUMPSaveDataStructure>> _saveDatas = new();
    private readonly PairEvent _onDataUpdated = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private PUMPSerializeManager() { }

    private static PUMPSerializeManager Instance
    {
        get
        {
            _instance ??= new PUMPSerializeManager();
            return _instance;
        }
    }

    private async UniTask GetDataInDictionaryFromFile(string path)
    {
        await _semaphore.WaitAsync();

        try
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
        finally
        {
            _semaphore.Release();
        }
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
    public static PairEvent OnDataUpdated => Instance._onDataUpdated;

    public static async UniTask AddData(string path, PUMPSaveDataStructure structure)
    {
        await Instance.GetDataInDictionaryFromFile(path);
        structure.SubscribeDeleteRequest(saveStructure => Instance.DeleteData(path, saveStructure));
        structure.SubscribeUpdateNotification(saveStructure => saveStructure.LastUpdate = DateTime.Now);
        structure.SubscribeDeleteRequest(_ => Instance._onDataUpdated.Invoke(path));
        structure.SubscribeUpdateNotification(_ => Instance._onDataUpdated.Invoke(path));
        structure.LastUpdate = DateTime.Now;

        Instance._saveDatas[path].Add(structure);

        Instance._onDataUpdated.Invoke(path);
    }

    public static async UniTask<List<PUMPSaveDataStructure>> GetDatas(string path)
    {
        await Instance.GetDataInDictionaryFromFile(path);
        return Instance._saveDatas[path].ToList();
    }
    #endregion
}

public class PUMPSaveDataStructure
{
    public PUMPSaveDataStructure() { }

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

    public void SubscribeDeleteRequest(Action<PUMPSaveDataStructure> action)
    {
        if (DeleteRequest != null)
        {
            foreach (Delegate d in DeleteRequest.GetInvocationList())
            {
                if (d.Equals(action))
                    return;
            }
        }
        DeleteRequest += action;
    }

    public void SubscribeUpdateNotification(Action<PUMPSaveDataStructure> action)
    {
        if (UpdateNotification != null)
        {
            foreach (Delegate d in UpdateNotification.GetInvocationList())
            {
                if (d.Equals(action))
                    return;
            }
        }
        UpdateNotification += action;
    }

    public void UnsubscribeDeleteRequest(Action<PUMPSaveDataStructure> action)
    {
        DeleteRequest -= action;
    }

    public void UnsubscribeUpdateNotification(Action<PUMPSaveDataStructure> action)
    {
        UpdateNotification -= action;
    }

    public void Paste(PUMPSaveDataStructure structure)
    {
        NodeInfos = structure.NodeInfos;
        Name = structure.Name;
        ImagePath = structure.ImagePath;
        Tag = structure.Tag;
    }

    public void Delete() => DeleteRequest?.Invoke(this);
    public void NotifyDataChanged() => UpdateNotification?.Invoke(this);
}

public class PairEvent
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

    public void RemoveEvent(string key, Action action)
    {
        if (Events.ContainsKey(key))
        {
            Events[key] -= action;
        }
    }
}