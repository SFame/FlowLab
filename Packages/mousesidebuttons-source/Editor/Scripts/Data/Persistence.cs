#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MouseSideButtons.Data
{
    [FilePath("ProjectSettings/Packages/MouseSideButtons/Persistence.yml", FilePathAttribute.Location.ProjectFolder)]
    public class Persistence : ScriptableSingleton<Persistence>
    {
        [SerializeField]
        public List<string> directoryHistory = new List<string>();

        [SerializeField]
        public int currentDirIndex = 0;

        public void Save()
        {
            Save(true);
        }
    }
}
#endif