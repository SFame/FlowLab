using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class BinaryDisplaySupport : MonoBehaviour
{
    [SerializeField] public TMP_Dropdown m_Dropdown;
    [SerializeField] private SegmentController[] m_SegmentControllers;

    public event Action<int> OnValueChanged;

    private bool _isInitialized = false;

    private int sum = 0;

    public void Initialize()
    {
        if (_isInitialized) return;
        
        _isInitialized = true;
        m_Dropdown.onValueChanged.AddListener(value => OnValueChanged?.Invoke(value));
    }

    public void UpdateBinaryDisplay(bool[] states)
    {
        InputNumSum(states);
        UpdateDisplay();
    }

    private void InputNumSum(bool[] states)
    {
        sum = 0;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i])
            {
                sum += 1 << i;
            }
        }
    }

    private void UpdateDisplay()
    {
        int[] sums = new int[4];
        sum.ConvertToDigitArray(in sums);

        m_SegmentControllers[0].SetDisplay(sums[0]);
        m_SegmentControllers[1].SetDisplay(sums[1]);
        m_SegmentControllers[2].SetDisplay(sums[2]);
        m_SegmentControllers[3].SetDisplay(sums[3]);
    }

    

}

