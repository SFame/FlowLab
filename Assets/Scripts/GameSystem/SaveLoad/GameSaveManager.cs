using System.Collections.Generic;
using OdinSerializer;
using UnityEngine;
using static Utils.Serializer;

public class GameSaveManager : MonoBehaviour
{
    #region singleton
    public static GameSaveManager Instance { get; private set; }
    #endregion

    private const string FILE_NAME = "SaveData.bin";
    [OdinSerialize][SerializeField] private GameSaveData _data;
    private void Awake()
    {
        // 싱글톤 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GlobalEventManager.GameStartEvent += LoadGame;
            GlobalEventManager.GameExitEvent += SaveGame;
            GlobalEventManager.StageClearEvent += SetPuzzleState;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SaveGame()
    {
        _data ??= new GameSaveData();

        SaveData(FILE_NAME, _data);
    }

    public void LoadGame()
    {
        _data = LoadData<GameSaveData>(FILE_NAME);
        if(_data == default)
            _data = new GameSaveData();
    }
    public void SetPuzzleState(StageData stageData)
    {
        _data.StageDictionary[stageData.StageID] = stageData;
    }
    public StageData FindPuzzleDataState(string puzzleID)
    {
        if(_data.StageDictionary.Count == 0)
            return null;
        if (!_data.StageDictionary.ContainsKey(puzzleID))
        {
            Debug.LogError("Error : Data not available");
            return null;
        }

        return _data.StageDictionary[puzzleID];
    }
}