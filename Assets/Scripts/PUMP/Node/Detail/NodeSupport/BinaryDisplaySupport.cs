using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BinaryDisplaySupport : MonoBehaviour
{
    [SerializeField] public TMP_Dropdown _dropdown;

    public event Action<int> OnValueChanged;

    private bool _isInitialized = false;
    public void Initialize()
    {
        if (_isInitialized) return;
        
        _isInitialized = true;
        _dropdown.onValueChanged.AddListener(value => OnValueChanged?.Invoke(value));
    }

    public void UpdateBinaryDisplay(bool[] states)
    {
        
    }

   
}
