using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConsoleWindowTMP_InpufField : TMP_InputField
{
    private const KeyCode UP_KEY_CODE = KeyCode.UpArrow;
    private const KeyCode DOWN_KEY_CODE = KeyCode.DownArrow;

    public event Action OnKeyUp;
    public event Action OnKeyDown;

    public override void OnSubmit(BaseEventData eventData) { }

    public override void OnUpdateSelected(BaseEventData eventData)
    {
        if (!isFocused)
        {
            base.OnUpdateSelected(eventData);
            return;
        }

        if (Input.GetKeyDown(UP_KEY_CODE))
        {
            OnKeyUp?.Invoke();
            return;
        }

        if (Input.GetKeyDown(DOWN_KEY_CODE))
        {
            OnKeyDown?.Invoke();
            return;
        }

        base.OnUpdateSelected(eventData);
    }
}