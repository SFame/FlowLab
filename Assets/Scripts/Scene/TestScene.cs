using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene : MonoBehaviour
{
    public void OnClickMoveButton()
    {
        SceneManager.LoadScene("PuzzleSelectScene");
    }
}
