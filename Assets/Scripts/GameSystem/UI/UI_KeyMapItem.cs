using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using Utils;

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
                BackgroundActionKeyMap changekeyMap = await new KeyMapDetector(_keyMap.m_ActionType).GetKeyMapAsync();
                if (changekeyMap == null)
                {
                    return;
                }
                if (changekeyMap.m_Modifiers.Count > 0)
                {
                    m_ButtonText.text = string.Join(" + ", changekeyMap.m_Modifiers) + " + " + string.Join(" + ", changekeyMap.m_ActionKeys);
                }
                else
                {
                    m_ButtonText.text = string.Join(" + ", changekeyMap.m_ActionKeys);
                }
                _keyMap.m_ActionKeys = changekeyMap.m_ActionKeys;
                _keyMap.m_Modifiers = changekeyMap.m_Modifiers;

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
