using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class SegmentController : MonoBehaviour
{
    [SerializeField] private Image[] m_Segments; // 7개
    [SerializeField] private Color m_OnColor = new Color(0, 1, 0);
    [SerializeField] private Color m_OffColor = new Color(0.5f, 0.5f, 0.5f);

    private int IntToSegment(int value) => value switch
    {
        0 => 63,
        1 => 6,
        2 => 91,
        3 => 79,
        4 => 102,
        5 => 109,
        6 => 125,
        7 => 7,
        8 => 127,
        9 => 111,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Invalid value")
    };

    public void SetDisplay(int value)
    {
        int segmentValue = IntToSegment(value);
        bool[] segmentStates = new bool[m_Segments.Length];

        for (int i = 0; i < m_Segments.Length; i++)
        {
            segmentStates[i] = (segmentValue & (1 << i)) != 0;
        }

        UpdateSegmentDisplay(segmentStates);
    }

    public void UpdateSegmentDisplay(bool[] inputs)
    {
        for (int i = 0; i < 7; i++)
        {
            m_Segments[i].color = inputs[i] ? m_OnColor : m_OffColor;
            m_Segments[i].color.Log();
            m_OnColor.Log();
        }
    }


}
