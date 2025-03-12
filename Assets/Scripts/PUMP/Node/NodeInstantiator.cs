using System;
using System.Collections.Generic;
using UnityEngine;

public static class NodeInstantiator
{
    public static Node GetNode(Type nodeType)
    {
        if (!nodeType.IsSubclassOf(typeof(Node)))
        {
            Debug.LogError("NodeInstantiator: nodeType is must child of Node");
            return null;
        }
        
        Node instance = Activator.CreateInstance(nodeType) as Node;
        if (instance is null)
        {
            Debug.LogError($"NodeInstantiator: Could not create node of type {nodeType}");
            return null;
        }
        
        string prefabPath = instance.NodePrefebPath;
        
        GameObject prefab = GetNodePrefab(prefabPath);
        GameObject go = GameObject.Instantiate(prefab);
        
        go.AddComponent(nodeType);
        return go.GetComponent<Node>();
    }
    
    #region Privates
    private static readonly string DEFAULT_PREFAB_PATH = "PUMP/Prefab/Node/NODE";
    private static readonly Dictionary<string, GameObject> _prefebCache = new();
    
    private static GameObject GetNodePrefab(string prefabPath = "")
    {
        string path = string.IsNullOrEmpty(prefabPath) ? DEFAULT_PREFAB_PATH : prefabPath;
        CachingPrefab(path);

        if (_prefebCache.TryGetValue(path, out GameObject prefab))
            return prefab;
        
        Debug.LogError($"NodeInstantiator: Could not find prefab {path}");
        return null;
    }

    private static void CachingPrefab(string prefabPath)
    {
        if (!_prefebCache.ContainsKey(DEFAULT_PREFAB_PATH))
            _prefebCache.Add(DEFAULT_PREFAB_PATH, Resources.Load<GameObject>(DEFAULT_PREFAB_PATH));
        
        if (_prefebCache.ContainsKey(prefabPath))
            return;
        
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab is not null)
            _prefebCache[prefabPath] = prefab;  
    }
    #endregion
}
