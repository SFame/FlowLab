using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonEvent 
{
    public void OnGuideButton()
    {

    }
    public void OnMiniMapButton()
    {

    }
    public void OnOptionButton()
    {

    }
    public void OnExitButton()
    {
        GlobalEventManager.OnGameExitEvent();
        SceneManager.LoadSceneAsync("1.MainMenu");
    }
}
