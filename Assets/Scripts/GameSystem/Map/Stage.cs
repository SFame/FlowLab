using OdinSerializer;
using System;
using UnityEngine;

[Serializable]
public class StageData
{
    [OdinSerialize][SerializeField] private string _stageID;
    [OdinSerialize][SerializeField] private float _clearTime;
    [OdinSerialize][SerializeField] private bool _clear;
    public StageData(string puzzleName, bool claer, float clearTime)
    {
        _stageID = puzzleName;
        _clearTime = clearTime;
        _clear = claer;
    }

    public bool Clear
    {
        get
        {
            return _clear;
        }
        set
        {
            if (!_clear)
            {
                _clear = value;
            }
        }
    }
    public string StageID
    {
        get { return _stageID; }
    }
}
