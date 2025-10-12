using Cysharp.Threading.Tasks;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public static int DefaultLoopThreshold = 2; //min 2 max 20
    public static bool DefaultIsImmediately = false;
    // Default key map settings
    public static List<BackgroundActionKeyMap> DefaultKeyMap => new List<BackgroundActionKeyMap>
    {
#if UNITY_EDITOR
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Undo,
            m_KeyMap = new InputKeyMap (ActionKeyCode.Z, new HashSet<ModifierKeyCode> { ModifierKeyCode.LeftControl })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Redo,
            m_KeyMap = new InputKeyMap (ActionKeyCode.X, new HashSet<ModifierKeyCode> { ModifierKeyCode.LeftControl, ModifierKeyCode.LeftShift })
        },
#else
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Undo,
            m_KeyMap = new InputKeyMap (ActionKeyCode.Z, new HashSet<ModifierKeyCode> { ModifierKeyCode.LeftControl })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.Redo,
            m_KeyMap = new InputKeyMap (ActionKeyCode.X, new HashSet<ModifierKeyCode> { ModifierKeyCode.LeftControl, ModifierKeyCode.LeftShift })
        },
#endif
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.OpenPalette,
            m_KeyMap = new InputKeyMap (ActionKeyCode.Tab, new HashSet<ModifierKeyCode> { })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.OpenSaveLoadPanel,
            m_KeyMap = new InputKeyMap (ActionKeyCode.S, new HashSet<ModifierKeyCode> { })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectAll,
            m_KeyMap = new InputKeyMap (ActionKeyCode.A, new HashSet<ModifierKeyCode> { ModifierKeyCode.LeftControl })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDelete,
            m_KeyMap = new InputKeyMap (ActionKeyCode.Delete, new HashSet<ModifierKeyCode> { })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.SelectDisconnect,
            m_KeyMap = new InputKeyMap (ActionKeyCode.Backspace, new HashSet<ModifierKeyCode> { })
        },
        new BackgroundActionKeyMap
        {
            m_ActionType = BackgroundActionType.MinimapToggle,
            m_KeyMap = new InputKeyMap (ActionKeyCode.M, new HashSet<ModifierKeyCode> { })
        },
    };

