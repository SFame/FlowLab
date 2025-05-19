#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace MouseSideButtons
{
    [InitializeOnLoad]
    public class Reflector
    {
        // Assembly
        public static Assembly _asm;

        // Classes
        public static Type _typeProjectWindowUtil;
        public static Type _typeProjectBrowser;

        // Methods
        public static MethodInfo _methodGetActiveFolderPath;
        public static MethodInfo _methodGetFolderInstanceID;
        public static MethodInfo _methodSetFolderSelection;
        public static MethodInfo _methodGetSelectedPath;
        public static MethodInfo _methodIsTwoColumns;
        public static MethodInfo _methodGetAllProjectBrowsers;
        public static MethodInfo _methodIsProjectBrowserInitialized;
        public static MethodInfo _methodProjectBrowserInitialize;

        static Reflector()
        {
            // Set up reflections
            _asm = Assembly.GetAssembly(typeof(ProjectWindowUtil));
            _typeProjectWindowUtil = _asm.GetType("UnityEditor.ProjectWindowUtil");
            _typeProjectBrowser = _asm.GetType("UnityEditor.ProjectBrowser");
            _methodGetActiveFolderPath = _typeProjectWindowUtil.GetMethod("GetActiveFolderPath", BindingFlags.NonPublic | BindingFlags.Static);
            _methodGetFolderInstanceID = _typeProjectBrowser.GetMethod("GetFolderInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
            _methodSetFolderSelection = _typeProjectBrowser.GetMethod("SetFolderSelection", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int[]), typeof(bool) }, null);
            _methodGetSelectedPath = _typeProjectBrowser.GetMethod("GetSelectedPath", BindingFlags.NonPublic | BindingFlags.Static);
            _methodIsTwoColumns = _typeProjectBrowser.GetMethod("IsTwoColumns", BindingFlags.NonPublic | BindingFlags.Instance);
            _methodGetAllProjectBrowsers = _typeProjectBrowser.GetMethod("GetAllProjectBrowsers", BindingFlags.Public | BindingFlags.Static);
            _methodIsProjectBrowserInitialized = _typeProjectBrowser.GetMethod("Initialized", BindingFlags.Public | BindingFlags.Instance);
            _methodProjectBrowserInitialize = _typeProjectBrowser.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
        }

        // Helpers
        public static object GetLastBrowserInstance()
        {
            object lastBrowser = _typeProjectBrowser.GetField("s_LastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            if (lastBrowser is null)
            {
                lastBrowser = GetAllProjectBrowsers()[0];
            }
            if (!IsBrowserInitialized(lastBrowser))
            {
                BrowserInitialize(lastBrowser);
            }
            return lastBrowser;
        }
        public static string GetActiveFolderPath()
        {
            return (string)_methodGetActiveFolderPath.Invoke(null, new object[] { });
        }

        public static int GetFolderInstanceID(string destination)
        {
            return (int)_methodGetFolderInstanceID.Invoke(null, new object[] { destination });
        }

        public static void SetFolderSelection(int destinationID)
        {
            _methodSetFolderSelection.Invoke(GetLastBrowserInstance(), new object[] { new int[] { destinationID }, false });
        }

        public static bool IsTwoColumns()
        {
            return (bool)_methodIsTwoColumns.Invoke(GetLastBrowserInstance(), new object[] { });
        }

        public static bool IsBrowserInitialized(object browser)
        {
            return (bool)_methodIsProjectBrowserInitialized.Invoke(browser, new object[] { });
        }

        public static void BrowserInitialize(object browser)
        {
            _methodProjectBrowserInitialize.Invoke(browser, new object[] { });
        }

        public static IList GetAllProjectBrowsers()
        {
            return (IList)_methodGetAllProjectBrowsers.Invoke(null, new object[] { });
        }
    }
}
#endif