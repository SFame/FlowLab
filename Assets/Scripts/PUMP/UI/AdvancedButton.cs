using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AdvancedButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private AdvancedButtonGraphicInfo[] m_GraphicGroup;

    [Space(10)]

    [SerializeField] private UnityEvent m_OnClick;

    private bool _isHover; 
    private bool _isDown;

    private void Awake()
    {
        if (m_GraphicGroup != null)
        {
            for (int i = 0; i < m_GraphicGroup.Length; i++)
            {
                m_GraphicGroup[i]._defaultColor = m_GraphicGroup[i].m_Graphic?.color ?? default;
            }
        }
    }

    private void SetColorEnter()
    {
        if (m_GraphicGroup == null)
        {
            return;
        }

        foreach (AdvancedButtonGraphicInfo graphicInfo in m_GraphicGroup)
        {
            if (graphicInfo.m_Graphic == null)
            {
                continue;
            }

            graphicInfo.m_Graphic.color = graphicInfo.m_EnterColor;
        }
    }

    private void SetColorDown()
    {
        if (m_GraphicGroup == null)
        {
            return;
        }

        foreach (AdvancedButtonGraphicInfo graphicInfo in m_GraphicGroup)
        {
            if (graphicInfo.m_Graphic == null)
            {
                continue;
            }

            graphicInfo.m_Graphic.color = graphicInfo.m_DownColor;
        }
    }

    private void SetColorDefault()
    {
        if (m_GraphicGroup == null)
        {
            return;
        }

        foreach (AdvancedButtonGraphicInfo graphicInfo in m_GraphicGroup)
        {
            if (graphicInfo.m_Graphic == null)
            {
                continue;
            }

            graphicInfo.m_Graphic.color = graphicInfo._defaultColor;
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        m_OnClick?.Invoke();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        _isHover = true;

        if (_isDown)
        {
            return;
        }

        SetColorEnter();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;

        if (_isDown)
        {
            return;
        }

        SetColorDefault();
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        _isDown = true;
        SetColorDown();
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        _isDown = false;

        if (_isHover)
        {
            SetColorEnter();
            return;
        }

        SetColorDefault();
    }
}

[Serializable]
public struct AdvancedButtonGraphicInfo
{
    public Graphic m_Graphic;
    public Color m_EnterColor;
    public Color m_DownColor;

    [NonSerialized] public Color _defaultColor;
}