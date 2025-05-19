#if UNITY_EDITOR
using UnityEditor;

namespace MouseSideButtons.UI
{
    public static class Menu
    {
        [MenuItem("Tools/Mouse Side Buttons/History", false, 1)]
        public static void ShowHistory()
        {
            HistoryWindow wnd = (HistoryWindow)EditorWindow.GetWindow(typeof(HistoryWindow), false, "MSB History");
        }

        [MenuItem("Tools/Mouse Side Buttons/Settings", false, 2)]
        public static void ShowSettings()
        {
            SettingsWindow wnd = (SettingsWindow)EditorWindow.GetWindow(typeof(SettingsWindow), false, "MSB Settings");
        }

        [MenuItem("Tools/Mouse Side Buttons/Move Backward _M3")]
        public static void MoveBackward()
        {
            Navigator.GoDirection(Navigator.Direction.Backward);
        }

        [MenuItem("Tools/Mouse Side Buttons/Move Forward _M4")]
        public static void MoveForward()
        {
            Navigator.GoDirection(Navigator.Direction.Forward);
        }
    }
}
#endif