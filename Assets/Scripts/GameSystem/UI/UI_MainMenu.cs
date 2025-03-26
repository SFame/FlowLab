using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject SettingUI;
    [SerializeField]
    private GameObject StartUI;
    [SerializeField]
    private GameObject MainMenuUI;

    public void OnClickStart()
    {
        if (StartUI.activeSelf)
        {
            StartUI.SetActive(false);
            MainMenuUI.SetActive(true);
        }
        else
        {
            StartUI.SetActive(true);
            MainMenuUI.SetActive(false);
        }
    }
    public void OnClickSandboxMode()
    {
        //SceneManager.LoadScene("SandboxMode");
        SceneManager.LoadScene("2.FieldScene");
    }
    public void OnClickSettings()
    {
        if (SettingUI.activeSelf)
        {
            SettingUI.SetActive(false);
        }
        else
        {
            SettingUI.SetActive(true);
        }
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