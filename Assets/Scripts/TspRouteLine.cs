using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TspRouteLine : Graphic
{
    [SerializeField] private float lineWidth = 6f;

    private readonly List<Vector2> points = new();

    protected override void Awake()
{
    base.Awake();
    color = Color.red;
    raycastTarget = false;
}

    public void SetPoints(List<Vector2> newPoints)
    {
        points.Clear();
        points.AddRange(newPoints);
        SetVerticesDirty();
    }

    public void SetLineColor(Color newColor)
{
    color = newColor;
    SetVerticesDirty();
}
    public void ClearLine()
    {
        points.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            AddSegment(vh, points[i], points[i + 1]);
        }
    }

    private void AddSegment(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 offset =
            new Vector2(-direction.y, direction.x) * (lineWidth / 2f);

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = start - offset;
        vh.AddVert(vertex);

        vertex.position = start + offset;
        vh.AddVert(vertex);

        vertex.position = end + offset;
        vh.AddVert(vertex);

        vertex.position = end - offset;
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}