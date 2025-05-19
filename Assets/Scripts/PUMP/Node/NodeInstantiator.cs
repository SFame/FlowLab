using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public static class NodeInstantiator
{
    public static Node GetNode(Type nodeType)
    {
        if (!nodeType.IsSubclassOf(typeof(Node)))
        {
            Debug.LogError("NodeInstantiator: nodeType is must child of Node");
            return null;
        }
        
        Node newNode = Activator.CreateInstance(nodeType) as Node;
        if (newNode is null)
        {
            throw new InvalidCastException($"NodeInstantiator: Could not create node of type {nodeType}");
        }
        
        string prefabPath = newNode.NodePrefabPath;
        
        GameObject prefab = GetNodePrefab(prefabPath);
        GameObject go = Object.Instantiate(prefab);

        INodeSupportInitializable newNodeSupport = go.GetComponent<NodeSupport>();

        if (newNodeSupport.IsUnityNull())
        {
            throw new MissingComponentException($"{newNode.GetType().Name} => The prefab is missing the NodeSupport component. Please add it.");
        }

        newNodeSupport.Initialize(newNode);

        if (newNode is INodeLifecycleCallable callable)
        {
            callable.CallOnAfterInstantiate();
        }
        else
        {
            throw new InvalidCastException($"NodeInstantiator: Node is not INodeLifecycleCallable. Node Type: [{nodeType.Name}]");
        }

        return newNode;
    }
    
    #region Privates
    private static readonly string DEFAULT_PREFAB_PATH = "PUMP/Prefab/NODE";
    private static readonly Dictionary<string, GameObject> _prefebCache = new();
    
    private static GameObject GetNodePrefab(string prefabPath = "")
    {
        string path = string.IsNullOrEmpty(prefabPath) ? DEFAULT_PREFAB_PATH : prefabPath;
        CachingPrefab(path);

        if (_prefebCache.TryGetValue(path, out GameObject prefab))
            return prefab;

        throw new System.IO.FileNotFoundException($"NodeInstantiator: Could not find prefab {path}");
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