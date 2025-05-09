using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PlayerNodeInventory
{
    public static event Action<Type> OnNodeUnlocked;

    //기본제공
    private static readonly List<Type> BaseNodes = new List<Type>
    {
        typeof(AND),
        typeof(OR),
        typeof(NOT),
        typeof(Splitter)
    };
    //해금된 노드
    private static List<Type> UnlockedNodes = new List<Type>();

    // 플레이어가 사용가능한 노드 목록
    public static List<Type> AvailableNodes => BaseNodes.Concat(UnlockedNodes).Distinct().ToList();

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
        { typeof(Comparator), "Comparator" },
        { typeof(Switch), "Switch" },
        // Add all possible nodes here
    };


    public static string GetDisplayName(Type nodeType)
    {
        if (NodeDisplayNames.TryGetValue(nodeType, out string name))
            return name;
        return nodeType.Name;
    }

    // 이미 사용가능한가?
    public static bool IsNodeAvailable(Type nodeType)
    {
        return BaseNodes.Contains(nodeType) || UnlockedNodes.Contains(nodeType);
    }

    // Get all available nodes as a dictionary of Type to display name
    public static Dictionary<Type, string> GetAvailableNodeTypes()
    {
        return AvailableNodes.ToDictionary(
            type => type,
            type => GetDisplayName(type)
        );
    }

    public static bool UnlockNode(Type nodeType)
    {
        if (IsNodeAvailable(nodeType))
            return false;

        UnlockedNodes.Add(nodeType);
        SaveUnlockedNodes();
        OnNodeUnlocked?.Invoke(nodeType);
        return true;
    }

    // 노드인벤토리 생성시 ,
    public static void Initialize()
    {
        //저장된 해금노드 불러오기
        LoadUnlockedNodes();
    }


    private static void SaveUnlockedNodes()
    {
        // 해금노드 저장
    }

    private static void LoadUnlockedNodes()
    {
        UnlockedNodes.Clear();

        // 저장된 정보에서 불러와서 UnlockedNodes 리스트에 추가
    }
}

