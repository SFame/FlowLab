using TMPro;
using UnityEngine;

public class SwitchSupport : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_AText;
    [SerializeField] private TextMeshProUGUI m_BText;

    private bool _transB;
    private const string TRANS_STRING = ">";

    public bool TransB
    {
        get => _transB;
        set
        {
            _transB = value;
            SetTransText(_transB);
        }
    }

    private void SetTransText(bool isB)
    {
        if (isB)
        {
            m_AText.text = string.Empty;
            m_BText.text = TRANS_STRING;
            return;
        }

        m_BText.text = string.Empty;
        m_AText.text = TRANS_STRING;
    }
}