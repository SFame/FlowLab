#if UNITY_EDITOR
using System.Collections.Generic;
using MouseSideButtons.Data;
using MouseSideButtons.UI;
using UnityEditor;

namespace MouseSideButtons
{
    public class DirectoryTracker : AssetPostprocessor
    {
        public static List<string> directoryHistory
        {
            get { return Persistence.instance.directoryHistory; }
            set { Persistence.instance.directoryHistory = value; }
        }
        public static int currentDirIndex
        {
            get { return Persistence.instance.currentDirIndex; }
            set { Persistence.instance.currentDirIndex = value; }
        }

        public static int lastDirIndex
        {
            get { return Persistence.instance.directoryHistory.Count - 1; }
        }
        public static string currentDir
        {
            get { return Persistence.instance.directoryHistory[Persistence.instance.currentDirIndex]; }
        }
        public static bool atStart
        {
            get { return Persistence.instance.currentDirIndex == lastDirIndex; }
        }
        public static bool atEnd
        {
            get { return Persistence.instance.currentDirIndex == 0; }
        }

        static DirectoryTracker()
        {
            if (directoryHistory.Count == 0)
            {
                currentDirIndex = 0;
                directoryHistory.Add(Reflector.GetActiveFolderPath());
            }
            Core.DirectoryChanged += OnDirectoryChanged;
        }

        public static void OnDirectoryChanged(string currentDir)
        {
            if (!Core.wasPopped)
            {
                UpdateDirectoryHistory(currentDir);
                HistoryWindow.UpdateHistoryDisplay();
            }
        }

        public static void UpdateDirectoryHistory(string currentDir)
        {
            if (currentDirIndex < lastDirIndex)
            {
                directoryHistory.RemoveRange(currentDirIndex + 1, lastDirIndex - currentDirIndex);
            }

            directoryHistory.Add(currentDir);
            currentDirIndex++;

            if (directoryHistory.Count > Settings.instance.MAX_HISTORY_SIZE)
            {
                directoryHistory.RemoveAt(0);
                currentDirIndex--;
            }

            if (currentDirIndex > lastDirIndex)
            {
                currentDirIndex = lastDirIndex;
            }
        }

        // Handle history size decreases and move indices appropriately
        public static void TruncateDirectoryHistory(int removed)
        {
            directoryHistory.RemoveRange(0, directoryHistory.Count - Settings.instance.MAX_HISTORY_SIZE);
            if (currentDirIndex <= removed)
            {
                currentDirIndex = 0;
            }
            else
            {
                currentDirIndex -= removed;
            }
        }

        // Remove adjacent duplicates in cases where folders are deleted
        public static void SquashHistory()
        {
            for (int i = lastDirIndex; i > 0; i--)
            {
                if (directoryHistory[i] == directoryHistory[i - 1])
                {
                    directoryHistory.RemoveAt(i);
                    if (DirectoryTracker.currentDirIndex >= i)
                    {
                        DirectoryTracker.currentDirIndex--;
                    }
                }
            }
        }

        // Handle folder deletions and renames, adjust history
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Handle deleted folders
            if (deletedAssets.Length != 0)
            {
                for (int i = 0; i < deletedAssets.Length; i++)
                {
                    // TODO add IsFolder check
                    // TODO Check if deleted folder is current folder
                    for (int j = 0; j <= lastDirIndex; j++)
                    {
                        if (directoryHistory[j] == deletedAssets[i])
                        {
                            if (currentDirIndex > j)
                            {
                                currentDirIndex--;
                            }
                            directoryHistory.RemoveAt(j);
                        }
                    }
                }
                HistoryWindow.UpdateHistoryDisplay();
            }

            // Handle renamed folders
            if (movedAssets.Length != 0)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    // TODO add IsFolder check
                    for (int j = 0; j <= lastDirIndex; j++)
                    {
                        if (directoryHistory[j] == movedFromAssetPaths[i])
                        {
                            directoryHistory[j] = movedAssets[i];
                        }
                    }
                }
                HistoryWindow.UpdateHistoryDisplay();
            }
        }
    }
}
#endif