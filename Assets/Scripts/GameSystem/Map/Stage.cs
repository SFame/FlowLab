using OdinSerializer;
using System;
using UnityEngine;

[Serializable]
public class Stage : MonoBehaviour
{
    [OdinSerialize][SerializeField] private string _stageID;
    [OdinSerialize][SerializeField] private bool _clear;

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
