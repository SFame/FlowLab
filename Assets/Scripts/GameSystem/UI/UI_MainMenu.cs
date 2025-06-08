using UnityEngine;
using Utils;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject m_MenuObject;
    [SerializeField] private GameObject m_codexPrefab;

    [SerializeField] private GameObject m_SettingUIPrefab;
    public static UI_MainMenu Instance { get; private set; }

    private CodexPalette _codexPalette;
    private UI_Settings _settingsUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        if (_codexPalette == null)
        {
            GameObject temp = Instantiate(m_codexPrefab, transform.parent, false);
            temp.GetComponent<RectTransform>().SetOffset(Vector2.zero, Vector2.zero);
            _codexPalette = temp.GetComponent<CodexPalette>();
            temp.SetActive(false);
        }

        if (_settingsUI == null)
        {
            GameObject temp = Instantiate(m_SettingUIPrefab, transform.parent, false);
            _settingsUI = temp.GetComponent<UI_Settings>();
            temp.SetActive(false);
        }
    }

    public void Open()
    {
        m_MenuObject.SetActive(true);
    }

    public void OpenCodex()
    {
        _codexPalette.Open();
    }

    public void OnClickStart()
    {
        if (m_MenuObject.activeSelf)
        {
            m_MenuObject.SetActive(false);
        }
        else
        {
            m_MenuObject.SetActive(true);
        }
    }

    public void OnClickSettings()
    {
        if (_settingsUI.gameObject.activeSelf)
        {
            _settingsUI.gameObject.SetActive(true);
        }
        else
        {
            _settingsUI.gameObject.SetActive(true);
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