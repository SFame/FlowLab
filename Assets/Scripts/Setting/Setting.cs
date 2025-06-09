using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Serializer = Utils.Serializer;

public static class Setting
{
    #region default values
    //default Settings
    public static float DefaultSoundVolume = 1.0f; // Default sound volume
    public static float DefaultVFXVolume = 1.0f; // Default VFX volume
    public static float DefaultSimulationSpeed = 0.0f; // Default simulation speed
    // Default key map settings
    public static List<BackgroundActionKeyMap> DefaultKeyMap => new List<BackgroundActionKeyMap>
    {
#if UNITY_EDITOR
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Undo,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl },
            m_ActionKeys = new List<KeyCode> { KeyCode.LeftArrow }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Redo,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl },
            m_ActionKeys = new List<KeyCode> { KeyCode.RightArrow }
        },
#else
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Undo,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl },
            m_ActionKeys = new List<KeyCode> { KeyCode.Z }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Redo,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl, KeyCode.LeftShift },
            m_ActionKeys = new List<KeyCode> { KeyCode.Z }
        },
#endif
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectAll,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl },
            m_ActionKeys = new List<KeyCode> { KeyCode.A }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDelete,
            m_Modifiers = new List<KeyCode> {},
            m_ActionKeys = new List<KeyCode> { KeyCode.Delete }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDisconnect,
            m_Modifiers = new List<KeyCode> {},
            m_ActionKeys = new List<KeyCode> { KeyCode.Backspace }
        }
    };

