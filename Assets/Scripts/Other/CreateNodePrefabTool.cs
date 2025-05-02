#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Object = UnityEngine.Object;

public class CreateNodePrefabTool
{
    private const string PREFAB_PATH = "Assets/Resources/PUMP/Prefab/NODE.prefab";
    private const string MENU_PATH = "Tools/Node (PUMP)";
    private const string DEFAULT_VARIANT_NAME = "New Node";
    private const string DEFAULT_VARIANT_DIRECTORY = "Assets/Resources/PUMP/Prefab/Node";

    [MenuItem(MENU_PATH, false, 100)]
    private static void CreateVariant()
    {
        try
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Error", $"Could not find prefab at path: {PREFAB_PATH}", "OK");
                return;
            }

            if (!Directory.Exists(DEFAULT_VARIANT_DIRECTORY))
            {
                try
                {
                    Directory.CreateDirectory(DEFAULT_VARIANT_DIRECTORY);
                    AssetDatabase.Refresh();
                }
                catch (IOException ex)
                {
                    EditorUtility.DisplayDialog("Directory Error", $"Failed to create directory: {ex.Message}", "OK");
                    Debug.LogError($"Failed to create directory: {ex.Message}");
                    return;
                }
            }

            string fullPath = Path.Combine(DEFAULT_VARIANT_DIRECTORY, DEFAULT_VARIANT_NAME + ".prefab");
            if (File.Exists(fullPath))
            {
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            }

            GameObject tempInstance = null;
            try
            {
                tempInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (tempInstance == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to instantiate prefab", "OK");
                    return;
                }

                GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(tempInstance, fullPath);
                if (prefabVariant != null)
                {
                    EditorGUIUtility.PingObject(prefabVariant);
                    Debug.Log($"Successfully created node prefab variant at: {fullPath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Failed to save prefab variant", "OK");
                }
            }
            finally
            {
                if (tempInstance != null)
                {
                    Object.DestroyImmediate(tempInstance);
                }
            }
        }
        catch (UnityException ex)
        {
            EditorUtility.DisplayDialog("Unity Error", $"Error creating prefab variant: {ex.Message}", "OK");
            Debug.LogError($"Unity Error: {ex.Message}\n{ex.StackTrace}");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Unexpected Error", $"An unexpected error occurred: {ex.Message}", "OK");
            Debug.LogError($"Unexpected error creating prefab variant: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
#endif