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
    [SerializeField] private Toggle immediatelyToggle;
    [SerializeField] private TMP_InputField loopThresholdInputField;


    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetToDefaultButton;
    public Button closeButton;

    [Header("KeyMap Settings")]
    [SerializeField] private Transform keyMapContent;
    [SerializeField] private GameObject keyMapItemPrefab;

    private const float FRAME_MODE_THRESHOLD = 0.001f;

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
        if (immediatelyToggle != null)
        {
            immediatelyToggle.onValueChanged.AddListener(OnImmediatelyToggleChanged);
        }
        if (loopThresholdInputField != null)
        {
            loopThresholdInputField.onEndEdit.AddListener(OnLoopThresholdChanged);
            loopThresholdInputField.characterLimit = 2;
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
        var (vfx, speed, immediately, loopThreshold, keyMap) = Setting.GetTempSettings();

        if (vfxSlider != null)
        {
            vfxSlider.SetValueWithoutNotify(vfx);
        }

        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.SetValueWithoutNotify(speed);
        }
        if (immediatelyToggle != null)
        {
            immediatelyToggle.SetIsOnWithoutNotify(immediately);
        }
        if (loopThresholdInputField != null)
        {
            loopThresholdInputField.SetTextWithoutNotify(loopThreshold.ToString());
        }
        // 텍스트 업데이트
        UpdateVFXVolumeText(vfx);
        UpdateSimulationSpeedText(speed);
        UpdateSimulationSpeedInteractable(immediately);
        UpdateLoopThresholdVisibility(immediately);
        // 키맵 UI 업데이트
        RefreshKeyMapUI(keyMap);
    }
    private void RefreshUIFromCurrentSettings()
    {
        var (vfx, speed, immediately, loopThreshold, keyMap) = Setting.GetCurrentSettings();

        if (vfxSlider != null)
        {
            vfxSlider.SetValueWithoutNotify(vfx);
        }
        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.SetValueWithoutNotify(speed);
        }
        if (immediatelyToggle != null)
        {
            immediatelyToggle.SetIsOnWithoutNotify(immediately);
        }
        if (loopThresholdInputField != null)
        {
            loopThresholdInputField.SetTextWithoutNotify(loopThreshold.ToString());
        }

        // 텍스트 업데이트
        UpdateVFXVolumeText(vfx);
        UpdateSimulationSpeedText(speed);
        UpdateSimulationSpeedInteractable(immediately);
        UpdateLoopThresholdVisibility(immediately);
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
    private void OnImmediatelyToggleChanged(bool isOn)
    {
        Setting.SetTempIsImmediately(isOn);
        UpdateSimulationSpeedInteractable(isOn);
        UpdateLoopThresholdVisibility(isOn);
    }
    private void OnLoopThresholdChanged(string value)
    {
        if (int.TryParse(value, out int threshold))
        {
            // 2~20 범위로 제한
            int clampedThreshold = Mathf.Clamp(threshold, 2, 20);
            Setting.SetTempLoopThreshold(clampedThreshold);

            // 범위를 벗어난 값이면 InputField에 제한된 값으로 표시
            if (threshold != clampedThreshold)
            {
                loopThresholdInputField.SetTextWithoutNotify(clampedThreshold.ToString());
            }
        }
        else
        {
            // 잘못된 값이면 현재 설정값으로 되돌리기
            var (_, _, _, loopThreshold, _) = Setting.GetTempSettings();
            loopThresholdInputField.SetTextWithoutNotify(loopThreshold.ToString());
        }
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
            if(speed < FRAME_MODE_THRESHOLD) 
            {
                simulationSpeedText.text = $"Frame";
                return;
            }
            simulationSpeedText.text = $"{speed:F2}";
        }
    }
    private void UpdateSimulationSpeedInteractable(bool isImmediately)
    {
        // Immediately 토글이 체크되면 슬라이더와 텍스트를 비활성화
        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.interactable = !isImmediately;

            // 슬라이더 Handle
            var sliderColors = simulationSpeedSlider.colors;
            sliderColors.normalColor = isImmediately ? new Color(0.2f, 0.2f, 0.2f, 0.2f) : Color.white;
            sliderColors.highlightedColor = isImmediately ? new Color(0.2f, 0.2f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            sliderColors.selectedColor = isImmediately ? new Color(0.2f, 0.2f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            sliderColors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            simulationSpeedSlider.colors = sliderColors;

            // 슬라이더 Fill
            var fillRect = simulationSpeedSlider.fillRect;
            if (fillRect != null)
            {
                var fillImage = fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    Color fillColor = fillImage.color;
                    fillImage.color = new Color(fillColor.r, fillColor.g, fillColor.b, isImmediately ? 0.3f : 1f);
                }
            }
        }

        if (simulationSpeedText != null)
        {
            // 현재 색상에서 투명도 조절
            Color currentColor = simulationSpeedText.color;
            simulationSpeedText.color = new Color(currentColor.r, currentColor.g, currentColor.b, isImmediately ? 0.2f : 1f);
        }
    }
    private void UpdateLoopThresholdVisibility(bool isImmediately)
    {
        // Immediately 토글이 체크되면 LoopThreshold InputField를 보이게 하고, 아니면 숨김
        if (loopThresholdInputField != null)
        {
            loopThresholdInputField.gameObject.SetActive(isImmediately);
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
        PUMPInputManager.Current?.RemoveBlocker(blocker);
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