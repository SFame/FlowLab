using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class UI_KeyMapItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI m_ActionNameText;
    [SerializeField] private Button m_ActionKeyButton;
    [SerializeField] private TextMeshProUGUI m_ButtonText;

    private BackgroundActionKeyMap _keyMap;
    private BackgroundActionKeyMap _originalKeyMap;

    public void Initialize(BackgroundActionKeyMap keyMap)
    {
        _keyMap = keyMap;
        
        _originalKeyMap = new BackgroundActionKeyMap
        {
            m_ActionType = Setting.GetActionType(keyMap.m_ActionType),
            m_KeyMap = Setting.GetKeyMap(keyMap.m_ActionType),
        };

        if (m_ActionNameText != null)
        {
            m_ActionNameText.text = keyMap.m_ActionType.ToString();
        }
        if (m_ButtonText != null)
        {
            
            if (_keyMap.m_KeyMap.Modifiers.Count > 0)
            {
                m_ButtonText.text = string.Join(" + ", keyMap.m_KeyMap.Modifiers) + " + " + keyMap.m_KeyMap.ActionKey.ToString();
            }
            else
            {
                m_ButtonText.text = keyMap.m_KeyMap.ActionKey.ToString();
            }
        }

        if (m_ActionKeyButton != null)
        {
            m_ActionKeyButton.onClick.AddListener(async () =>
            {
                InputKeyMap? changeKeyMap = await new KeyMapDetector().GetKeyMapAsync();
                if (changeKeyMap == null)
                {
                    return;
                }
                if (changeKeyMap.Value.Modifiers.Count > 0)
                {
                    m_ButtonText.text = string.Join(" + ", changeKeyMap.Value.Modifiers) + " + " +changeKeyMap.Value.ActionKey.ToString();
                }
                else
                {
                    m_ButtonText.text = changeKeyMap.Value.ActionKey.ToString();
                }
                _keyMap.m_KeyMap = changeKeyMap.Value;
                //_keyMap.m_ActionKeys = changeKeyMap.m_KeyMap.Modifiers;
                //_keyMap.m_Modifiers = changeKeyMap.m_KeyMap.Modifiers;
            });
        }

    }

    public void ResetToOriginal()
    {
        if (_originalKeyMap != null)
        {
            _keyMap.m_ActionType = _originalKeyMap.m_ActionType;
            _keyMap.m_KeyMap = _originalKeyMap.m_KeyMap;
            //_keyMap.m_ActionKeys = new List<KeyCode>(_originalKeyMap.m_ActionKeys);
            //_keyMap.m_Modifiers = new List<KeyCode>(_originalKeyMap.m_Modifiers);
            Initialize(_keyMap);
        }
    }
    public BackgroundActionKeyMap GetKeyMap()
    {
        return _keyMap;
    }
    public void OnDisable()
    {
        //Setting.OnClickApplyButton();
    }
}
