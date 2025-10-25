using UnityEngine;

public class ConsoleWindowHandle : DraggableUGUI
{
    [SerializeField] private RectTransform m_MoveTarget;
    [SerializeField] private RectTransform m_Boundary;

    public override RectTransform Rect => m_MoveTarget;

    private void Awake()
    {
        BoundaryRect = m_Boundary;
    }
}