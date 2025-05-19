#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using MouseSideButtons.Data;

namespace MouseSideButtons.UI
{
    [InitializeOnLoad]
    public class HistoryWindow : EditorWindow
    {
        // Properties
        public static ScrollView scrollView;

        // Setup
        static HistoryWindow()
        {
            // Some Unity versions complain about doing this in InitializeOnLoad
            try {
                scrollView = new ScrollView();
            } catch {
                return;
            }
        }

        public void OnEnable()
        {
            scrollView = new ScrollView();
        }

        // History Window
        public void CreateGUI()
        {
            // Clear History button
            Button btnClearHistory = new Button();
            btnClearHistory.text = "Clear History";
            btnClearHistory.tooltip = "Clears directory history";
            btnClearHistory.style.width = 165;
            btnClearHistory.style.marginTop = 5;
            btnClearHistory.style.marginBottom = 5;
            btnClearHistory.clickable.clicked += () =>
            {
                DirectoryTracker.currentDirIndex = 0;
                DirectoryTracker.directoryHistory.Clear();
                DirectoryTracker.directoryHistory.Add(Reflector.GetActiveFolderPath());
                UpdateHistoryDisplay();
            };
            rootVisualElement.Add(btnClearHistory);

            // History label
            Label labelHistory = new Label("History (most recent on top):");
            labelHistory.style.marginLeft = 3;
            labelHistory.style.marginBottom = 5;
            rootVisualElement.Add(labelHistory);

            // History box
            Box historyBox = new Box();
            historyBox.style.flexGrow = 1;
            historyBox.style.borderTopWidth = 1;
            historyBox.style.borderBottomWidth = 1;
            historyBox.style.borderLeftWidth = 1;
            historyBox.style.borderRightWidth = 1;
            historyBox.style.borderTopColor = Color.gray;
            historyBox.style.borderBottomColor = Color.gray;
            historyBox.style.borderLeftColor = Color.gray;
            historyBox.style.borderRightColor = Color.gray;
            scrollView.style.paddingTop = 3;
            scrollView.style.paddingBottom = 3;
            scrollView.style.paddingLeft = 5;
            scrollView.style.paddingRight = 5;
            scrollView.style.flexGrow = 1;
            scrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            historyBox.Add(scrollView);
            rootVisualElement.Add(historyBox);

            UpdateHistoryDisplay();
        }

        public static void UpdateHistoryDisplay()
        {
            DirectoryTracker.SquashHistory();
            // Catch cases where GUI has not been opened yet so scrollView is null
            try {
                scrollView.Clear();
                foreach (Label link in MakeHistoryDisplayLabels())
                {
                    scrollView.Add(link);
                }
                FocusOnLabel();
            } catch {
                return;
            }
        }

        public static List<Label> MakeHistoryDisplayLabels()
        {
            List<Label> history = new List<Label>();
            for (int i = DirectoryTracker.lastDirIndex; i >= 0; i--)
            {
                if (i == DirectoryTracker.currentDirIndex)
                    history.Add(MakeLink(DirectoryTracker.directoryHistory[i], i, active: true));
                else
                    history.Add(MakeLink(DirectoryTracker.directoryHistory[i], i));
            }
            return history;
        }

        // Clickable label
        private static Label MakeLink(string destination, int index, bool active = false)
        {
            Label dirLink = new Label(destination);
            dirLink.userData = index;
            dirLink.RegisterCallback<ClickEvent>(evt => LabelClicked(evt.target as Label));
            dirLink.style.marginTop = 2;
            dirLink.style.marginBottom = 2;
            dirLink.tooltip = $"{destination}";
            if (active)
            {
                dirLink.style.color = Color.black;
                dirLink.style.backgroundColor = Settings.instance.HIGHLIGHT_COLOR;
            }
            return dirLink;
        }

        private static void LabelClicked(Label clickedLabel)
        {
            if (clickedLabel != null && clickedLabel.userData is int index)
            {
                Navigator.GoToDirectory(DirectoryTracker.directoryHistory[index]);
                DirectoryTracker.currentDirIndex = index;
                UpdateHistoryDisplay();
            }
        }

        public static void FocusOnLabel()
        {
            var target = scrollView.contentContainer.Children()
                            .OfType<Label>()
                            .FirstOrDefault(label => label.resolvedStyle.backgroundColor == Settings.instance.HIGHLIGHT_COLOR);
            float offset = 0;
            foreach (var child in scrollView.contentContainer.Children())
            {
                if (child == target)
                {
                    break;
                }
                offset += child.resolvedStyle.height;
            }
            float centeringOffset = (scrollView.resolvedStyle.height - target.resolvedStyle.height) / 2;
            offset = Mathf.Max(0, offset - centeringOffset);
            scrollView.scrollOffset = new Vector2(0, offset);
        }
    }
}
#endif