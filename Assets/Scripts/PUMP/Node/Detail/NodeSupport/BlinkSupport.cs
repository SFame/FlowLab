using System;
using TMPro;
using UnityEngine;
using Utils;

public class BlinkSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

    private int _value;


    public event Action<int> OnValueChange;

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            m_InputField.text = _value.ToString();
        }
    }

    public void Initialize(int initValue, int maxValue, Action<int> onValueChange)
    {
        _value = initValue;
        m_InputField.text = _value.ToString();
        OnValueChange += onValueChange;

        m_InputField.onEndEdit.AddListener(value =>
        {
            _value = int.TryParse(value, out int strToInt) ? strToInt.Clamp(1, maxValue) : 1;

            m_InputField.text = _value.ToString();
            OnValueChange?.Invoke(_value);
        });
    }
}