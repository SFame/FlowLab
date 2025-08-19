using System;
using UnityEngine;
using UnityEngine.UI;

public class ClockGateSupport : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private UnityEngine.UI.InputField channelField;  // UGUI 고정
    [SerializeField] private UnityEngine.UI.InputField periodField;   // UGUI 고정
    [SerializeField] private Toggle emitNowIfAlignedToggle;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button alignNowButton;
    [SerializeField] private Button closeButton;

    private Action<int, int, bool> _onApply;
    private Action _onAlignNow;

    public void Open(
        int channel,
        int periodFrames,
        bool emitNowIfAligned,
        Action<int, int, bool> onApply,
        Action onAlignNow)
    {
        _onApply = onApply;
        _onAlignNow = onAlignNow;

        if (channelField != null) channelField.text = channel.ToString();
        if (periodField != null) periodField.text = periodFrames.ToString();
        if (emitNowIfAlignedToggle != null) emitNowIfAlignedToggle.isOn = emitNowIfAligned;
        if (panel != null) panel.SetActive(true);

        if (applyButton != null) applyButton.onClick.AddListener(OnApplyClick);
        if (alignNowButton != null) alignNowButton.onClick.AddListener(OnAlignNowClick);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnApplyClick()
    {
        string chText = (channelField != null) ? channelField.text : "0";
        string perText = (periodField != null) ? periodField.text : "1";

        int ch; if (!int.TryParse(chText, out ch)) ch = 0;
        int per; if (!int.TryParse(perText, out per)) per = 1;

        bool emitNow = (emitNowIfAlignedToggle != null) && emitNowIfAlignedToggle.isOn;

        _onApply?.Invoke(
            Mathf.Clamp(ch, 0, ChannelClock.MaxChannels - 1),
            Mathf.Max(1, per),
            emitNow
        );

        Close();
    }

    private void OnAlignNowClick()
    {
        _onAlignNow?.Invoke();
    }

    private void Close()
    {
        if (applyButton != null) applyButton.onClick.RemoveListener(OnApplyClick);
        if (alignNowButton != null) alignNowButton.onClick.RemoveListener(OnAlignNowClick);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (panel != null) panel.SetActive(false);
    }
}