#endregion
    #region Properties
    // 현재 설정값 (UI에서 수정하는 임시값)
    private static float _tempVfxVolume = DefaultVFXVolume;
    private static float _tempSimulationSpeed = DefaultSimulationSpeed;
    private static bool _tempIsImmediately = DefaultIsImmediately;
    private static int _tempLoopThreshold = DefaultLoopThreshold;
    private static List<BackgroundActionKeyMap> _tempKeyMap = new List<BackgroundActionKeyMap>(DefaultKeyMap);

    // 실제 적용된 설정값
    private static SettingData _currentSettings = new SettingData();

    // 저장 파일명
    private const string SAVE_FILE_NAME = "Settings.json";

    // 공개 프로퍼티
    public static float VFXVolume => _currentSettings.vfxVolume;
    public static float SimulationSpeed => _currentSettings.simulationSpeed;
    public static List<BackgroundActionKeyMap> CurrentKeyMap => new List<BackgroundActionKeyMap>(_currentSettings.keyMapList);

    public static bool IsImmediately => _currentSettings.isImmediately;
    public static int LoopThreshold => _currentSettings.loopThreshold;

    // ConnectionAwait 상태를 반환하는 프로퍼티
    public static ConnectionAwait CurrentConnectionAwait
    {
        get
        {
            if (_currentSettings.isImmediately)
                return ConnectionAwait.Immediately;
            else if (_currentSettings.simulationSpeed < 0.001f)
                return ConnectionAwait.Frame;
            else
                return ConnectionAwait.FixedTime;
        }
    }
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

        AudioMixer audioMixer = Resources.Load<AudioMixer>("AudioMixer");
        if (audioMixer != null)
        {
            SetAudio(audioMixer).Forget();
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
    public static void SetTempKeyMap(List<BackgroundActionKeyMap> keyMap)
    {
        _tempKeyMap = new List<BackgroundActionKeyMap>(keyMap);
    }
    public static void SetTempKeyMapSingle(BackgroundActionType actionType, InputKeyMap keyMap)
    {
        var index = _tempKeyMap.FindIndex(k => k.m_ActionType == actionType);
        if (index >= 0)
        {
            _tempKeyMap[index].m_KeyMap = keyMap;
        }
        else
        {
            _tempKeyMap.Add(new BackgroundActionKeyMap { m_ActionType = actionType, m_KeyMap = keyMap });
        }
    }
    public static void SetTempIsImmediately(bool isImmediately)
    {
        _tempIsImmediately = isImmediately;
    }
    public static void SetTempLoopThreshold(int loopThreshold)
    {
        _tempLoopThreshold = Mathf.Clamp(loopThreshold, 2, 20); // 최소값 1로 제한..
    }
    #endregion
    public static void ResetTempToDefault()
    {
        _tempVfxVolume = DefaultVFXVolume;
        _tempSimulationSpeed = DefaultSimulationSpeed;
        _tempKeyMap = new List<BackgroundActionKeyMap>(DefaultKeyMap);
        _tempLoopThreshold = DefaultLoopThreshold;
        _tempIsImmediately = DefaultIsImmediately;
    }
    public static void OnClickApplyButton()
    {
        // 임시 설정값을 현재 설정으로 복사
        _currentSettings.vfxVolume = _tempVfxVolume;
        _currentSettings.simulationSpeed = _tempSimulationSpeed;
        _currentSettings.isImmediately = _tempIsImmediately;
        _currentSettings.loopThreshold = _tempLoopThreshold;
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
        [OdinSerialize] public bool isImmediately;
        [OdinSerialize] public int loopThreshold;

        public SettingData()
        {
            vfxVolume = DefaultVFXVolume;
            simulationSpeed = DefaultSimulationSpeed;
            isImmediately = DefaultIsImmediately;
            loopThreshold = DefaultLoopThreshold;
            keyMapList = new List<BackgroundActionKeyMap>(DefaultKeyMap);
        }

        public SettingData(float vfx, float speed, bool immediately, int threshold, List<BackgroundActionKeyMap> keyMap)
        {
            vfxVolume = vfx;
            simulationSpeed = speed;
            isImmediately = immediately;
            loopThreshold = threshold;
            keyMapList = new List<BackgroundActionKeyMap>(keyMap);
        }
    }

    private static void SaveSettings()
    {
        try
        {
            Serializer.SaveData(SAVE_FILE_NAME, _currentSettings, format: DataFormat.Binary);
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
                _tempIsImmediately = _currentSettings.isImmediately;
                _tempLoopThreshold = _currentSettings.loopThreshold;
                _tempKeyMap = new List<BackgroundActionKeyMap>(_currentSettings.keyMapList);
            }
            else
            {
                // 로드 실패시 기본값 사용
                _currentSettings = new SettingData();
                ResetTempToDefault();
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
    public static (float vfx, float speed, bool immediately, int loopThreshold, List<BackgroundActionKeyMap> keyMap) GetTempSettings()
    {
        return (_tempVfxVolume, _tempSimulationSpeed, _tempIsImmediately, _tempLoopThreshold, new List<BackgroundActionKeyMap>(_tempKeyMap));
    }

    // 현재 설정값 가져오기 (시스템 적용용)
    public static (float vfx, float speed, bool immediately, int loopThreshold, List<BackgroundActionKeyMap> keyMap) GetCurrentSettings()
    {
        return (_currentSettings.vfxVolume, _currentSettings.simulationSpeed, _currentSettings.isImmediately, _currentSettings.loopThreshold, new List<BackgroundActionKeyMap>(_currentSettings.keyMapList));
    }

    // ActionType에 해당하는 디폴트 키맵 가져오기
    public static BackgroundActionType GetActionType(BackgroundActionType actionType)
    {
        return DefaultKeyMap.Find(k => k.m_ActionType == actionType)?.m_ActionType ?? actionType;
    }
    public static ActionKeyCode GetActionKey(BackgroundActionType actionType)
    {
        var keyMapInfo = _currentSettings.keyMapList.Find(k => k.m_ActionType == actionType);
        return keyMapInfo.m_KeyMap.ActionKey;
    }
    public static List<ModifierKeyCode> GetActionModifiers(BackgroundActionType actionType)
    {
        var keyMapInfo = _currentSettings.keyMapList.Find(k => k.m_ActionType == actionType);
        return keyMapInfo?.m_KeyMap.Modifiers.ToList() ?? new List<ModifierKeyCode>();
    }
    public static InputKeyMap GetKeyMap(BackgroundActionType actionType)
    {
        var keyMapInfo = _currentSettings.keyMapList.Find(k => k.m_ActionType == actionType);
        return keyMapInfo?.m_KeyMap ?? new InputKeyMap(ActionKeyCode.None);
    }

    private static float ConvertToDecibel(float volume)
    {
        // 0 = 음소거, 1 = 0dB (최대 볼륨)
        return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
    }

    public class SettingKeyManager
    {
        private List<BackgroundActionKeyMap> _keyMaps;
        private readonly Action<List<BackgroundActionKeyMap>> _applier;

        public SettingKeyManager(List<BackgroundActionKeyMap> keyMaps, Action<List<BackgroundActionKeyMap>> applier)
        {
            _keyMaps = keyMaps;
            _applier = applier;
        }

        public IReadOnlyList<BackgroundActionKeyMap> KeyMaps => _keyMaps;

        public List<BackgroundActionKeyMap> Apply(List<BackgroundActionKeyMap> newKeyMaps)
        {
            List<BackgroundActionType> updatedType = new();
            foreach (BackgroundActionKeyMap keyMap in newKeyMaps)
            {
                InputKeyMap lastKeyMap =
                    _keyMaps.FirstOrDefault(lk => lk.m_ActionType == keyMap.m_ActionType)?.m_KeyMap ??
                    DefaultKeyMap.FirstOrDefault(defaultKeymap => defaultKeymap.m_ActionType == keyMap.m_ActionType)!.m_KeyMap;
                InputKeyMap newKeyMap = keyMap.m_KeyMap;

                if (!lastKeyMap.Equals(newKeyMap))
                {
                    updatedType.Add(keyMap.m_ActionType);
                }
            }

            HashSet<BackgroundActionType> removeTypes = new();
            var keyMapDuplicateGroup = newKeyMaps.GroupBy(km => km.m_KeyMap);
            foreach (IGrouping<InputKeyMap, BackgroundActionKeyMap> group in keyMapDuplicateGroup)
            {
                if (group.Count() == 1)
                {
                    continue;
                }

                if (group.Count() == 2)
                {
                    List<BackgroundActionType> nonContainRemoveTarget = new();
                    foreach (BackgroundActionKeyMap elem in group)
                    {
                        if (updatedType.Contains(elem.m_ActionType))
                        {
                            removeTypes.Add(elem.m_ActionType);
                            break;
                        }
                        nonContainRemoveTarget.Add(elem.m_ActionType);
                    }

                    if (nonContainRemoveTarget.Count >= 2)
                    {
                        removeTypes.AddRange(nonContainRemoveTarget);
                    }
                    continue;
                }

                foreach (BackgroundActionKeyMap elem in group)
                {
                    removeTypes.Add(elem.m_ActionType);
                }
            }

            foreach (BackgroundActionKeyMap newKeyMap in newKeyMaps)
            {
                if (InputManager.TryFind(newKeyMap.m_KeyMap, out InputKeyMapArgs findArgs) && findArgs.Immutable)
                {
                    removeTypes.Add(newKeyMap.m_ActionType);
                }
            }

            foreach (BackgroundActionType removeType in removeTypes)
            {
                BackgroundActionKeyMap target = newKeyMaps.FirstOrDefault(km => km.m_ActionType == removeType);
                if (target != null)
                {
                    target.m_KeyMap = new InputKeyMap(ActionKeyCode.None, new HashSet<ModifierKeyCode>());
                }
            }

            List<BackgroundActionKeyMap> newKeyMapList = newKeyMaps.ToList();

            _keyMaps = newKeyMapList;
            _applier?.Invoke(newKeyMapList);
            return _keyMaps;
        }
    }
}