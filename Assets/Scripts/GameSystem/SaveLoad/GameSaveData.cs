using OdinSerializer;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    [OdinSerialize][SerializeField] private Vector3 _playerPosition;
    [OdinSerialize][SerializeField] private Quaternion _playerRotation;
    [OdinSerialize][SerializeField] private List<RoomData> _roomDataList;
    [OdinSerialize][SerializeField] private DateTime _lastSaveTime;

    #region Properties
    public Vector3 PlayerPosition
    {
        get { return _playerPosition; }
        set { _playerPosition = value; }
    }
    public Quaternion PlayerRotation
    {
        get { return _playerRotation; }
        set { _playerRotation = value; }
    }
    public List<RoomData> RoomDataList
    {
        get { return _roomDataList; }
        set { _roomDataList = value; }
    }
    public DateTime LastSaveTime => _lastSaveTime;
    #endregion

    public GameSaveData()
    {
        _playerPosition = Vector3.zero;
        _playerRotation = Quaternion.identity;
        _lastSaveTime = DateTime.Now;
        _roomDataList = new List<RoomData>();
    }
    public void UpdateSaveTime()
    {
        _lastSaveTime = DateTime.Now;
    }
}
