using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputSwitchSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;

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
        m_InputField.text = "10";
    }


}
