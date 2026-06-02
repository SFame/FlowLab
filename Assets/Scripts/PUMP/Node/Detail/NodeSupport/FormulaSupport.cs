using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FormulaSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_Text;
    [SerializeField] private TextMeshProUGUI m_ErrorText;
    [SerializeField] private float m_ErrorShowDuration = 4f;

    [SerializeField] private Transform m_TextArea;

    private readonly object _inputManagerBlockerObject = new();

    private List<RectTransform> _resetOffsetRectGroup = new();

    private bool _initialized = false;

    public string Text
    {
        get => m_Text.text;
        set => m_Text.text = value;
    }

    public void Initialize(string ininText, Action<string> onTextSubmitted)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        foreach (Transform transform in m_TextArea)
        {
            _resetOffsetRectGroup.Add((RectTransform)transform);
        }

        m_Text.text = ininText;

        m_Text.onValueChanged.AddListener(_=>
        {
            foreach (RectTransform rect in _resetOffsetRectGroup)
            {
                rect.offsetMin = new Vector2(0, 0);
                rect.offsetMax = new Vector2(0, 0);
            }
        });

        m_Text.onEndEdit.AddListener(value => onTextSubmitted?.Invoke(value));
        m_Text.onSelect.AddListener(_ => InputManager.AddBlocker(_inputManagerBlockerObject));
        m_Text.onDeselect.AddListener(_ => InputManager.RemoveBlocker(_inputManagerBlockerObject));
    }

    public void ShowError(string errorText)
    {
        InternalShowError(errorText).Forget();
    }

    private async UniTaskVoid InternalShowError(string errorText)
    {
        m_ErrorText.text = errorText;

        await UniTask.WaitForSeconds(m_ErrorShowDuration);

        m_ErrorText.text = string.Empty;
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