using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject SettingUI;
    [SerializeField]
    private GameObject StartUI;
    [SerializeField]
    private GameObject ExitUI;
    [SerializeField]
    private GameObject LoadGameUI;

    private void Start()
    {
        LoadGameUI.GetComponent<Button>().interactable = Utils.Serializer.LoadData<GameSaveData>("SaveData.bin") != default ? true : false;
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