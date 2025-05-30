using DG.Tweening;
using TMPro;
using UnityEngine;

public class StateDisplay : MonoBehaviour
{
    #region Interface
    public static void Render(Transition state, Vector2 worldPos, Canvas rootCanvas)
    {
        Instance.InternalRender(state, worldPos, rootCanvas.transform);
    }

    public static void Update(Transition state)
    {
        Instance.InternalUpdate(state);
    }

    public static void Clear()
    {
        Instance.InternalClear();
    }
    #endregion

    private const string PREFAB_PATH = "PUMP/Prefab/Other/StateDisplay";
    private const float FADE_DURATION = 0.2f;
    private static GameObject _prefab;
    private static StateDisplay _instance;

    private static GameObject Prefab => _prefab ??= Resources.Load<GameObject>(PREFAB_PATH);

    private static StateDisplay Instance
    {
        get
        {
            if (_instance == null || _instance.gameObject == null)
            {
                GameObject go = Instantiate(Prefab);
                go.SetActive(false);
                _instance = go.GetComponent<StateDisplay>();
            }

            return _instance;
        }
    }

    #region On Inspector
    [SerializeField] private TextMeshProUGUI m_StateText;
    [SerializeField] private RectTransform m_Rect;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private float m_StringTypeWidth = 150f;
    [SerializeField] private float m_OtherTypeWidth = 100f;
    #endregion

    private void InternalRender(Transition transition, Vector2 worldPos, Transform parent)
    {
        m_Rect.SetParent(parent);
        gameObject.SetActive(true);
        InternalUpdate(transition);
        SetPosition(worldPos);
        m_CanvasGroup.DOKill();
        m_CanvasGroup.alpha = 1f;
    }

    private void InternalUpdate(Transition transition)
    {
        float sizeDeltaX = transition.Type == TransitionType.String ? m_StringTypeWidth : m_OtherTypeWidth;
        string text = MakeText(transition);
        m_StateText.text = text;
        m_Rect.sizeDelta = new Vector2(sizeDeltaX, m_StateText.GetPreferredValues().y);
    }

    private void InternalClear()
    {
        m_CanvasGroup.DOKill();
        m_CanvasGroup.DOFade(0f, FADE_DURATION).OnComplete(() =>
        {
            m_StateText.text = string.Empty;
            m_Rect.SetParent(null);
            gameObject.SetActive(false);
        });
    }

    private void SetPosition(Vector2 position)
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 contextSize = m_Rect.sizeDelta;

        float xPos = position.x;
        if (xPos - contextSize.x < 0)
            xPos = contextSize.x;
        if (xPos > screenSize.x)
            xPos = screenSize.x;

        float yPos = position.y;
        if (yPos - contextSize.y < 0)
            yPos = contextSize.y;
        if (yPos > screenSize.y)
            yPos = screenSize.y;

        m_Rect.position = new Vector2(xPos, yPos);
    }

    private static string MakeText(Transition transition)
    {
        string typeText = $"<<color={transition.Type.GetColorHexCodeString(true)}><b>{transition.Type.ToString()}</b></color>>";
        string valueText = transition.GetValueString();
        valueText = transition is { Type: TransitionType.String, IsNull: false } ? $"\"<noparse>{valueText}</noparse>\"" : valueText;
        return typeText + "\n" + valueText;
    }
}