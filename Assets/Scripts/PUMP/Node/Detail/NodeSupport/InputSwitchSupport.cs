using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InputSwitchSupport : MonoBehaviour
{
    [SerializeField] public TMP_InputField m_InputField;
    [SerializeField] private Image m_InputImage;
    [SerializeField] private CanvasGroup m_InputPanelGroup;
    [SerializeField] private float m_InputPanelFadeDuration = 0.2f;


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
        m_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        m_InputField.text = "";
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

    public void OpenInputPanel()
    {
        m_InputPanelGroup.blocksRaycasts = true;
        m_InputPanelGroup.interactable = true;

        m_InputPanelGroup.DOKill();
        m_InputPanelGroup.DOFade(1f, m_InputPanelFadeDuration);
    }

    public void CloseInputPanel()
    {
        m_InputPanelGroup.DOKill();
        m_InputPanelGroup.DOFade(0f, m_InputPanelFadeDuration).OnComplete(() =>
        {
            m_InputField.DeactivateInputField();
            m_InputPanelGroup.blocksRaycasts = false;
            m_InputPanelGroup.interactable = false;
        });
    }

    public void ButtonShadowActive()
    {
        m_InputImage.DOKill();
        m_InputImage.DOFade(0.35f, 0.1f);
    }

    public void ButtonShadowInactive()
    {
        m_InputImage.DOKill();
        m_InputImage.DOFade(0f, 0.1f);
    }
}

