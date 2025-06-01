using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

public class UI_Settings : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider vfxSlider;
    [SerializeField] private TextMeshProUGUI vfxValueText;

    [Header("Simulation Delay Settings")]
    [SerializeField] private Slider simulationSpeedSlider;
    [SerializeField] private TextMeshProUGUI simulationSpeedText;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetToDefaultButton;
    [SerializeField] private Button closeButton;

    [Header("KeyMap Settings")]
    [SerializeField] private GameObject keyMapPanel;
    [SerializeField] private Transform keyMapContent;
    [SerializeField] private GameObject keyMapItemPrefab;


    private void Start()
    {
        InitializeUI();
        SetupEventHandlers();

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

        // 키맵 패널 초기 상태
        if (keyMapPanel != null)
        {
            keyMapPanel.SetActive(false);
        }

        // 초기 UI 값 설정
        RefreshUIFromTempSettings();
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
        Setting.OnClickApplyButton();
    }
    private void OnResetToDefaultClicked()
    {
        Setting.ResetTempToDefault();
        RefreshUIFromTempSettings();
    }
    #endregion
    private void OnCloseButtonClicked()
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
            simulationSpeedText.text = $"{speed:F1}";
        }
    }
    private void RefreshKeyMapUI()
    {
        // 기존 키맵 아이템 제거
        foreach (Transform child in keyMapContent)
        {
            Destroy(child.gameObject);
        }
        // 현재 임시 설정값에서 키맵 가져오기
        List<BackgroundActionKeyMap> keyMapList = Setting.CurrentKeyMap;

        // 키맵 아이템 생성
        foreach (BackgroundActionKeyMap keyMap in keyMapList)
        {
            var item = Instantiate(keyMapItemPrefab, keyMapContent);
            var dropbox = item.GetComponentInChildren<TextMeshProUGUI>(); // 키맵Item 스크립트 가져와서 값넘겨주고 드롭박스UI 갱신시키기
            if (dropbox != null)
            {
                
            }
        }
    }
    private void OnKeyMapItemChanged()
    {
        // 변경된 키맵 리스트 수집
        List<BackgroundActionKeyMap> updatedKeyMapList = new List<BackgroundActionKeyMap>();

        foreach (Transform child in keyMapContent)
        {
            //KeyMapItemUI itemUI = child.GetComponent<KeyMapItemUI>();
            //if (itemUI != null)
            //{
            //    updatedKeyMapList.Add(itemUI.GetKeyMap());
            //}
        }

        // 임시 키맵 업데이트
        Setting.SetTempKeyMap(updatedKeyMapList);
    }
    #endregion
    private void OnSettingUpdated()
    {
        // 현재 적용된 설정값으로 UI 업데이트
        Debug.Log("UI_Settings: 설정이 적용되었습니다.");
    }
    private void OnEnable()
    {
        // 패널이 열릴 때마다 현재 임시 설정값으로 UI 업데이트
        RefreshUIFromTempSettings();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        Setting.OnSettingUpdated -= OnSettingUpdated;
    }
}