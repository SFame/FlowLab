using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class NumericComparatorSupport : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown m_OperatorDropdown;

    private readonly string[] _operatorTexts = new[]
    {
        "<",
        ">",
        "<=",
        ">=",
        "==",
        "!="
    };

    public ComparisonOperator Operator
    {
        get => ConvertOperatorFromIndex(m_OperatorDropdown.value);
        set => m_OperatorDropdown.value = (int)value;
    }

    public event Action<ComparisonOperator> OnOperatorChanged;

    public void Initialize()
    {
        m_OperatorDropdown.options = _operatorTexts.Select(opText => new TMP_Dropdown.OptionData(opText)).ToList();
        m_OperatorDropdown.RefreshShownValue();

        m_OperatorDropdown.onValueChanged.AddListener(index => OnOperatorChanged?.Invoke(ConvertOperatorFromIndex(index)));
    }

    private ComparisonOperator ConvertOperatorFromIndex(int index)
    {
        return (ComparisonOperator)index;
    }
}