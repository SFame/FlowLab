
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    [SerializeField] private List<Vector2> _anchorPoints;
    [SerializeField] private List<Vector2> _points;
    [SerializeField] private float _curveSize;
    [SerializeField] private float _resolution;
    [SerializeField] private float _widthSize;
    [SerializeField] private Color _color;
    public List<Vector2> AnchorPoints => _anchorPoints;

    public void Init()
    {
        _anchorPoints ??= GetComponent<LineConnector>().GetVertices();
        _points ??= new List<Vector2>();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);

        vh.Clear();

        //Draw Vertices
        CreateLineMesh(vh);
    }

    public void SetAnchorPoints()
    {
        Init();
        _points.Clear();
        _points.Add(_anchorPoints[0]);

        for (int i = 1; i < AnchorPoints.Count - 1; i++) 
        {
            Vector2 targetPoint = AnchorPoints[i];
            Vector2 targetDir = (AnchorPoints[i] - AnchorPoints[i - 1]).normalized;
            float dstToTarget = (AnchorPoints[i] - AnchorPoints[i - 1]).magnitude;
            float dstToCurveStart = Mathf.Max(dstToTarget - _curveSize, dstToTarget / 2);

            Vector2 nextTarget = AnchorPoints[i + 1];
            Vector2 nextTargetDir = (AnchorPoints[i + 1] - AnchorPoints[i]).normalized;
            float nextLineLength = (AnchorPoints[i + 1] - AnchorPoints[i]).magnitude;

            Vector2 curveStartPoint = AnchorPoints[i-1] + targetDir * dstToCurveStart;
            Vector2 curveEndPoint = targetPoint + nextTargetDir * Mathf.Min(_curveSize, nextLineLength / 2);

            // Bezier
            for (int j = 0; j < _resolution; j++)
            {
                float t = j / (_resolution - 1f);
                Vector2 a = Vector2.Lerp(curveStartPoint, targetPoint, t);
                Vector2 b = Vector2.Lerp(targetPoint, curveEndPoint, t);
                Vector2 p = Vector2.Lerp(a, b, t);

                if ((p - (Vector2)_points[_points.Count - 1]).sqrMagnitude > 0.001f)
                {
                    _points.Add(p);
                }
            }
        }
        _points.Add(AnchorPoints[^1]);
    }
    public void CreateLineMesh(VertexHelper vh)
    {
        SetAnchorPoints();
        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 forward = Vector2.zero;
            if (i < _points.Count - 1)
                forward += _points[i + 1] - _points[i];
            if (i > 0)
                forward += _points[i] - _points[i - 1];

            forward.Normalize();
            Vector2 left = new Vector2(-forward.y, forward.x);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = _color;

            //left
            vertex.position = _points[i] + _widthSize * 0.5f * left;
            vh.AddVert(vertex);
            //right
            vertex.position = _points[i] - _widthSize * 0.5f * left;
            vh.AddVert(vertex);
        }
        //tris[triIndex] = vertIndex;
        //tris[triIndex + 1] = vertIndex + 2;
        //tris[triIndex + 2] = vertIndex + 1;

        //tris[triIndex + 3] = vertIndex + 1;
        //tris[triIndex + 4] = vertIndex + 2;
        //tris[triIndex + 5] = vertIndex + 3;
        for (int i = 0; i < _points.Count - 1; i++)
        {
            int index = i * 2;
            vh.AddTriangle(index + 0, index + 2, index + 1);
            vh.AddTriangle(index + 1, index + 2, index + 3);
        }
    }
}
