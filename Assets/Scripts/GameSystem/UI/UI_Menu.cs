using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Menu : MonoBehaviour
{
    //buttons
    [SerializeField]
    private GameObject puzzleButton;
    [SerializeField]
    private GameObject settingsButton;
    [SerializeField]
    private GameObject exitButton;

    [SerializeField]
    private GameObject settingsMenu;
    
    public void OnPuzzleButtonClicked()
    {
        SceneManager.LoadScene("PuzzleSelectScene");
    }
    public void OnSettingsButtonClicked()
    {
        settingsMenu.SetActive(true);
    }
    public void OnExitButtonClicked()
    {
        Application.Quit();
    }
}
