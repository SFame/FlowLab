using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SegmentController : MonoBehaviour
{
    [SerializeField] private Image[] m_Segments; // 7개


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




}
