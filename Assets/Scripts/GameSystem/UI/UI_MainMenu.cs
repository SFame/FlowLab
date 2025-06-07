using UnityEngine;
using UnityEngine.UI;
using Utils;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField]private GameObject SettingUI;
    [SerializeField]private GameObject StartUI;
    [SerializeField]private Button CodexUI;
    [SerializeField] private CodexPalette m_codexPalette;
    [SerializeField] private GameObject m_codexPrefab;

    [SerializeField] private UI_Settings m_SettingsUI;
    [SerializeField] private Button QuizButton;
    [SerializeField] private GameObject m_SettingUIPrefab;
    public static UI_MainMenu Instance { get; private set; }
    private void Awake()
    {
        // 싱글톤 처리
        if (Instance == null)
        {
            Instance = this;
        }

        if (m_codexPalette == null)
        {
            GameObject temp = Instantiate(m_codexPrefab);
            temp.transform.parent = transform.parent;
            temp.GetComponent<RectTransform>().SetOffset(Vector2.zero, Vector2.zero);
            m_codexPalette = temp.GetComponent<CodexPalette>();
            temp.SetActive(false);
        }

        if (m_SettingsUI == null)
        {
            GameObject temp = Instantiate(m_SettingUIPrefab);
            temp.transform.parent = transform.parent;
            m_SettingsUI = temp.GetComponent<UI_Settings>();
            temp.SetActive(false);
        }
    }
    private void Start()
    {
        CodexUI.onClick.AddListener(m_codexPalette.Open);
    }
    public CodexPalette GetCodexPalette()
    {
        if (m_codexPalette == null)
        {
            GameObject temp = Instantiate(m_codexPrefab);
            temp.transform.parent = transform.parent;
            m_codexPalette = temp.GetComponent<CodexPalette>();
            temp.SetActive(false);
        }
        return m_codexPalette;
    }
    public void GetStartUIOpen()
    {
        StartUI.SetActive(true);
    }
    public void OnClickStart()
    {
        if (StartUI.activeSelf)
        {
            StartUI.SetActive(false);
        }
        else
        {
            StartUI.SetActive(true);
        }
    }
    public void OnClickSettings()
    {
        if (SettingUI.activeSelf)
        {
            m_SettingsUI.gameObject.SetActive(true);
        }
        else
        {
            SettingUI.SetActive(true);
        }
    }
    public void OnClickQuiz()
    {
        SceneManager.LoadScene("PuzzleSelectScene");
    }
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}