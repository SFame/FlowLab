using TMPro;
using UnityEngine;

public class SwitchSupport : MonoBehaviour
{
    [SerializeField] private RectTransform m_ARect;
    [SerializeField] private RectTransform m_BRect;
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

    public void SetYPositions(float aY, float bY)
    {
        Vector3 aPos = m_ARect.localPosition;
        Vector3 bPos = m_BRect.localPosition;
        m_ARect.localPosition = new Vector3(aPos.x, aY, aPos.z);
        m_BRect.localPosition = new Vector3(bPos.x, bY, bPos.z);
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