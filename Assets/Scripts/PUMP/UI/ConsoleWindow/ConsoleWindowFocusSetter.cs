using UnityEngine;
using UnityEngine.EventSystems;

public class ConsoleWindowFocusSetter : MonoBehaviour, IPointerClickHandler
{
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        ConsoleWindow.SetFocus(true);
    }
}
