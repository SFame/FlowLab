using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class BinaryDisplaySupport : MonoBehaviour
{
    [SerializeField] private Slider m_Slider;
    [SerializeField] private SegmentController[] m_SegmentControllers;

    /// <summary>
    /// 사용자의 Handle 조작으로 인한 Value Change
    /// </summary>
    public event Action<int> OnValueChanged;

    private bool _isInitialized = false;
    private bool _blockEvent = false;


    public void Initialize()
    {
        if (_isInitialized) 
            return;
        
        _isInitialized = true;

        m_Slider.onValueChanged.AddListener(InvokeOnValueChange);
        UpdateDisplay(0);
    }

    public void SetSliderValue(int value)
    {
        _blockEvent = true;
        m_Slider.value = value;
        _blockEvent = false;
    }

    public void UpdateBinaryDisplay(bool[] states)
    {
        UpdateDisplay(ConvertToDecimal(states));
    }

    private void InvokeOnValueChange(float value)
    {
        if (_blockEvent)
            return;

        OnValueChanged?.Invoke((int)value);
    }

    private int ConvertToDecimal(bool[] binaryArray)
    {
        int result = 0;
        for (int i = 0; i < binaryArray.Length; i++)
        {
            if (binaryArray[i])
            {
                result += 1 << i;
            }
        }

        return result;
    }

    private void UpdateDisplay(int value)
    {
        int[] sums = new int[4];
        value.ConvertToDigitArray(sums);

        m_SegmentControllers[0].SetDisplay(sums[0]);
        m_SegmentControllers[1].SetDisplay(sums[1]);
        m_SegmentControllers[2].SetDisplay(sums[2]);
        m_SegmentControllers[3].SetDisplay(sums[3]);
    }
}