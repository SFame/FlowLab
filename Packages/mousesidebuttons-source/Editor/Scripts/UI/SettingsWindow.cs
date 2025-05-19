#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using MouseSideButtons.Data;

namespace MouseSideButtons.UI
{
    [InitializeOnLoad]
    public class SettingsWindow : EditorWindow
    {
        // Settings Window
        public void CreateGUI()
        {
            rootVisualElement.style.paddingTop = 3;
            rootVisualElement.style.paddingBottom = 3;
            rootVisualElement.style.paddingLeft = 3;
            rootVisualElement.style.paddingRight = 3;

            // History size config container
            VisualElement historyRowContainer = new VisualElement();
            historyRowContainer.style.flexDirection = FlexDirection.Row;
            historyRowContainer.style.marginBottom = 5;
            // History size input box
            IntegerField historySizeField = new IntegerField("History Size");
            historySizeField.style.width = 125;
            historySizeField.labelElement.style.marginRight = -30;
            historySizeField.value = Settings.instance.MAX_HISTORY_SIZE;
            historyRowContainer.Add(historySizeField);
            // History size set button
            Button btnSetHistorySize = new Button();
            btnSetHistorySize.text = "Set";
            btnSetHistorySize.clickable.clicked += () =>
            {
                SetHistorySize(historySizeField.value);
            };
            historyRowContainer.Add(btnSetHistorySize);
            rootVisualElement.Add(historyRowContainer);

            // Highlight color
            Label colorLabel = new Label();
            colorLabel.text = "Highlight Color";
            colorLabel.style.marginLeft = 3;
            colorLabel.tooltip = "The Highlight Color is used to indiciate the current directory in the History window.";
            colorLabel.style.whiteSpace = WhiteSpace.Normal;
            // Used to keep the color picker and button side by side
            VisualElement colorRowContainer = new VisualElement();
            colorRowContainer.style.flexDirection = FlexDirection.Row;
            colorRowContainer.style.marginTop = 5;
            ColorField highlightColorSelector = new ColorField();
            highlightColorSelector.value = Settings.instance.HIGHLIGHT_COLOR;
            highlightColorSelector.style.width = 125;
            Button btnSetHighlightColor = new Button();
            btnSetHighlightColor.text = "Set";
            btnSetHighlightColor.clicked += () =>
            {
                SetHighlightColor(highlightColorSelector.value);
            };
            colorRowContainer.Add(highlightColorSelector);
            colorRowContainer.Add(btnSetHighlightColor);
            rootVisualElement.Add(colorLabel);
            rootVisualElement.Add(colorRowContainer);

            // Restore defaults
            Button btnRestoreDefaults = new Button();
            btnRestoreDefaults.text = "Restore Defaults";
            btnRestoreDefaults.style.marginTop = 10;
            btnRestoreDefaults.clickable.clicked += () =>
            {
                if (SetHistorySize(20))
                {
                    historySizeField.value = 20;
                }
                SetHighlightColor(new UnityEngine.Color(.3f, .7f, 1f, 1f));
                highlightColorSelector.value = new UnityEngine.Color(.3f, .7f, 1f, 1f);
            };
            rootVisualElement.Add(btnRestoreDefaults);
        }

        private static bool SetHistorySize(int newHistorySize)
        {
            // Warn if smaller than current directory index
            if (newHistorySize < DirectoryTracker.directoryHistory.Count - DirectoryTracker.currentDirIndex)
            {
                EditorUtility.DisplayDialog(
                    "Mouse Side Buttons",
                    "Cannot set History Size lower than current position in history.",
                    "OK"
                );
                return false;
            }
            else
            {
                Settings.instance.MAX_HISTORY_SIZE = newHistorySize;
                Settings.instance.Save();
                // Resize if smaller than directory history count
                if (newHistorySize < DirectoryTracker.directoryHistory.Count)
                {
                    int removed = DirectoryTracker.directoryHistory.Count - newHistorySize;
                    DirectoryTracker.TruncateDirectoryHistory(removed);
                }
                HistoryWindow.UpdateHistoryDisplay();
                return true;
            }
        }

        private static void SetHighlightColor(UnityEngine.Color newHighlightColor)
        {
            Settings.instance.HIGHLIGHT_COLOR = newHighlightColor;
            Settings.instance.Save();
            HistoryWindow.UpdateHistoryDisplay();
        }
    }
}
#endif