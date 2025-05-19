using System;
using TMPro;
using UnityEngine;

public class DebouncerSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

    public event Action<float> OnValueChanged;

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(InvokeValueChangeEvent);
    }

    public void SetText(float value)
    {
        m_InputField.text = value.ToString();
    }

    private void InvokeValueChangeEvent(string value)
    {
        if (float.TryParse(value, out float result))
        {
            OnValueChanged?.Invoke(result);
            return;
        }

        m_InputField.text = "0";
        OnValueChanged?.Invoke(0);
    }
}