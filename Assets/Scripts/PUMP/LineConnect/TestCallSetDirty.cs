using UnityEngine;

public class TestCallSetDirty : MonoBehaviour
{
    public UILineRenderer lineRenderer;
    void Update()
    {
        if (lineRenderer != null)
        {
            lineRenderer.test();
        }
    }
}
