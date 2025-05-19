using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utils;


public class SegmentSupport : MonoBehaviour
{
    [SerializeField] private Image[] m_Segments;
    [SerializeField] private Color m_OnColor = new Color(0, 1, 0);
    [SerializeField] private Color m_OffColor = new Color(0.5f, 0.5f, 0.5f);



    public void UpdateSegmentDisplay(bool[] inputs)
    {
        for (int i = 0; i < 7; i++)
        {
            m_Segments[i].color = inputs[i] ? m_OnColor : m_OffColor;
        }
    }
}
