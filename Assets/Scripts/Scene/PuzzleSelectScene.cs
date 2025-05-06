using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PuzzleSelectScene : MonoBehaviour
{

    void Start()
    {

    }

    public void OnClickExitButton()
    {
        SceneManager.LoadScene("TestScene");
    }

}
