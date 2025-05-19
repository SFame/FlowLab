#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MouseSideButtons.Data
{
    [FilePath("ProjectSettings/Packages/MouseSideButtons/Settings.yml", FilePathAttribute.Location.ProjectFolder)]
    public class Settings : ScriptableSingleton<Settings>
    {
        [SerializeField]
        public int MAX_HISTORY_SIZE = 20;

        [SerializeField]
        public Color HIGHLIGHT_COLOR = new Color(.3f, .7f, 1f, 1f);

        public void Save()
        {
            Save(true);
        }
    }
}
#endif