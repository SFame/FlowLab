using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using OdinSerializer;
using System.Text;
using System;

[Serializable]
public class Room : MonoBehaviour
{
    [OdinSerialize][SerializeField] private string _roomID;
    [OdinSerialize][SerializeField] private List<Stage> _stageList;
    [OdinSerialize][SerializeField] private bool _clear;
    [OdinSerialize][SerializeField] private object _tag;

    #region Properties
    public string RoomID
    {
        get { return _roomID; }
        set { _roomID = value; }
    }
    public List<Stage> StageList
    {
        get { return _stageList; }
        set { _stageList = value; }
    }
    public bool Clear
    {
        get { return _clear; }
        set { _clear = value; }
    }
    public object Tag
    {
        get { return _tag; }
        set { _tag = value; }
    }
    #endregion
    public Room()
    {
        _roomID = string.Empty;
        _stageList = new List<Stage>();
        _clear = false;
        _tag = null;
    }
    public void UpdateClearStatus()
    {
        if (_stageList.Count == 0)
        {
            _clear = false;
            return;
        }

        foreach (var state in _stageList)
        {
            if (!state)
            {
                _clear = false;
                return;
            }
        }

        _clear = true;
    }
    public float GetRoomCompletionRate()
    {
        if (_stageList.Count == 0)
            return 0f;

        int clearedStages = 0;

        foreach (var state in _stageList)
        {
            if (state)
                clearedStages++;
        }

        return (float)clearedStages / _stageList.Count;
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SerializeRoomInfo]");
        sb.AppendLine("{");
        sb.AppendLine($"    [NodeType]: {_roomID ?? "null"}");
        sb.AppendLine($"    [StageCount]: {_stageList.Count}");
        sb.AppendLine($"    [StageStates]");
        sb.AppendLine("    {");
        foreach (var state in _stageList)
        {
            sb.AppendLine($"        [Stage ID]: {state.StageID}");
            sb.AppendLine($"        [Stage Clear]: {state.Clear}");
        }
        sb.AppendLine("    }");
        sb.Append("}");

        return sb.ToString();
    }
}
