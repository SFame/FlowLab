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
    #endregion

    private const string FILE_NAME = "SaveData.bin";
    [OdinSerialize][SerializeField] private GameSaveData _data;
    [SerializeField] private List<Room> _roomList = new List<Room>();
    private void Awake()
    {
        // 싱글톤 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GlobalEventManager.GameStartEvent += LoadGame;
            GlobalEventManager.GameExitEvent += SaveGame;
        }
        else
        {
            Destroy(gameObject);
        }
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

        if(_data != default)
        {
            ApplyPlayerTransform();
            ApplyRoomState();
        }
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
    private void ApplyRoomState()
    {
        for (int i = 0; i < _roomList.Count; i++)
        {
            string roomName = _roomList[i].RoomID;
            _roomList[i] = _data.RoomList[roomName];
        }
    }
    public void UpdateRoomData(Room roomData)
    {
        for (int i = 0; i < _data.RoomList.Count; i++)
        {
            if (_data.RoomList.ContainsKey(roomData.RoomID))
            {
                _data.RoomList[roomData.RoomID] = roomData;
                return;
            }
        }

        // 존재하지 않으면 추가
        _data.RoomList[roomData.RoomID] = roomData;
    }
    public Room GetRoomData(string roomID)
    {
        if (_data.RoomList.ContainsKey(roomID))
        {
            return _data.RoomList[roomID];
        }
        return null;
    }
    public void SetStageCleared(string roomID, int stageID, bool cleared)
    {
        Room roomData = GetRoomData(roomID);
        roomData.UpdateClearStatus();
        UpdateRoomData(roomData);
    }
    public float GetRoomCompletionRate(string roomID)
    {
        return GetRoomData(roomID).GetRoomCompletionRate();
    }
}

[Serializable]
public class GameSaveData
{
    [OdinSerialize][SerializeField] private Vector3 _playerPosition;
    [OdinSerialize][SerializeField] private Quaternion _playerRotation;
    [OdinSerialize][SerializeField] private Dictionary<string, Room> _roomList;
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
    public Dictionary<string, Room> RoomList
    {
        get{ return RoomList; }
        set {  RoomList = value; }
    }
    public DateTime LastSaveTime => _lastSaveTime;
    #endregion

    public GameSaveData()
    {
        _playerPosition = Vector3.zero;
        _playerRotation = Quaternion.identity;
        _lastSaveTime = DateTime.Now;
        _roomList = new Dictionary<string, Room>();
    }
    public void UpdateSaveTime()
    {
        _lastSaveTime = DateTime.Now;
    }
}
