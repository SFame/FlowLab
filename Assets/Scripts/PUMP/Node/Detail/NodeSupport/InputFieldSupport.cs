using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class InputFieldSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_InputField;
    [SerializeField] private RectTransform m_InputPanelRect;
    [SerializeField] private float m_InputPanelFadeDuration = 0.5f;
    [SerializeField] private Vector2 m_InputPanelExtendedSize = new Vector2(300f, 600f);

    private object _inputManagerBlockerObject = new();
    private Vector2 _inputPanelDefaultSize;
    private Sequence _rectSizeSequence;

    private void SetFieldType(TransitionType transitionType)
    {
        m_InputField.contentType = transitionType switch
        {
            TransitionType.None => throw new TransitionNoneTypeException(),
            TransitionType.Bool => throw new TransitionTypeArgumentOutOfRangeException(transitionType),
            TransitionType.Int => TMP_InputField.ContentType.IntegerNumber,
            TransitionType.Float => TMP_InputField.ContentType.DecimalNumber,
            TransitionType.String => TMP_InputField.ContentType.Standard,
            _ => throw new TransitionTypeArgumentOutOfRangeException(transitionType)
        };
    }

    private void OnDestroy()
    {
        InputManager.RemoveBlocker(_inputManagerBlockerObject);
    }

    private void OnDisable()
    {
        InputManager.RemoveBlocker(_inputManagerBlockerObject);
    }

    public string Text
    {
        get => m_InputField.text;
        set => m_InputField.text = value;
    }

    public bool BlockOpenPanel { get; set; }

    public event Action<string> OnEndEdit;

    public void Initialize()
    {
        m_InputField.onEndEdit.AddListener(value => OnEndEdit?.Invoke(value));
        m_InputField.onSelect.AddListener(_ => InputManager.AddBlocker(_inputManagerBlockerObject));
        m_InputField.onDeselect.AddListener(_ => InputManager.RemoveBlocker(_inputManagerBlockerObject));
        m_InputPanelRect.pivot = Vector2.one;
        _inputPanelDefaultSize = m_InputPanelRect.sizeDelta;
    }

    public void Refresh(TransitionType transitionType)
    {
        m_InputField.text = string.Empty;
        SetFieldType(transitionType);
        Text = transitionType.Default().GetValueString();
    }

    public void OpenInputPanel()
    {
        if (BlockOpenPanel)
            return;

        _rectSizeSequence?.Kill();

        _rectSizeSequence = DOTween.Sequence();
        _rectSizeSequence.Append(m_InputPanelRect.DOSizeDelta(new Vector2(m_InputPanelExtendedSize.x, m_InputPanelRect.sizeDelta.y), m_InputPanelFadeDuration * 0.5f))
            .Append(m_InputPanelRect.DOSizeDelta(m_InputPanelExtendedSize, m_InputPanelFadeDuration * 0.5f));
    }

    public void CloseInputPanel()
    {
        if (BlockOpenPanel)
            return;

        m_InputField.DeactivateInputField();
        _rectSizeSequence?.Kill();

        _rectSizeSequence = DOTween.Sequence();
        _rectSizeSequence.Append(m_InputPanelRect.DOSizeDelta(new Vector2(m_InputPanelRect.sizeDelta.x, _inputPanelDefaultSize.y), m_InputPanelFadeDuration * 0.5f))
            .Append(m_InputPanelRect.DOSizeDelta(_inputPanelDefaultSize, m_InputPanelFadeDuration * 0.5f));
    }
}