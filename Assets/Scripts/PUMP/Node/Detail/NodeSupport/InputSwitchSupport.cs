using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputSwitchSupport : MonoBehaviour, ISoundable
{
    [SerializeField] private TMP_InputField m_InputField;
    [SerializeField] private CanvasGroup m_InputPanelGroup;
    [SerializeField] private float m_InputPanelFadeDuration;
    [SerializeField] private Image m_NodeImage;
    [SerializeField] private Sprite m_ActiveImage;
    [SerializeField] private Sprite m_DeactiveImage;


    public event Action OnValueChanged;
    public event SoundEventHandler OnSounded;

    private object _inputManagerBlockerObject = new();

    public void Initialize(Transition initValue)
    {
        m_InputField.onEndEdit.AddListener(_ => OnValueChanged?.Invoke());
        m_InputField.onSelect.AddListener(_ => InputManager.AddBlocker(_inputManagerBlockerObject));
        m_InputField.onDeselect.AddListener(_ => InputManager.RemoveBlocker(_inputManagerBlockerObject));
        m_NodeImage.sprite = m_DeactiveImage;
        SetType(initValue.Type);
        SetValue(initValue);
        m_InputField.text = "";
    }

    public bool TryGetValue(TransitionType asType, out Transition value)
    {
        bool success = false;
        value = asType.Default();

        switch (asType)
        {
            case TransitionType.Int:
                success = int.TryParse(string.IsNullOrEmpty(m_InputField.text) ? "0" : m_InputField.text, out int intResult);
                value = intResult;
                break;
            case TransitionType.Float:
                success = float.TryParse(string.IsNullOrEmpty(m_InputField.text) ? "0.0" : m_InputField.text, out float floatResult);
                value = floatResult;
                break;
            case TransitionType.String:
                success = true;
                value = m_InputField.text ?? string.Empty;
                break;
        }

        return success;
    }

    public void SetValue(Transition value)
    {
        SetValueAsync(value).Forget();
    }

    public void SetType(TransitionType type)
    {
        switch (type)
        {
            case TransitionType.Int:
                m_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                return;
            case TransitionType.Float:
                m_InputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                return;
            case TransitionType.String:
                m_InputField.contentType = TMP_InputField.ContentType.Standard;
                return;
        }

        throw new Exception($"{GetType().Name}.SetType(): 허용되지 않는 타입 할당: {type.ToString()}");
    }

    public void SetDown(bool isDown)
    {
        m_NodeImage.sprite = isDown ? m_ActiveImage : m_DeactiveImage;
    }

    public void PlaySound(bool isActivate)
    {
        OnSounded?.Invoke(this, new SoundEventArgs(isActivate ? 1 : 2));
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

    private async UniTaskVoid SetValueAsync(Transition value)
    {
        await UniTask.Yield();

        if (value.IsNull || value.Type == TransitionType.None || value.Type == TransitionType.Bool || value.Type == TransitionType.Pulse)
        {
            m_InputField.text = string.Empty;
            return;
        }

        if (m_InputField != null)
        {
            m_InputField.text = value.GetValueString();
        }
    }

    private void OnDestroy()
    {
        InputManager.RemoveBlocker(_inputManagerBlockerObject);
    }

    private void OnDisable()
    {
        InputManager.RemoveBlocker(_inputManagerBlockerObject);
    }
}