using OdinSerializer;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

[Serializable]
public class GameSaveData
{
    [OdinSerialize][SerializeField] private List<string> _stageKeyList; 
    [OdinSerialize][SerializeField] private List<StageData> _stageDataList;
    [OdinSerialize][SerializeField] private List<object> _objectDataList;
    [SerializeField] private Dictionary<string, StageData> _stageDictionary;

    [OdinSerialize][SerializeField] private List<Type> _unlockedNodeList;

    #region Properties
    public List<string> StageKeyList
    {
        get { return _stageKeyList; }
        set { _stageKeyList = value; }
    }
    public List<StageData> StageDataList
    {
        get { return _stageDataList; }
        set { _stageDataList = value; }
    }
    public Dictionary<string, StageData> StageDictionary
    {
        get { return _stageDictionary; }
        set { _stageDictionary = value; }
    }
    public List<Type> UnlockedNodeList
    {
        get { return _unlockedNodeList; }
        set { _unlockedNodeList = value; }
    }
    #endregion

    public GameSaveData()
    {
        _stageKeyList = new List<string>();
        _stageDataList = new List<StageData>();
        _stageDictionary = new Dictionary<string, StageData>();
        _unlockedNodeList = new List<Type>();
        _objectDataList = new List<object>();
    }

    public void SerializeDictionary()
    {
        _stageKeyList = _stageDictionary.Keys.ToList();
        _stageDataList = _stageDictionary.Values.ToList();
    }
    public void DeserializeSaveData()
    {
        _stageDictionary ??= new Dictionary<string, StageData>();
        _stageDictionary.Clear();

        for (int i = 0; i < _stageKeyList.Count; i++)
        {
            if (_stageDictionary.ContainsKey(_stageKeyList[i]))
            {
                _stageDictionary[_stageKeyList[i]] = _stageDataList[i];
            }
        }
    }
}
