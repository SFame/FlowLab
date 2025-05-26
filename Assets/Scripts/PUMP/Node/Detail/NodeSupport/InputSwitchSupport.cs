using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InputSwitchSupport : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] public TMP_InputField m_InputField;
    [SerializeField] private Image m_InputImage;


    public event Action<object> OnValueChanged;

    // 입력 필드의 텍스트를 반환
    public string GetInputText()
    {
        return m_InputField != null ? m_InputField.text : string.Empty;
    }

    // 필요시 외부에서 텍스트를 설정할 수 있도록 메서드 추가
    public void SetInputText(object value)
    {
        if (m_InputField != null)
            m_InputField.text = value.ToString();
    }

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(InvokeValueChangeEvent);
        m_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        m_InputField.text = "0";
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


    public void OnPointerDown(PointerEventData eventData)
    {
        m_InputImage.DOKill();
        m_InputImage.DOFade(0.35f, 0.1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_InputImage.DOKill();
        m_InputImage.DOFade(0f, 0.1f);
    }
}

