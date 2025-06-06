using System;
using TMPro;
using UnityEngine;

public class SignalDetectorSupport : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_Input;
    
    public float Value
    {
        get
        {
            if (float.TryParse(m_Input.text, out float result))
            {
                return result;
            }

            Debug.LogError("Parse fail");
            return 0f;
        }

        set => m_Input.text = value.ToString();
    }

    public event Action<float> OnEndEdit;

    public void Initialize()
    {
        m_Input.onEndEdit.AddListener(value =>
        {
            if (float.TryParse(value, out float f_value))
            {
                OnEndEdit?.Invoke(f_value);
            }
        });
    }
}