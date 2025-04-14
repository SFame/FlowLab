using System;
using System.Collections.Generic;
using System.Linq;
using OdinSerializer;
using TMPro;
using UnityEngine;

public class Comparator : DynamicIONode, INodeAdditionalArgs<Comparator.ComparatorSerializeInfo>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefebPath => "PUMP/Prefab/Node/COMPARATOR";

    protected override float InEnumeratorXPos => -70f;

    protected override float OutEnumeratorXPos => 70f;
    
    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(180f, 100f);

    protected override string NodeDisplayName => "Comparator";

    protected override float TextSize => 16f;

    protected override int DefaultInputCount => 2;
    protected override int DefaultOutputCount => 1;
    protected override void StateUpdate(TransitionEventArgs args = null) => PushResult();
    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";
    protected override string DefineOutputName(int tpNumber) => "out";

    private bool Operating(int a, int b, string @operator) => @operator switch
    {
        "<" => a < b,
        ">" => a > b,
        "<=" => a <= b,
        ">=" => a >= b,
        _ => false
    };

    private void PushResult()
    {
        int activeCount = InputToken.Count(tp => tp.State);
        bool result = Operating(activeCount, CompareNumber, Operator);

        foreach (ITransitionPoint tp in OutputToken)
            tp.State = result;
    }
    
    protected override void OnAfterInit()
    {
        base.OnAfterInit();

        // Input count
        InputCountInputCountDropdown.value = InputCount - 1;
        InputCountInputCountDropdown.onValueChanged.AddListener(value => InputCount = value + 1);
        InputCountInputCountDropdown.onValueChanged.AddListener(_ => ReportChanges());
        
        // Compare number
        CompareNumberInputField.text = CompareNumber.ToString();
        CompareNumberInputField.onEndEdit.AddListener(countString =>
        {
            if (int.TryParse(countString, out int count))
                CompareNumber = count;
            else
            {
                CompareNumber = 0;
                CompareNumberInputField.text = CompareNumber.ToString();
            }
            PushResult();
        });
        CompareNumberInputField.onEndEdit.AddListener(countString => ReportChanges());
        
        // Operator
        OperatorDropdown.ClearOptions();
        OperatorDropdown.AddOptions(_operatorElement);
        int index = OperatorDropdown.options.FindIndex(option => option.text == Operator);
        OperatorDropdown.value = index >= 0 ? index : 0;
        OperatorDropdown.onValueChanged.AddListener(select =>
        {
            Operator = OperatorDropdown.options[select].text;
            PushResult();
        });
        OperatorDropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    #region Node Setting
    // Input Count
    private const string INPUT_COUNT_OBJECT_NAME = "CountDropdown";
    private TMP_Dropdown InputCountInputCountDropdown
    {
        get
        {
            if (_inputCountDropdown is null)
                _inputCountDropdown = transform.Find(INPUT_COUNT_OBJECT_NAME).GetComponent<TMP_Dropdown>();
                
            return _inputCountDropdown;
        }
    }
    private TMP_Dropdown _inputCountDropdown;
    
    // Operator
    private const string OPERATOR_OBJECT_NAME = "Operator";
    private TMP_Dropdown OperatorDropdown
    {
        get
        {
            if (_operatorDropdown is null)
                _operatorDropdown = transform.Find(OPERATOR_OBJECT_NAME).GetComponent<TMP_Dropdown>();
            
            return _operatorDropdown;
        }
    }
    private TMP_Dropdown _operatorDropdown;
    private readonly List<string> _operatorElement = new() { "<", ">", "<=", ">=" };
    
    // Compare number
    private const string COMPARE_NUMBER_OBJECT_NAME = "CompareNumber";
    private TMP_InputField CompareNumberInputField
    {
        get
        {
            if (_compareNumberInputField is null)
                _compareNumberInputField = transform.Find(COMPARE_NUMBER_OBJECT_NAME).GetComponent<TMP_InputField>();
            return _compareNumberInputField;
        }
    }
    private TMP_InputField _compareNumberInputField;
    #endregion

    #region Serialize target
    // InputCount 포함
    private int CompareNumber { get; set; } = 0;
    private string Operator { get; set; } = "<";
    #endregion

    public ComparatorSerializeInfo AdditionalTArgs
    {
        get => new() { _inputCount = InputCount, _compareNumber = CompareNumber, _operator = Operator };

        set
        {
            InputCount = value._inputCount;
            CompareNumber = value._compareNumber;
            Operator = value._operator;
        }
    }
    
    public object AdditionalArgs { get => AdditionalTArgs; set => AdditionalTArgs = (ComparatorSerializeInfo)value; }
    
    [Serializable]
    public struct ComparatorSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public int _compareNumber;
        [OdinSerialize] public string _operator;

        public override string ToString()
        {
            return $"Input Count: {_inputCount}, Compare Number: {_compareNumber}, Operator: {_operator}";
        }
    }
}