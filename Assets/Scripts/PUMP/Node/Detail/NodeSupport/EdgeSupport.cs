using System;
using TMPro;
using UnityEngine;

public class EdgeSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;
    private EdgeDetector.DelayType _delayType = EdgeDetector.DelayType.FixedTime;
    private int _value;

    public event Action<float> OnValueChanged;

    public void Initialize(Action<float> valueChangeListener)
    {
        if (valueChangeListener == null)
        {
            Debug.LogError("valueChangeListener is null");
            return;
        }

        m_InputField.onEndEdit.AddListener(strValue =>
        {
            if (!float.TryParse(strValue, out float floatValue))
            {
                return;
            }

            _value = _delayType == EdgeDetector.DelayType.FixedTime ? Mathf.RoundToInt(floatValue * 1000f) : (int)floatValue;
            float callbackValue = _delayType == EdgeDetector.DelayType.FixedTime ? (_value * 0.001f) : _value;

            OnValueChanged?.Invoke(callbackValue);
            valueChangeListener?.Invoke(callbackValue);
        });
    }

    public void Set(EdgeDetector.DelayType delayType, float value)
    {
        _delayType = delayType;
        _value = _delayType == EdgeDetector.DelayType.FixedTime ? Mathf.RoundToInt(value * 1000f) : (int)value;

        m_InputField.text = string.Empty;

        switch (_delayType)
        {
            case EdgeDetector.DelayType.FixedTime:
                m_InputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                m_InputField.text = (_value * 0.001f).ToString("F2");
                break;
            case EdgeDetector.DelayType.Frame:
                m_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                m_InputField.text = _value.ToString();
                break;
        }
    }

    public void SetInputValue(float value)
    {
        _value = _delayType == EdgeDetector.DelayType.FixedTime ? Mathf.RoundToInt(value * 1000f) : (int)value;

        if (_delayType == EdgeDetector.DelayType.FixedTime)
        {
            m_InputField.text = (_value * 0.001f).ToString("F2");
        }
        else
        {
            m_InputField.text = _value.ToString();
        }
    }
}