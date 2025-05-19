using System;
using TMPro;
using UnityEngine;

public class EdgeSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

    public void Initialize(Action<float> valueChangeListener)
    {
        if (valueChangeListener == null)
        {
            Debug.LogError("valueChangeListener is null");
            return;
        }

        m_InputField.onEndEdit.AddListener(strValue =>
        {
            if (float.TryParse(strValue, out float floatValue))
            {
                valueChangeListener?.Invoke(floatValue);
            }
        });
    }

    public void SetInputValue(float value)
    {
        m_InputField.text = value.ToString();
    }
}