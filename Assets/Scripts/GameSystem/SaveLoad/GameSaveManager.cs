using System;
using System.Collections.Generic;
using OdinSerializer;
using UnityEngine;
using static Utils.Serializer;

public class GameSaveManager
{
    #region singleton
    public static GameSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameSaveManager();
            }
            return _instance;
        }
    }
    private static GameSaveManager _instance;
    #endregion

    private const string FILE_NAME = "SaveData.bin";
    [OdinSerialize] private GameSaveData Data 
    {
        get
        {
            LoadGame();
            return _data;
        }
    }
    private GameSaveData _data;


    private GameSaveManager()
    {
        GlobalEventManager.GameStartEvent += LoadGame;
        GlobalEventManager.GameExitEvent += SaveGame;
        GlobalEventManager.StageClearEvent += SetPuzzleState;
        GlobalEventManager.StageClearEventForNode += SetUnlockNodeList;
    }

    private bool _onGameLoaded = false;
    public void SaveGame()
    {
        _data ??= new GameSaveData();

        SaveData(FILE_NAME, _data);
    }

    public void LoadGame()
    {
        if (_onGameLoaded)
            return;
        _onGameLoaded = true;
        _data = LoadData<GameSaveData>(FILE_NAME);
        if(Data == null)
            _data = new GameSaveData();
        else
            PlayerNodeInventory.LoadUnlockedNodes();
    }
    public void SetPuzzleState(StageData stageData)
    {
        Data.StageDictionary[stageData.StageID] = stageData;
    }
    public void SetUnlockNodeList()
    {
        Data.UnlockedNodeList = PlayerNodeInventory.GetUnlockedNodeList();
    }
    public List<Type> GetUnlockNodeList()
    {
        return Data.UnlockedNodeList;
    }
    public StageData FindPuzzleDataState(string puzzleID)
    {
        if(Data.StageDictionary.Count == 0)
            return null;
        if (!Data.StageDictionary.ContainsKey(puzzleID))
        {
            return null;
        }

        return Data.StageDictionary[puzzleID];
    }
}