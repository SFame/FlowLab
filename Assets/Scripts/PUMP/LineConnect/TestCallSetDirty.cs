using UnityEngine.UI;

public class TestCallSetDirty : Graphic
{
    public UILineRenderer lineRenderer;

    protected override void Awake()
    {
        lineRenderer = transform.parent.parent.GetComponent<UILineRenderer>();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);
        lineRenderer.SetAllDirty();
    }
}
