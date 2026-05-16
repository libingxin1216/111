using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineDrawer : MaskableGraphic
{
    [System.Serializable]
    public class LineEntry
    {
        public RectTransform from;
        public RectTransform to;
        public Color color = Color.gray;
        public float width = 2f;
    }

    public List<LineEntry> lines = new();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var line in lines)
        {
            if (line.from == null || line.to == null) continue;
            DrawLine(vh, line);
        }
    }

    void DrawLine(VertexHelper vh, LineEntry line)
    {
        Vector2 start = WorldToLocal(line.from);
        Vector2 end = WorldToLocal(line.to);

        Vector2 dir = (end - start).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (line.width * 0.5f);

        int idx = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = line.color;

        v.position = start - perp; vh.AddVert(v);
        v.position = start + perp; vh.AddVert(v);
        v.position = end + perp; vh.AddVert(v);
        v.position = end - perp; vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    Vector2 WorldToLocal(RectTransform target)
    {
        Vector3 worldPos = target.TransformPoint(Vector3.zero);
        return rectTransform.InverseTransformPoint(worldPos);
    }

    public void Refresh() => SetVerticesDirty();
}