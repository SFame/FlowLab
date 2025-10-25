using UnityEngine;

public class ConsoleWindowActiveObject : MonoBehaviour
{
    public void OpenToggle() => ConsoleWindow.IsOpen = !ConsoleWindow.IsOpen;

    public void Open() => ConsoleWindow.IsOpen = true;

    public void Close() => ConsoleWindow.IsOpen = false;
}