using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BinaryDisplaySupport : MonoBehaviour
{
    [SerializeField] public TMP_Dropdown m_Dropdown;

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
        //UpdateDisplay
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

    //private void UpdateDisplay
    //{
        
    //}


}

