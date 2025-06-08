using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
            m_ActionKeys = Setting.GetActionKeys(keyMap.m_ActionType),
            m_Modifiers = Setting.GetActionModifiers(keyMap.m_ActionType)
        };

        if (m_ActionNameText != null)
        {
            m_ActionNameText.text = keyMap.m_ActionType.ToString();
        }
        if (m_ButtonText != null)
        {
            
            if (_keyMap.m_Modifiers.Count > 0)
            {
                m_ButtonText.text = string.Join(" + ", keyMap.m_Modifiers) + " + " + string.Join(" + ", keyMap.m_ActionKeys);
            }
            else
            {
                m_ButtonText.text = string.Join(" + ", keyMap.m_ActionKeys);
            }
        }

        if (m_ActionKeyButton != null)
        {
            m_ActionKeyButton.onClick.AddListener(async () =>
            {
                BackgroundActionKeyMap changeKeyMap = await new KeyMapDetector(_keyMap.m_ActionType).GetKeyMapAsync();
                if (changeKeyMap == null)
                {
                    return;
                }
                if (changeKeyMap.m_Modifiers.Count > 0)
                {
                    m_ButtonText.text = string.Join(" + ", changeKeyMap.m_Modifiers) + " + " + string.Join(" + ", changeKeyMap.m_ActionKeys);
                }
                else
                {
                    m_ButtonText.text = string.Join(" + ", changeKeyMap.m_ActionKeys);
                }
                _keyMap.m_ActionKeys = changeKeyMap.m_ActionKeys;
                _keyMap.m_Modifiers = changeKeyMap.m_Modifiers;
            });

        }

        
    }

    public void ResetToOriginal()
    {
        if (_originalKeyMap != null)
        {
            _keyMap.m_ActionType = _originalKeyMap.m_ActionType;
            _keyMap.m_ActionKeys = new List<KeyCode>(_originalKeyMap.m_ActionKeys);
            _keyMap.m_Modifiers = new List<KeyCode>(_originalKeyMap.m_Modifiers);
            Initialize(_keyMap);
        }
    }
    public BackgroundActionKeyMap GetKeyMap()
    {
        return _keyMap;
    }

}
