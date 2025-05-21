using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PlayerNodeInventory
{
    public static event Action<Type> OnNodeUnlocked;

    //기본제공
    // 기본 노드 카테고리 구분
    private static readonly Dictionary<string, List<Type>> BaseNodesByCategory = new Dictionary<string, List<Type>>
    {
        {
            "Logic", new List<Type> { typeof(AND), typeof(OR), typeof(NOT) }
        },
        {
            "Signal", new List<Type> { typeof(Splitter) }
        }
    };
    // 해금된 노드 카테고리 구분
    private static Dictionary<string, List<Type>> UnlockedNodesByCategory = new Dictionary<string, List<Type>>();


    // 플레이어가 사용가능한 노드 목록
    //public static List<Type> AvailableNodes => BaseNodes.Concat(UnlockedNodes).Distinct().ToList();

    private static readonly Dictionary<Type, string> NodeCategoryMap = new Dictionary<Type, string>
    {
        { typeof(AND), "Logic" },
        { typeof(OR), "Logic" },
        { typeof(NOT), "Logic" },
        { typeof(NAND), "Logic" },
        { typeof(NOR), "Logic" },
        { typeof(XNOR), "Logic" },
        { typeof(XOR), "Logic" },      

        { typeof(Splitter), "Signal" },
        
    };

    private static readonly Dictionary<Type, string> NodeDisplayNames = new Dictionary<Type, string>
    {
        { typeof(AND), "AND" },
        { typeof(OR), "OR" },
        { typeof(NOT), "NOT" },
        { typeof(NAND), "NAND" },
        { typeof(NOR), "NOR" },
        { typeof(XNOR), "XNOR" },
        { typeof(XOR), "XOR" },
        { typeof(Splitter), "Split" },
        
    };


    public static string GetDisplayName(Type nodeType)
    {
        if (NodeDisplayNames.TryGetValue(nodeType, out string name))
            return name;
        return nodeType.Name;
    }
    public static string GetNodeCategory(Type nodeType)
    {
        if (NodeCategoryMap.TryGetValue(nodeType, out string category))
            return category;
        return "Logic"; // 기본 카테고리
    }

    public static Dictionary<string, Dictionary<Type, string>> GetAvailableNodesByCategory()
    {
        Dictionary<string, Dictionary<Type, string>> result = new Dictionary<string, Dictionary<Type, string>>();

        // 모든 카테고리를 먼저 초기화
        foreach (string category in NodeCategoryMap.Values.Distinct())
        {
            result[category] = new Dictionary<Type, string>();
        }

        // 기본 노드 추가
        foreach (var categoryKvp in BaseNodesByCategory)
        {
            string category = categoryKvp.Key;
            foreach (Type nodeType in categoryKvp.Value)
            {
                if (!result.ContainsKey(category))
                    result[category] = new Dictionary<Type, string>();

                result[category][nodeType] = GetDisplayName(nodeType);
            }
        }

        // 해금된 노드 추가
        foreach (var categoryKvp in UnlockedNodesByCategory)
        {
            string category = categoryKvp.Key;
            foreach (Type nodeType in categoryKvp.Value)
            {
                if (!result.ContainsKey(category))
                    result[category] = new Dictionary<Type, string>();

                result[category][nodeType] = GetDisplayName(nodeType);
            }
        }

        // 빈 카테고리 제거
        var emptyCategories = result.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
        foreach (var emptyCategory in emptyCategories)
        {
            result.Remove(emptyCategory);
        }

        return result;
    }
    // 이미 사용 가능한가?
    public static bool IsNodeAvailable(Type nodeType)
    {
        string category = GetNodeCategory(nodeType);

        return (BaseNodesByCategory.ContainsKey(category) && BaseNodesByCategory[category].Contains(nodeType)) ||
               (UnlockedNodesByCategory.ContainsKey(category) && UnlockedNodesByCategory[category].Contains(nodeType));
    }

    public static List<Type> GetUnlockedNodeList()
    {
        List<Type> allUnlockedNodes = new List<Type>();
        foreach (var categoryKvp in UnlockedNodesByCategory)
        {
            allUnlockedNodes.AddRange(categoryKvp.Value);
        }
        return allUnlockedNodes;
    }

    public static bool UnlockNode(Type nodeType)
    {
        if (IsNodeAvailable(nodeType))
            return false;

        string category = GetNodeCategory(nodeType);

        if (!UnlockedNodesByCategory.ContainsKey(category))
            UnlockedNodesByCategory[category] = new List<Type>();

        UnlockedNodesByCategory[category].Add(nodeType);
        OnNodeUnlocked?.Invoke(nodeType);
        return true;
    }

    // 노드인벤토리 생성시 ,
    public static void Initialize()
    {
        //저장된 해금노드 불러오기
        LoadUnlockedNodes();
    }

    public static void LoadUnlockedNodes()
    {
        UnlockedNodesByCategory.Clear();

        // 저장된 정보에서 불러와서 UnlockedNodes 리스트에 추가
        foreach (var nodeType in GameSaveManager.Instance.GetUnlockNodeList())
        {
            if (nodeType != null && !IsNodeAvailable(nodeType))
            {
                string category = GetNodeCategory(nodeType);

                if (!UnlockedNodesByCategory.ContainsKey(category))
                    UnlockedNodesByCategory[category] = new List<Type>();

                UnlockedNodesByCategory[category].Add(nodeType);
            }
        }

    }
}

