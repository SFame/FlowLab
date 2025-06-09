using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UI_Settings : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider vfxSlider;
    [SerializeField] private TextMeshProUGUI vfxValueText;

    [Header("Simulation Delay Settings")]
    [SerializeField] private Slider simulationSpeedSlider;
    [SerializeField] private TextMeshProUGUI simulationSpeedText;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetToDefaultButton;
    public Button closeButton;

    [Header("KeyMap Settings")]
    [SerializeField] private Transform keyMapContent;
    [SerializeField] private GameObject keyMapItemPrefab;

    private readonly object blocker = new object();


    private void Start()
    {
        InitializeUI();
        SetupEventHandlers();

        ApplyVFXVolume(Setting.VFXVolume);
    }
    private void InitializeUI()
    {
        if (vfxSlider != null)
        {
            vfxSlider.minValue = 0f;
            vfxSlider.maxValue = 1f;
        }

        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.minValue = 0f;
            simulationSpeedSlider.maxValue = 2.0f;
        }

        // 초기 UI 값 설정
        RefreshUIFromCurrentSettings();
    }
    private void SetupEventHandlers()
    {
        if (vfxSlider != null)
        {
            vfxSlider.onValueChanged.AddListener(OnVFXVolumeChanged);
        }

        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.onValueChanged.AddListener(OnSimulationSpeedChanged);
        }

        // 버튼 이벤트
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyButtonClicked);
        }

        if (resetToDefaultButton != null)
        {
            resetToDefaultButton.onClick.AddListener(OnResetToDefaultClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // Setting 이벤트 구독
        Setting.OnSettingUpdated += OnSettingUpdated;
    }
    private void RefreshUIFromTempSettings()
    {
        var (vfx, speed, keyMap) = Setting.GetTempSettings();

        if (vfxSlider != null)
        {
            vfxSlider.SetValueWithoutNotify(vfx);
        }

        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.SetValueWithoutNotify(speed);
        }
        

        // 텍스트 업데이트
        UpdateVFXVolumeText(vfx);
        UpdateSimulationSpeedText(speed);
        // 키맵 UI 업데이트
        RefreshKeyMapUI(keyMap);
    }
    private void RefreshUIFromCurrentSettings()
    {
        var (vfx, speed, keyMap) = Setting.GetCurrentSettings();

        if (vfxSlider != null)
        {
            vfxSlider.SetValueWithoutNotify(vfx);
        }
        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.SetValueWithoutNotify(speed);
        }
        // 텍스트 업데이트
        UpdateVFXVolumeText(vfx);
        UpdateSimulationSpeedText(speed);
        // 키맵 UI 업데이트
        RefreshKeyMapUI(keyMap);
    }

    #region UI Event Handlers

    private void OnVFXVolumeChanged(float value)
    {
        Setting.SetTempVFXVolume(value);
        UpdateVFXVolumeText(value);
    }
    private void OnSimulationSpeedChanged(float value)
    {
        Setting.SetTempSimulationSpeed(value);
        UpdateSimulationSpeedText(value);
    }

    private void OnApplyButtonClicked()
    {
        List<BackgroundActionKeyMap> UI_KeyMap = new List<BackgroundActionKeyMap>();
        // 키맵 아이템에서 변경된 값을 가져와 임시 설정에 적용
        foreach (Transform child in keyMapContent)
        {
            UI_KeyMapItem itemUI = child.GetComponent<UI_KeyMapItem>();
            if (itemUI != null)
            {
                UI_KeyMap.Add(itemUI.GetKeyMap());
            }
        }
        Setting.SetTempKeyMap(UI_KeyMap);
        Setting.OnClickApplyButton();
        ApplyVFXVolume(Setting.VFXVolume);
        RefreshUIFromTempSettings();
    }
    private void OnResetToDefaultClicked()
    {
        Setting.ResetTempToDefault();
        RefreshUIFromTempSettings();
    }
    #endregion
    public void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }

    #region UI Update Methods
    private void UpdateVFXVolumeText(float volume)
    {
        if (vfxValueText != null)
        {
            vfxValueText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    private void UpdateSimulationSpeedText(float speed)
    {
        if (simulationSpeedText != null)
        {
            if(speed < 0.1f) 
            {
                simulationSpeedText.text = $"Frame";
                return;
            }
            simulationSpeedText.text = $"{speed:F1}";
        }
    }
    private void RefreshKeyMapUI(List<BackgroundActionKeyMap> keyMaps)
    {
        // 기존 키맵 아이템 제거
        foreach (Transform child in keyMapContent)
        {
            Destroy(child.gameObject);
        }
        // 현재 임시 설정값에서 키맵 가져오기
       

        // 키맵 아이템 생성
        foreach (BackgroundActionKeyMap keyMap in keyMaps)
        {
            var item = Instantiate(keyMapItemPrefab, keyMapContent);
            item.GetComponent<UI_KeyMapItem>().Initialize(keyMap);
        }
    }
    #endregion
    private void OnSettingUpdated()
    {
        // 설정이 업데이트되면 UI를 현재 설정값으로 갱신
        RefreshUIFromCurrentSettings();
        // VFX 볼륨을 오디오 믹서에 적용
        ApplyVFXVolume(Setting.VFXVolume);
        Debug.Log("UI_Settings: 설정이 적용되었습니다.");
    }
    private void OnEnable()
    {
        // 패널이 열릴 때마다 현재 임시 설정값으로 UI 업데이트
        PUMPInputManager.Current.AddBlocker(blocker);
        RefreshUIFromCurrentSettings();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        Setting.OnSettingUpdated -= OnSettingUpdated;
        PUMPInputManager.Current.RemoveBlocker(blocker);
    }

    //sound temp
    private void ApplyVFXVolume(float volume)
    {
        float dB = ConvertToDecibel(volume);
        audioMixer.SetFloat("VFX", dB);
    }
    public void ApplyCurrentVFXVolume()
    {
        float volume = Setting.VFXVolume;
        ApplyVFXVolume(volume);
    }

    private float ConvertToDecibel(float volume)
    {
        // 0 = 음소거, 1 = 0dB (최대 볼륨)
        return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
    }
}