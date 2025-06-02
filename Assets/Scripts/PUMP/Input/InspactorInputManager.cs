using System.Collections.Generic;
using UnityEngine;

public class InspactorInputManager : PUMPInputManager
{
    [SerializeField] private List<BackgroundActionKeyMap> m_KeyMap;
    [SerializeField] private bool m_Enable;

    protected override void Initialize()
    {
        KeyMap.Clear();
        KeyMap.AddRange(m_KeyMap);
        SortKeyMap();
        Enable = m_Enable;
    }
}