#if UNITY_EDITOR
using System;
using UnityEditor;
using MouseSideButtons.Data;

namespace MouseSideButtons
{
    [InitializeOnLoad]
    public static class Core
    {
        // Properties
        private static string currentDirectory;
        private static string prevDirectory;
        public static bool skipThisFrame = true; // Skip first frame since the editor always starts in Assets/ before focusing the saved directory
        public static bool wasPopped = true; // Keep to true by default to prevent domain reload initial directory change from triggering event
        public static Action<string> DirectoryChanged;

        static Core()
        {
            EditorApplication.quitting += () => { Persistence.instance.Save(); };
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            TabTracker.OnEditorUpdate();

            if (skipThisFrame)
            {
                skipThisFrame = false;
                return;
            }

            // Handle new Project tab opening, which will cause GetActiveFolderPath to
            // throw a NullReferenceException while the ProjectBrowser loads
            try
            {
                currentDirectory = Reflector.GetActiveFolderPath();
            }
            catch
            {
                // Essentially treat this like a new editor session
                skipThisFrame = true;
                wasPopped = true;
                return;
            }

            if (prevDirectory != currentDirectory)
            {
                DirectoryChanged?.Invoke(currentDirectory);
                wasPopped = false;
            }

            prevDirectory = currentDirectory;
        }
    }
}
#endif