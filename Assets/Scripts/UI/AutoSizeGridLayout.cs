using UnityEngine;
using UnityEngine.UI;

public class AutoSizeGridLayout : GridLayoutGroup
{
    public bool AutoSize { get; set; } = true;

    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        if (AutoSize) UpdateRectSize();
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
        if (AutoSize) UpdateRectSize();
    }

    private void UpdateRectSize()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        int elementsPerRow = Mathf.Max(1, Mathf.FloorToInt((rectTransform.rect.width - padding.left - padding.right + spacing.x) / (cellSize.x + spacing.x)));
        int rowCount = Mathf.CeilToInt((float)childCount / elementsPerRow);
        float requiredHeight = padding.top + padding.bottom + rowCount * cellSize.y + (rowCount - 1) * spacing.y;

        Vector2 sizeDelta = rectTransform.sizeDelta;
        sizeDelta.y = requiredHeight;
        rectTransform.sizeDelta = sizeDelta;
    }
}