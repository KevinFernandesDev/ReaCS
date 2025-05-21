#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class FlowingEdge : Edge
{
    private List<VisualElement> trailDots = new();
    private const int TrailLength = 5;

    public bool IsLoopPulse { get; private set; }

    public FlowingEdge()
    {
        for (int i = 0; i < TrailLength; i++)
        {
            var dot = new VisualElement();
            dot.style.width = 6;
            dot.style.height = 6;
            dot.style.position = Position.Absolute;
            dot.style.opacity = 0f;
            dot.style.backgroundImage = new StyleBackground(MakeCircleTexture());
            dot.style.visibility = Visibility.Hidden;
            trailDots.Add(dot);
            Add(dot);
        }
    }

    public void UpdateTrail(float t)
    {
        if (edgeControl == null || trailDots.Count == 0)
            return;

        for (int i = 0; i < trailDots.Count; i++)
        {
            float delay = i * 0.08f;
            float trailT = (t - delay) % 1f;
            if (trailT < 0f) trailT += 1f;

            Vector2 pos = BezierPoint(trailT);
            var dot = trailDots[i];
            dot.style.left = pos.x - 3;
            dot.style.top = pos.y - 3;
            dot.style.opacity = 1f - (i / (float)trailDots.Count);
            dot.style.backgroundColor = Color.yellow;
            dot.style.visibility = Visibility.Visible;
        }
    }

    public void HideTrail()
    {
        foreach (var dot in trailDots)
        {
            dot.style.opacity = 0f;
            dot.style.visibility = Visibility.Hidden;
        }
    }

    private Vector2 BezierPoint(float t)
    {
        if (edgeControl == null) return Vector2.zero;

        Vector2 p0 = edgeControl.from;
        Vector2 p1 = edgeControl.from + new Vector2(50f, 0f);
        Vector2 p2 = edgeControl.to + new Vector2(-50f, 0f);
        Vector2 p3 = edgeControl.to;

        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return uuu * p0 +
               3 * uu * t * p1 +
               3 * u * tt * p2 +
               ttt * p3;
    }

    private Texture2D MakeCircleTexture()
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                pixels[index] = dist <= radius ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
#endif
