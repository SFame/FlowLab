using UnityEngine;
using Utils;

public class UI_TMPTextResizeFiltter : MonoBehaviour
{
    [SerializeField]private TMPro.TextMeshProUGUI m_TextMeshProUGUI;
    [SerializeField]private RectTransform m_TMPRectTransform;
    public TMPro.TextMeshProUGUI TextMeshPro
    {
        get
        {
            if(m_TextMeshProUGUI == null && transform.GetComponentInChildren<TMPro.TextMeshPro>())
            {
                m_TextMeshProUGUI = transform.GetComponent<TMPro.TextMeshProUGUI>();
                m_TMPRectTransform = m_TextMeshProUGUI.rectTransform;
            }

            return m_TextMeshProUGUI;
        }
    }
    public RectTransform TMPRectTransform { get { return m_TMPRectTransform; } }

    [SerializeField]private RectTransform m_RectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if(m_RectTransform == null)
            {
                m_RectTransform = GetComponent<RectTransform>();
            }
            return m_RectTransform;
        }
    }

    private float m_PreferredHeight;
    public float PreferredHeight { get { return m_PreferredHeight; } }

    private void SetHeight()
    {
        if (TextMeshPro == null)
            return;

        m_PreferredHeight = TextMeshPro.preferredHeight;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, m_PreferredHeight);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void OnEnable()
    {
        SetHeight();
    }
    private void Start()
    {
        SetHeight();
    }
    private void Update()
    {
        if (PreferredHeight != TextMeshPro.preferredHeight)
            SetHeight();
    }

}
