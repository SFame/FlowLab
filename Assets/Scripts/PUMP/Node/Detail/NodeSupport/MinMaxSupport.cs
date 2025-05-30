using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;

public class MinMaxSupport : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown m_InputCountDropdown;
    [SerializeField] private TMP_Dropdown m_OperatorDropdown;
    [SerializeField] private TextMeshProUGUI m_NodeName;

    private readonly List<string> _operatorElement = new() { "Min", "Max" };

    public event Action<int> OnInputCountUpdated;
    public event Action<string> OnOperatorUpdated;

    public int InputCount
    {
        get => m_InputCountDropdown.value + 1;
        set => m_InputCountDropdown.value = value - 1;
    }

    public string Operator
    {
        get => m_OperatorDropdown.options[m_OperatorDropdown.value].text;
        set
        {
            int index = m_OperatorDropdown.options.FindIndex(option => option.text == value);
            m_OperatorDropdown.value = index >= 0 ? index : 0;
            m_NodeName.text = m_OperatorDropdown.options[m_OperatorDropdown.value].text;
        }
    }
    public string NodeName
    {
        get => m_NodeName.text;
        set => m_NodeName.text = value;
    }
    public void Initialize(int inputCount, string @operator)
    {
        InputCount = inputCount;
        m_OperatorDropdown.ClearOptions();
        m_OperatorDropdown.AddOptions(_operatorElement);
        Operator = @operator;

        m_InputCountDropdown.onValueChanged.AddListener(value => OnInputCountUpdated?.Invoke(value + 1));
        m_OperatorDropdown.onValueChanged.AddListener(value => OnOperatorUpdated?.Invoke(m_OperatorDropdown.options[value].text));
        m_OperatorDropdown.onValueChanged.AddListener(value => NodeName = $"{m_OperatorDropdown.options[value].text}");
    }
}