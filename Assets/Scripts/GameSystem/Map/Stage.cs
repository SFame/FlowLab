using OdinSerializer;
using System;
using UnityEngine;

[Serializable]

public class ObjectData
{

}

[Serializable]
public class StageData : ObjectData
{
    [OdinSerialize][SerializeField] private string _stageID;
    [OdinSerialize][SerializeField] private bool _clear;
    public StageData(string puzzleName, bool claer)
    {
        _stageID = puzzleName;
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
