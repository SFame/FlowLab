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
        _data = new GameSaveData();
        // 플레이어 현재 위치 및 회전 업데이트
        UpdatePlayerTransform();
        UpdateRoomState();
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
    private void UpdateRoomState()
    {
        for (int i = 0; i < _roomList.Count; i++)
        {
            _data.RoomDataList.Add(_roomList[i].GetRoomData());
        }
    }
    private void ApplyRoomState()
    {
        for (int i = 0; i < _data.RoomDataList.Count; i++)
        {
            _roomList[i].SetRoomData(_data.RoomDataList[i]);
        }
    }
}