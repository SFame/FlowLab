using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_KeyMapItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI m_ActionNameText;
    [SerializeField] private Button m_ActionKeyButton;
    [SerializeField] private TextMeshProUGUI m_ButtonText;

    private BackgroundActionKeyMap _keyMap;
    private BackgroundActionKeyMap _originalKeyMap;

    // 키맵이 변경되었을 때 발생하는 이벤트 (자기 자신을 파라미터로 전달)
    public event Action<UI_KeyMapItem> OnKeyMapChanged;

    public void Initialize(BackgroundActionKeyMap keyMap)
    {
        _keyMap = keyMap;

        _originalKeyMap = new BackgroundActionKeyMap
        {
            m_ActionType = Setting.GetActionType(keyMap.m_ActionType),
            m_KeyMap = Setting.GetKeyMap(keyMap.m_ActionType),
        };

        // UI 업데이트
        UpdateUI();

        // 버튼 이벤트 설정 (중복 등록 방지)
        if (m_ActionKeyButton != null)
        {
            m_ActionKeyButton.onClick.RemoveAllListeners();
            m_ActionKeyButton.onClick.AddListener(() => OnButtonClicked().Forget());
        }
    }

    private async UniTask OnButtonClicked()
    {
        InputKeyMap? changeKeyMap = await new KeyMapDetector().GetKeyMapAsync();

        if (changeKeyMap == null)
        {
            return;
        }

        // 키맵 변경
        _keyMap.m_KeyMap = changeKeyMap.Value;

        // UI 업데이트
        UpdateUI();

        // 이벤트 발생 - UI_Settings에서 중복 체크 수행
        OnKeyMapChanged?.Invoke(this);
    }

    /// <summary>
    /// 키맵을 UI에 반영 (내부 호출용)
    /// </summary>
    private void UpdateUI()
    {
        if (m_ActionNameText != null)
        {
            m_ActionNameText.text = _keyMap.m_ActionType.ToString();
        }

        if (m_ButtonText != null)
        {
            if (_keyMap.m_KeyMap.ActionKey == ActionKeyCode.None)
            {
                // None인 경우 빨간색으로 표시
                m_ButtonText.text = "<color=red>None (Duplicate!)</color>";
            }
            else if (_keyMap.m_KeyMap.Modifiers.Count > 0)
            {
                m_ButtonText.text = string.Join(" + ", _keyMap.m_KeyMap.Modifiers) + " + " + _keyMap.m_KeyMap.ActionKey.ToString();
            }
            else
            {
                m_ButtonText.text = _keyMap.m_KeyMap.ActionKey.ToString();
            }
        }
    }

    /// <summary>
    /// 외부에서 키맵을 업데이트 (이벤트 발생 없이 조용히 업데이트)
    /// UI_Settings에서 중복 체크 후 호출
    /// </summary>
    public void UpdateKeyMap(BackgroundActionKeyMap newKeyMap)
    {
        _keyMap = newKeyMap;
        UpdateUI();
    }

    public void ResetToOriginal()
    {
        if (_originalKeyMap != null)
        {
            _keyMap.m_ActionType = _originalKeyMap.m_ActionType;
            _keyMap.m_KeyMap = _originalKeyMap.m_KeyMap;
            UpdateUI();

            // 리셋 시에도 이벤트 발생
            OnKeyMapChanged?.Invoke(this);
        }
    }

    public BackgroundActionKeyMap GetKeyMap()
    {
        return _keyMap;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        OnKeyMapChanged = null;

        // 버튼 리스너 제거
        if (m_ActionKeyButton != null)
        {
            m_ActionKeyButton.onClick.RemoveAllListeners();
        }
    }
}
