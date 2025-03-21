using UnityEngine;

public class UI_Settings : MonoBehaviour
{
    // sound volume? screen size? etc.

    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}