#endregion
    #region Properties
    // 현재 설정값 (UI에서 수정하는 임시값)
    private static float _tempVfxVolume = DefaultVFXVolume;
    private static float _tempSimulationSpeed = DefaultSimulationSpeed;
    private static List<BackgroundActionKeyMap> _tempKeyMap = new List<BackgroundActionKeyMap>(DefaultKeyMap);

    // 실제 적용된 설정값
    private static SettingData _currentSettings = new SettingData();

    // 저장 파일명
    private const string SAVE_FILE_NAME = "Settings.json";

    // 공개 프로퍼티
    public static float VFXVolume => _currentSettings.vfxVolume;
    public static float SimulationSpeed => _currentSettings.simulationSpeed;
    public static List<BackgroundActionKeyMap> CurrentKeyMap => new List<BackgroundActionKeyMap>(_currentSettings.keyMapList);
    #endregion

    #region Events
    // 설정 변경 이벤트
    public static event Action OnSettingUpdated;
    #endregion

    #region Initialization
    static Setting()
    {
        // 저장된 설정 로드
        LoadSettings();

        OnSettingUpdated?.Invoke();
        AudioMixer audioMixer = Resources.Load<AudioMixer>("AudioMixer");
        if (audioMixer != null)
        {
            SetAudio(audioMixer);
            // VFX 볼륨을 오디오 믹서에 적용
            //float dB = ConvertToDecibel(_currentSettings.vfxVolume);
            //audioMixer.SetFloat("VFX", dB);
        }
        else
        {
            Debug.LogWarning("AudioMixer not found. VFX volume will not be applied.");
        }
    }
    private static async UniTaskVoid SetAudio(AudioMixer audioMixer)
    {
        await UniTask.Yield();
        float dB = ConvertToDecibel(_currentSettings.vfxVolume);
        audioMixer.SetFloat("VFX", dB);
    }
    #endregion

    #region Temporary Settings (UI에서 사용)

    public static void SetTempVFXVolume(float volume)
    {
        _tempVfxVolume = Mathf.Clamp01(volume);
    }

    public static void SetTempSimulationSpeed(float speed)
    {
        _tempSimulationSpeed = Mathf.Clamp(speed, 0f, 2.0f);
    }
    public static void SetTempKeyMap(List<BackgroundActionKeyMap> keyMap) //apply 버튼을 누를시 UI의 목록을 가져와 다듬어 넣기..
    {
        _tempKeyMap = new List<BackgroundActionKeyMap>(keyMap);
    }
    #endregion
    public static void ResetTempToDefault()
    {
        _tempVfxVolume = DefaultVFXVolume;
        _tempSimulationSpeed = DefaultSimulationSpeed;
        _tempKeyMap = new List<BackgroundActionKeyMap>(DefaultKeyMap);
    }
    public static void OnClickApplyButton()
    {
        // 임시 설정값을 현재 설정으로 복사
        _currentSettings.vfxVolume = _tempVfxVolume;
        _currentSettings.simulationSpeed = _tempSimulationSpeed;
        _currentSettings.keyMapList = new List<BackgroundActionKeyMap>(_tempKeyMap);

        // 설정 저장
        SaveSettings();

        // 이벤트 발생 - 각 시스템이 이를 받아서 Setting의 값을 가져가 적용
        OnSettingUpdated?.Invoke();
    }
  
    #region Save/Load Settings
    [Serializable]
    public class SettingData
    {
        [OdinSerialize] public float vfxVolume;
        [OdinSerialize] public float simulationSpeed;
        [OdinSerialize] public List<BackgroundActionKeyMap> keyMapList;

        public SettingData()
        {
            vfxVolume = DefaultVFXVolume;
            simulationSpeed = DefaultSimulationSpeed;
            keyMapList = new List<BackgroundActionKeyMap>(DefaultKeyMap);
        }

        public SettingData( float vfx, float speed, List<BackgroundActionKeyMap> keyMap)
        {
            vfxVolume = vfx;
            simulationSpeed = speed;
            keyMapList = new List<BackgroundActionKeyMap>(keyMap);
        }
    }

    private static void SaveSettings()
    {
        try
        {
            Serializer.SaveData(SAVE_FILE_NAME, _currentSettings, format: DataFormat.Binary);
            Debug.Log("설정이 저장되었습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"설정 저장 실패: {e.Message}");
        }
    }

    private static void LoadSettings()
    {
        try
        {
            SettingData loadedData = Serializer.LoadData<SettingData>(SAVE_FILE_NAME, format: DataFormat.Binary);

            if (loadedData != null)
            {
                _currentSettings = loadedData;

                // 로드된 값으로 임시 설정값도 초기화
                _tempVfxVolume = _currentSettings.vfxVolume;
                _tempSimulationSpeed = _currentSettings.simulationSpeed;
                _tempKeyMap = new List<BackgroundActionKeyMap>(_currentSettings.keyMapList);

                Debug.Log("설정이 로드되었습니다.");
            }
            else
            {
                // 로드 실패시 기본값 사용
                _currentSettings = new SettingData();
                ResetTempToDefault();
                Debug.Log("저장된 설정이 없습니다. 기본값을 사용하고 저장하겠습니다.");
                SaveSettings(); // 기본값 저장
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"설정 로드 실패: {e.Message}");
            _currentSettings = new SettingData();
            ResetTempToDefault();
        }
    }
    #endregion


    // 현재 임시 설정값 가져오기 (UI 표시용)
    public static (float vfx, float speed, List<BackgroundActionKeyMap> keyMap) GetTempSettings()
    {
        return (_tempVfxVolume, _tempSimulationSpeed, new List<BackgroundActionKeyMap>(_tempKeyMap));
    }
    // 현재 설정값 가져오기 (시스템 적용용)
    public static (float vfx, float speed, List<BackgroundActionKeyMap> keyMap) GetCurrentSettings()
    {
        return (_currentSettings.vfxVolume, _currentSettings.simulationSpeed, new List<BackgroundActionKeyMap>(_currentSettings.keyMapList));
    }

    // ActionType에 해당하는 디폴트 키맵 가져오기
    public static BackgroundActionType GetActionType(BackgroundActionType actionType)
    {
        return DefaultKeyMap.Find(k => k.m_ActionType == actionType)?.m_ActionType ?? actionType;
    }
    public static List<KeyCode> GetActionKeys(BackgroundActionType actionType)
    {
        return DefaultKeyMap.Find(k => k.m_ActionType == actionType)?.m_ActionKeys ?? new List<KeyCode>();
    }
    public static List<KeyCode> GetActionModifiers(BackgroundActionType actionType)
    {
        return DefaultKeyMap.Find(k => k.m_ActionType == actionType)?.m_Modifiers ?? new List<KeyCode>();
    }

    private static float ConvertToDecibel(float volume)
    {
        // 0 = 음소거, 1 = 0dB (최대 볼륨)
        return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
    }
}