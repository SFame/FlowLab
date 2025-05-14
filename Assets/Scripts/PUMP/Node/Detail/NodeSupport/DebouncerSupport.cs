using System;
using TMPro;
using UnityEngine;

public class DebouncerSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

    public event Action<int> OnValueChanged;

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(InvokeValueChangeEvent);
    }

    public void SetText(int value)
    {
        m_InputField.text = value.ToString();
    }

    private void InvokeValueChangeEvent(string value)
    {
        if (int.TryParse(value, out int result))
        {
            OnValueChanged?.Invoke(result);
            return;
        }

        m_InputField.text = "0";
        OnValueChanged?.Invoke(0);
    }
}