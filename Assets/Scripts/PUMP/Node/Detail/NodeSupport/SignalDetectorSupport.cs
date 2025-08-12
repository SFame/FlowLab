using System;
using TMPro;
using UnityEngine;

public class SignalDetectorSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_Input;
    private SignalDetector.DelayType _delayType = SignalDetector.DelayType.FixedTime;
    private int _value;
    public float Value
    {
        get
        {
            if (float.TryParse(m_Input.text, out float result))
            {
                return result;
            }

            Debug.LogError("Parse fail");
            return 0f;
        }

        set => m_Input.text = value.ToString();
    }

    public event Action<float> OnEndEdit;

    public void Initialize()
    {
        m_Input.onEndEdit.AddListener(strValue =>
        {
            if (!float.TryParse(strValue, out float floatValue))
            {
                return;
            }

            _value = _delayType == SignalDetector.DelayType.FixedTime ? Mathf.RoundToInt(floatValue * 1000f) : (int)floatValue;
            float callbackValue = _delayType == SignalDetector.DelayType.FixedTime ? (_value * 0.001f) : _value;

            OnEndEdit?.Invoke(callbackValue);
        });
    }

    public void Set(SignalDetector.DelayType delayType, float value)
    {
        _delayType = delayType;
        _value = _delayType == SignalDetector.DelayType.FixedTime ? Mathf.RoundToInt(value * 1000f) : (int)value;

        m_Input.text = string.Empty;

        switch (_delayType)
        {
            case SignalDetector.DelayType.FixedTime:
                m_Input.contentType = TMP_InputField.ContentType.DecimalNumber;
                m_Input.text = (_value * 0.001f).ToString("F2");
                break;
            case SignalDetector.DelayType.Frame:
                m_Input.contentType = TMP_InputField.ContentType.IntegerNumber;
                m_Input.text = _value.ToString();
                break;
        }
    }
}