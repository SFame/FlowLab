#if MSBDEBUG
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MouseSideButtons.UI
{
    public class DebugWindow : EditorWindow
    {
        public void CreateGUI()
        {
            rootVisualElement.style.paddingTop = 3;
            rootVisualElement.style.paddingBottom = 3;
            rootVisualElement.style.paddingLeft = 3;
            rootVisualElement.style.paddingRight = 3;

            // Debug Dump button
            Button btnDebugDump = new Button();
            btnDebugDump.text = "Debug Dump";
            btnDebugDump.clickable.clicked += () =>
            {
                DebugDump();
            };
            rootVisualElement.Add(btnDebugDump);
        }

        public static void DebugDump()
        {
            Debug.Log("v--- DEBUG DUMP ---v");
            Debug.Log($"[+] DirectoryTracker.directoryHistory:");
            for (int i = DirectoryTracker.lastDirIndex; i >= 0; i--)
            {
                if (DirectoryTracker.currentDirIndex == i)
                {
                    Debug.Log($"->\t[{i}] {DirectoryTracker.directoryHistory[i]}");
                }
                else
                {
                    Debug.Log($"\t[{i}] {DirectoryTracker.directoryHistory[i]}");
                }
            }
            Debug.Log("^--- DEBUG DUMP ---^");
        }

        [MenuItem("Tools/Mouse Side Buttons/Debug", false, 3)]
        public static void ShowDebug()
        {
            DebugWindow wnd = (DebugWindow)EditorWindow.GetWindow(typeof(DebugWindow), false, "MSB Debug");
        }
    }
}
#endif