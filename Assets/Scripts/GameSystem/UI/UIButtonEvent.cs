using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonEvent : MonoBehaviour
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
        SceneManager.LoadSceneAsync("3.StageScene_UI");
    }
    public void OnClickHowToPlay()
    {
        CutsceneDisplay.ShowCutscene("HowToPlay");
    }
}
