using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Serialize Manager 여러개를 관리 DataDirectory에 따라 사용자 AppData path에 저장할지, 리소스에 포함할 지 선택 가능
/// </summary>
/// <![CDATA[
/// private async Task PushAsync(string name)
/// {
///     PUMPSaveDataStructure newStructure = new()
///     {
///         Name = name,
///         NodeInfos = GetNodeInfos(),
///         ImagePath = GetImagePath(),
///         Tag = GetTag(),
///     };
///
///     if (ValidateBeforeSerialization(newStructure))
///     {
///         await SerializeManagerCatalog.AddData(DataDirectory.PumpAppData, savePath, newStructure);
///     }
/// }
///
/// private async Task AddNewAsync(IClassedNode classedNode)
/// {
///     List<PUMPSaveDataStructure> pumpData = await SerializeManagerCatalog.GetDatas<PUMPSaveDataStructure>(DataDirectory.PumpAppData, savePath);
///     PUMPSaveDataStructure matchedStructure = string.IsNullOrEmpty(classedNode.Id) ?
///         null : pumpData.FirstOrDefault(structure => structure.Tag.Equals(classedNode.Id));
///
///     PUMPBackground newBackground = BackgroundGetter?.Invoke();
///     if (matchedStructure != null)
///     {
///         newBackground.SetSerializeNodeInfos(matchedStructure.NodeInfos, true);
///         classedNode.Name = matchedStructure.Name;
///     }
///
///     LinkClassedToExternal(classedNode, newBackground.ExternalInput, newBackground.ExternalOutput);
///
///     newBackground.ExternalInput.OnCountUpdate += _ => OnExternalCountUpdateHandler(classedNode, newBackground);
///     newBackground.ExternalOutput.OnCountUpdate += _ => OnExternalCountUpdateHandler(classedNode, newBackground);
///
///     ClassedDict.Add(classedNode, newBackground);
/// }
///
/// private async UniTask Initialize()
/// {
///     if (_initialized)
///         return;
///
///     await GetDatasFromManager();
///     ScrollRect.Initialize(this);
///
///     saveButton?.onClick.AddListener(() =>
///     {
///         TextGetterManager.Set(RootCanvas, newName => AddNewSave(newName).Forget(), "Save name", defaultSaveName);
///     });
///
///     SerializeManagerCatalog.GetOnDataUpdatedEvent(DataDirectory.PumpAppData).AddEvent(savePath, ReloadData);
///     _initialized = true;
/// }
///]]>
public static class SerializeManagerCatalog
{
    #region Interface
    /// <summary>
    /// 데이터 저장
    /// </summary>
    /// <typeparam name="T">Data Type</typeparam>
    /// <param name="dataDirectory">Data Directory</param>
    /// <param name="fileName">File Name</param>
    /// <param name="serializeObject">Serialize Data</param>
    /// <returns></returns>
    public static Task AddData<T>(DataDirectory dataDirectory, string fileName, T serializeObject)
    {
        ISerializeManagable currentManager = GetSerializeManager(dataDirectory);
        if (currentManager == null)
        {
            Debug.Log("DataDirectory is invalid");
            return Task.CompletedTask;
        }

        return currentManager.AddData(fileName, serializeObject);
    }

    /// <summary>
    /// 데이터 불러오기 (파라미터 오류 발생 시 null 반환)
    /// </summary>
    /// <typeparam name="T">Return Type</typeparam>
    /// <param name="dataDirectory">Data Directory</param>
    /// <param name="fileName">File Name</param>
    /// <returns></returns>
    public static async Task<List<T>> GetDatas<T>(DataDirectory dataDirectory, string fileName)
    {
        ISerializeManagable currentManager = GetSerializeManager(dataDirectory);
        if (currentManager == null)
        {
            Debug.Log("DataDirectory is invalid");
            return null;
        }

        List<object> objects = await currentManager.GetDatas(fileName);

        if (objects == null)
        {
            Debug.LogError("직렬화 매니저가 좆됐습니다. 제작자에게 문의하세요.");
            return null;
        }

        bool isCastFail = false;
        List<T> results = objects.Select(obj =>
        {
            if (obj is T t)
            {
                return t;
            }

            isCastFail = true;
            return default;
        }).ToList();

        if (isCastFail)
        {
            Debug.LogError($"Type casting error: Failed to convert object to {typeof(T).Name}");
            return null;
        }

        return results;
    }

    /// <summary>
    /// 데이터 변경 이벤트 핸들 등록
    /// </summary>
    /// <param name="dataDirectory">Data Directory</param>
    /// <returns></returns>
    public static PairEvent GetOnDataUpdatedEvent(DataDirectory dataDirectory)
    {
        ISerializeManagable currentManager = GetSerializeManager(dataDirectory);
        if (currentManager == null)
        {
            Debug.Log("DataDirectory is invalid");
            return null;
        }

        return currentManager.OnDataUpdated;
    }
    #endregion

    #region Privates
    private static ISerializeManagable GetSerializeManager(DataDirectory directory) => directory switch
    {
        DataDirectory.PumpAppData => PUMPAppdataSerializeManager.Instance,
        DataDirectory.PumpResources => PUMPResourcesSerializeManager.Instance,
        _ => null
    };
    #endregion
}

public enum DataDirectory
{
    PumpAppData,
    PumpResources
}