using System;
using TMPro;
using UnityEngine;

public class DelaySupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_Input;

    private Delay.DelayType _delayType = Delay.DelayType.FixedTime;
    private int _value;

    public event Action<Delay.DelayType, int> OnValueChange;

    public void Initialize(Delay.DelayType delayType, int delay)
    {
        Set(delayType, delay);

        m_Input.onEndEdit.AddListener(value =>
        {
            if (!float.TryParse(value, out float floatValue))
            {
                return;
            }

            _value = _delayType == Delay.DelayType.FixedTime ? Mathf.RoundToInt(floatValue * 1000f) : (int)floatValue;
            OnValueChange?.Invoke(_delayType, _value);
        });
    }

    public void Set(Delay.DelayType delayType, int value)
    {
        _delayType = delayType;
        _value = value;

        m_Input.text = string.Empty;

        switch (_delayType)
        {
            case Delay.DelayType.FixedTime:
                m_Input.contentType = TMP_InputField.ContentType.DecimalNumber;
                m_Input.text = (_value * 0.001f).ToString();
                break;
            case Delay.DelayType.Frame:
                m_Input.contentType = TMP_InputField.ContentType.IntegerNumber;
                m_Input.text = _value.ToString();
                break;
        }
    }
}