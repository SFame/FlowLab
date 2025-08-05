using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ComparatorSupport : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown m_InputCountDropdown;
    [SerializeField] private TMP_Dropdown m_OperatorDropdown;
    [SerializeField] private TMP_InputField m_CompareNumberInputField;

    private readonly List<string> _operatorElement = new() { "<", ">", "<=", ">=", "==", "!=" };

    public event Action<int> OnInputCountUpdated;
    public event Action<string> OnOperatorUpdated;
    public event Action<int> OnCompareNumberUpdated;

    public int InputCount
    {
        get => m_InputCountDropdown.value + 1;
        set => m_InputCountDropdown.value = value - 1;
    }

    public int CompareNumber
    {
        get => int.TryParse(m_CompareNumberInputField.text, out int result) ? result : 0;
        set => m_CompareNumberInputField.text = value.ToString();
    }

    public string Operator
    {
        get => m_OperatorDropdown.options[m_OperatorDropdown.value].text;
        set
        {
            int index = m_OperatorDropdown.options.FindIndex(option => option.text == value);
            m_OperatorDropdown.value = index >= 0 ? index : 0;
        }
    }

    public void Initialize(int inputCount, int compareNumber, string @operator)
    {
        InputCount = inputCount;

        m_OperatorDropdown.ClearOptions();
        m_OperatorDropdown.AddOptions(_operatorElement);
        Operator = @operator;

        CompareNumber = CompareNumber;

        m_InputCountDropdown.onValueChanged.AddListener(value => OnInputCountUpdated?.Invoke(value + 1));
        m_OperatorDropdown.onValueChanged.AddListener(value => OnOperatorUpdated?.Invoke(m_OperatorDropdown.options[value].text));
        m_CompareNumberInputField.onEndEdit.AddListener(value => OnCompareNumberUpdated?.Invoke(int.TryParse(value, out int result) ? result : 0));
    }
}