using TMPro;
using UnityEngine;

public class DisplaySupport : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;

    private string _text = "";

    public void SetText(Transition transition)
    {
        if (transition.IsNull)
        {
            m_Text.text = string.Empty;
            return;
        }

        _text = transition.GetValueString();
        m_Text.text = _text;
    }

    public string GetText() => _text;
}