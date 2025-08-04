using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class KeyInputSupport : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown m_KeycodeDropdown;

    private static List<KeyValuePair<string, int>> _keyCodePairs = null;

    public event Action<KeyCode> OnValueChange;

    public void Initialize(KeyCode initKeyCode)
    {
        SetKeyCodePairs();
        m_KeycodeDropdown.options = _keyCodePairs.Select(pair => new TMP_Dropdown.OptionData(pair.Key)).ToList();
        m_KeycodeDropdown.RefreshShownValue();
        m_KeycodeDropdown.value = ConvertKeyCodeToIndex(initKeyCode);
        m_KeycodeDropdown.onValueChanged.AddListener(value => OnValueChange.Invoke(ConvertIndexToKeyCode(value)));
    }

    private void SetKeyCodePairs()
    {
        if (_keyCodePairs != null)
            return;

        _keyCodePairs = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>()
            .Select(k => new KeyValuePair<string, int>(k.ToString(), (int)k))
            .ToList();
    }

    private int ConvertKeyCodeToIndex(KeyCode keyCode)
    {
        return _keyCodePairs.FindIndex(pair => pair.Key == keyCode.ToString());
    }

    private KeyCode ConvertIndexToKeyCode(int index)
    {
        return (KeyCode)_keyCodePairs[index].Value;
    }
}