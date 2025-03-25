using System;
using System.Collections.Generic;
using System.Text;
using OdinSerializer;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static Utils.Serializer;
using System.IO;
using UnityEditor.Overlays;

public class GameSaveManager : SerializedMonoBehaviour
{
    #region singleton
    public static GameSaveManager Instance { get; private set; }
    private static PUMPSerializeManager _instance;
    #endregion

    private const string FILE_NAME = "node_data.bin";
    [OdinSerialize] private GameSaveData _data;

    private void Awake()
    {
        // 싱글톤 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private async UniTask InitializeSaveSystem()
    {
        _data = await LoadDataAsync<GameSaveData>(FILE_NAME);
        _data ??= new();
    }
    public void SaveGame()
    {
        // 플레이어 현재 위치 및 회전 업데이트
        UpdatePlayerTransform();

        _data.UpdateSaveTime();

        SaveData(FILE_NAME, _data);
    }
    public void LoadGame()
    {
        _data = LoadData<GameSaveData>(FILE_NAME);
        ApplyPlayerTransform();
    }
    private void UpdatePlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _data.PlayerPosition = player.transform.position;
            _data.PlayerRotation = player.transform.rotation;
        }
    }
    private void ApplyPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = _data.PlayerPosition;
            player.transform.rotation = _data.PlayerRotation;
        }
    }
    public void UpdateRoomData(RoomData roomData)
    {
        for (int i = 0; i < _data.RoomList.Count; i++)
        {
            if (_data.RoomList[i].RoomID == roomData.RoomID)
            {
                _data.RoomList[i] = roomData;
                return;
            }
        }

        // 존재하지 않으면 추가
        _data.RoomList.Add(roomData);
    }
    public RoomData GetRoomData(string roomID)
    {
        foreach (var roomData in _data.RoomList)
        {
            if (roomData.RoomID == roomID)
            {
                return roomData;
            }
        }

        // 존재하지 않으면 새로 생성
        return new RoomData(roomID);
    }
    public void SetStageCleared(string roomID, int stageID, bool cleared)
    {
        RoomData roomData = GetRoomData(roomID);
        roomData.UpdateClearStatus();
        UpdateRoomData(roomData);
    }
    public float GetRoomCompletionRate(string roomID)
    {
        return GetRoomData(roomID).GetRoomCompletionRate();
    }
}

[Serializable]
public class GameSaveData : MonoBehaviour
{
    [OdinSerialize] private Vector3 _playerPosition;
    [OdinSerialize] private Quaternion _playerRotation;
    [OdinSerialize] private List<RoomData> _roomList;
    [OdinSerialize] private DateTime _lastSaveTime;

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
    public List<RoomData> RoomList => _roomList;
    public DateTime LastSaveTime => _lastSaveTime;
    #endregion

    public GameSaveData()
    {
        _playerPosition = Vector3.zero;
        _playerRotation = Quaternion.identity;
        _lastSaveTime = DateTime.Now;
        _roomList ??= new();
    }
    public void UpdateSaveTime()
    {
        _lastSaveTime = DateTime.Now;
    }
}

[Serializable]
public struct RoomData
{
    [OdinSerialize] private string _roomID;
    [OdinSerialize] private Dictionary<int, bool> _stageStates;
    [OdinSerialize] private bool _clear;
	[OdinSerialize] private object _tag;

    #region Properties
    public string RoomID 
    {  
        get { return _roomID; } 
        set { _roomID = value; }
    }
    public Dictionary<int, bool> StageState 
    { 
        get { return _stageStates; } 
        set { _stageStates = value; } 
    }
    public bool Clear 
    {
        get {  return _clear; } 
        set { _clear = value; }
    }
	public object Tag
	{
		get { return _tag; }	
		set { _tag = value; }
	}
    #endregion

    public RoomData(string roomID, int stageCount = 0)
    {
        _tag = null;
        _roomID = roomID;
        _stageStates = new Dictionary<int, bool>();
        for (int i = 0; i< stageCount; i++)
        {
            _stageStates[i] = false;
        }
        _clear = false;
    }
    public void UpdateClearStatus()
    {
        if (_stageStates.Count == 0)
        {
            _clear = false;
            return;
        }

        foreach (var state in _stageStates.Values)
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
        if (_stageStates.Count == 0)
            return 0f;

        int clearedStages = 0;

        foreach (var state in _stageStates.Values)
        {
            if (state)
                clearedStages++;
        }

        return (float)clearedStages / _stageStates.Count;
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SerializeRoomInfo]");
        sb.AppendLine("{");
        sb.AppendLine($"    [NodeType]: {_roomID ?? "null"}");
        sb.AppendLine($"    [StageCount]: {_stageStates.Count}");
        sb.AppendLine($"    [StageState]");
        sb.AppendLine( "    {");
        foreach( var state in _stageStates)
        {
        sb.AppendLine($"        [Stage ID]: {state.Key}");
        sb.AppendLine($"        [Stage State]: {state.Value}");
        }
        sb.AppendLine( "    }");
        sb.Append("}");

        return sb.ToString();
    }
}