using UnityEngine;

public class UI_MenuApproachObject : MonoBehaviour
{
    public void OpenMenu()
    {
        UI_MainMenu.Instance?.Open();
    }    
}