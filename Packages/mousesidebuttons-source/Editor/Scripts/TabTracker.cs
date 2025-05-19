#if UNITY_EDITOR
using UnityEditor;

namespace MouseSideButtons
{
    public class TabTracker
    {
        private static EditorWindow currentProjectBrowser;
        private static EditorWindow lastProjectBrowser;

        public static void OnEditorUpdate()
        {
            currentProjectBrowser = GetFocusedProjectTab();
            // Check for Project tab change
            if (lastProjectBrowser != currentProjectBrowser)
            {
                Core.skipThisFrame = true;
                Core.wasPopped = true;
            }
            lastProjectBrowser = currentProjectBrowser;
        }

        public static EditorWindow GetFocusedProjectTab()
        {
            foreach (EditorWindow window in Reflector.GetAllProjectBrowsers())
            {
                if (window.hasFocus)
                {
                    return window;
                }
            }
            return null;
        }
    }
}
#endif