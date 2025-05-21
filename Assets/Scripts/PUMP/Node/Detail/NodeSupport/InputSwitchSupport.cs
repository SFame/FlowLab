using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputSwitchSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

    public event Action<object> OnValueChanged;

    // 입력 필드의 텍스트를 반환
    public string GetInputText()
    {
        return m_InputField != null ? m_InputField.text : string.Empty;
    }

    // 필요시 외부에서 텍스트를 설정할 수 있도록 메서드 추가
    public void SetInputText(string value)
    {
        if (m_InputField != null)
            m_InputField.text = value;
    }

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(InvokeValueChangeEvent);
        m_InputField.contentType = TMP_InputField.ContentType.Standard;
        m_InputField.text = "10";
    }

    private void InvokeValueChangeEvent(string value)
    {
        if (int.TryParse(value, out int i))
        {
            OnValueChanged?.Invoke(i);
            return;
        }
        if (float.TryParse(value, out float f))
        {
            OnValueChanged?.Invoke(f);
            return;
        }
        OnValueChanged?.Invoke(value);
    }


}

