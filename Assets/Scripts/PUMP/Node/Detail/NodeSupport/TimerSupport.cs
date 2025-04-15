using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerSupport : MonoBehaviour
{
    [SerializeField] private Slider m_Slider;
    [SerializeField] private TMP_InputField m_InputField;

    public event Action<float> OnValueChanged;

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(InvokeValueChangeEvent);
        m_Slider.value = 1f;
    }

    public void SetText(float value)
    {
        m_InputField.text = value.ToString();
    }

    public void SliderUpdate(float value)
    {
        value = Mathf.Clamp01(value);
        m_Slider.value = value;
    }

    private void InvokeValueChangeEvent(string value)
    {
        if (float.TryParse(value, out float result))
        {
            OnValueChanged?.Invoke(result);
            return;
        }

        m_InputField.text = "0";
        OnValueChanged?.Invoke(0f);
    }
}
