#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MouseSideButtons
{
    [InitializeOnLoad]
    public static class Navigator
    {
        public enum Direction
        {
            Forward,
            Backward
        }

        // This is used to fix a case where a shortcut double-fires in Play Mode with the Game window focused
        // TODO See which versions are affected
        private static bool shouldIgnoreNextShortcut = false;

        public static void GoDirection(Direction direction)
        {
            if (!ShouldProcessShortcut())
            {
                return;
            }

            // Backward
            if (direction == Direction.Backward)
            {
                if (!DirectoryTracker.atEnd)
                {
                    DirectoryTracker.currentDirIndex--;
                    Navigator.GoToDirectory(DirectoryTracker.currentDir);
                }
                else
                {
                    Debug.Log("Reached the end of the directory history.");
                }
            }

            // Forward
            else
            {
                if (!DirectoryTracker.atStart)
                {
                    DirectoryTracker.currentDirIndex++;
                    Navigator.GoToDirectory(DirectoryTracker.currentDir);
                }
                else
                {
                    Debug.Log("Reached the start of the directory history.");
                }
            }
        }

        public static void GoToDirectory(string destination)
        {
            int destinationID = Reflector.GetFolderInstanceID(destination);
            if (destinationID < 0)
            {
                // TODO Handle case where this is reached when called from GoDirection, since the index will have changed
                // and we will need to set it back
                Debug.Log($"[ERROR] Destination ID < 0; Directory '{destination}' does not exist (may have been deleted)");
                return;
            }
            UI.HistoryWindow.UpdateHistoryDisplay();
            Reflector.SetFolderSelection(destinationID);
            Core.wasPopped = true;
        }

        private static bool ShouldProcessShortcut()
        {
            if (shouldIgnoreNextShortcut == true)
            {
                shouldIgnoreNextShortcut = false;
                return false;
            }
            if (EditorApplication.isPlaying && EditorWindow.focusedWindow.titleContent.text == "Game")
            {
                shouldIgnoreNextShortcut = true;
            }
            if (!Reflector.IsTwoColumns())
            {
                Debug.Log("MSB only works in Two Columns Layout");
                return false;
            }
            return true;
        }
    }
}
#endif