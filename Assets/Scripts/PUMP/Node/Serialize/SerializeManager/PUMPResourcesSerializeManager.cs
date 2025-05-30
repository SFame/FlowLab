using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Utils.ResourceSerializer;

public class PUMPResourcesSerializeManager : ISerializeManagable<PUMPSaveDataStructure>
{
    #region Privates
    private static PUMPResourcesSerializeManager _instance;
    private readonly Dictionary<string, List<PUMPSaveDataStructure>> _saveDatas = new();
    private readonly PairEvent _onDataUpdated = new();
    private IPairEventInvokable _invokable;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private const string FOLDER_NAME = "SerializeData";

    #region Singleton
    private PUMPResourcesSerializeManager() { }
    static PUMPResourcesSerializeManager() => _instance = new PUMPResourcesSerializeManager();
    public static PUMPResourcesSerializeManager Instance => _instance;
    #endregion

    private async UniTask GetDataInDictionaryFromFile(string fileName)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_saveDatas.ContainsKey(fileName) && _saveDatas[fileName] != null)
                return;

            List<PUMPSaveDataStructure> datas = await LoadFromResourcesAsync<List<PUMPSaveDataStructure>>(fileName, FOLDER_NAME);
            datas ??= new();

            foreach (PUMPSaveDataStructure data in datas)
            {
                data.SubscribeDeleteRequest(saveStructure => DeleteData(fileName, saveStructure));
                data.SubscribeUpdateNotification(saveStructure => saveStructure.LastUpdate = DateTime.Now);
                data.SubscribeDeleteRequest(_ => InvokeDataUpdated(fileName));
                data.SubscribeUpdateNotification(_ => InvokeDataUpdated(fileName));
            }

            _onDataUpdated.AddEvent(fileName, () => WriteData(fileName), GetType().Name);
            _saveDatas.Add(fileName, datas);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void WriteData(string fileName)
    {
        _ = SaveToResourcesAsync(fileName, _saveDatas[fileName], FOLDER_NAME);
    }
    private void DeleteData(string fileName, PUMPSaveDataStructure data)
    {
        _saveDatas[fileName].Remove(data);
    }

    private void InvokeDataUpdated(string fileName)
    {
        _invokable ??= _onDataUpdated;
        _invokable.Invoke(fileName);
    }
    #endregion

    #region Interface
    public PairEvent OnDataUpdated => _onDataUpdated;

    public async Task AddData(string fileName, PUMPSaveDataStructure serializeObject)
    {
        await GetDataInDictionaryFromFile(fileName);
        serializeObject.SubscribeDeleteRequest(saveStructure => DeleteData(fileName, saveStructure));
        serializeObject.SubscribeUpdateNotification(saveStructure => saveStructure.LastUpdate = DateTime.Now);
        serializeObject.SubscribeDeleteRequest(_ => InvokeDataUpdated(fileName));
        serializeObject.SubscribeUpdateNotification(_ => InvokeDataUpdated(fileName));
        serializeObject.LastUpdate = DateTime.Now;

        _saveDatas[fileName].Add(serializeObject);

        InvokeDataUpdated(fileName);
    }

    public async Task<List<PUMPSaveDataStructure>> GetDatas(string fileName)
    {
        await GetDataInDictionaryFromFile(fileName);
        return _saveDatas[fileName].ToList();
    }

    public Task AddData(string fileName, object serializeObject)
    {
        if (serializeObject is PUMPSaveDataStructure structure)
        {
            return AddData(fileName, structure);
        }

        Debug.LogError("Cast fail serializeObject to PUMPSaveDataStructure");
        return Task.CompletedTask;
    }

    async Task<List<object>> ISerializeManagable.GetDatas(string fileName)
    {
        List<PUMPSaveDataStructure> structures = await GetDatas(fileName);
        List<object> objects = structures.Select(structure => (object)structure).ToList();
        return objects;
    }
    #endregion
}