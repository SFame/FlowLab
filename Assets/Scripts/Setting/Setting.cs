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
    public static float DefaultSimulationSpeed = 1.0f; // Default simulation speed
    // Default key map settings
    public static List<BackgroundActionKeyMap> DefaultKeyMap => new List<BackgroundActionKeyMap>
    {
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
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectAll,
            m_Modifiers = new List<KeyCode> { KeyCode.LeftControl },
            m_ActionKeys = new List<KeyCode> { KeyCode.A }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDelete,
            m_ActionKeys = new List<KeyCode> { KeyCode.Delete }
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDisconnect,
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
    // AudioMixer 참조
    private static AudioMixer _audioMixer;

    // AudioMixer 파라미터 이름
    private const string MusicVolumeParam = "Music";
    private const string VFXVolumeParam = "VFX";
    // 저장 파일명
    private const string SAVE_FILE_NAME = "Game_Settings.json";

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
    public static void Initialize()
    {
        
        // 저장된 설정 로드
        LoadSettings();

        ApplyAllSettings();//sound

        OnSettingUpdated?.Invoke();
    }
    #endregion

    #region Temporary Settings (UI에서 사용)

    public static void SetTempVFXVolume(float volume)
    {
        _tempVfxVolume = Mathf.Clamp01(volume);
    }

    public static void SetTempSimulationSpeed(float speed)
    {
        _tempSimulationSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
    }
    public static void SetTempKeyMap(List<BackgroundActionKeyMap> keyMap)
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

        // 설정 적용
        ApplyAllSettings();

        // 설정 저장
        SaveSettings();

        // 이벤트 발생 - 각 시스템이 이를 받아서 Setting의 값을 가져가 적용
        OnSettingUpdated?.Invoke();
    }
    private static void ApplyAllSettings()
    {
        ApplyVFXVolume(_currentSettings.vfxVolume);
    }
    #region Sound Settings
    private static void ApplyVFXVolume(float volume)
    {

        float dB = ConvertToDecibel(volume);
        _audioMixer.SetFloat(VFXVolumeParam, dB);
    }
    
    private static float ConvertToDecibel(float volume)
    {
        // 0 = 음소거, 1 = 0dB (최대 볼륨)
        return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
    }

    #endregion

    #region Save/Load Settings
    [Serializable]
    public class SettingData
    {
        [OdinSerialize] public float soundVolume;
        [OdinSerialize] public float vfxVolume;
        [OdinSerialize] public float simulationSpeed;
        [OdinSerialize] public List<BackgroundActionKeyMap> keyMapList;

        public SettingData()
        {
            soundVolume = DefaultSoundVolume;
            vfxVolume = DefaultVFXVolume;
            simulationSpeed = DefaultSimulationSpeed;
            keyMapList = new List<BackgroundActionKeyMap>(DefaultKeyMap);
        }

        public SettingData(float sound, float vfx, float speed, List<BackgroundActionKeyMap> keyMap)
        {
            soundVolume = sound;
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
                Debug.Log("저장된 설정이 없습니다. 기본값을 사용합니다.");
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

}
