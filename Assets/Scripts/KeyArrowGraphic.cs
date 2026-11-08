using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    // One upward arrow; each key rotates this graphic to its required direction.
    public sealed class KeyArrowGraphic : MaskableGraphic
    {
        [SerializeField, Range(0.25f, 2f)] private float edgeSoftness = 1f;
        public float EdgeSoftness
        {
            get => edgeSoftness;
            set
            {
                value = Mathf.Clamp(value, 0.25f, 2f);
                if (Mathf.Approximately(edgeSoftness, value)) return;
                edgeSoftness = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            float x = rect.center.x;
            float y = rect.center.y;
            float shaft = rect.width * 0.15f;
            Vector2[] outline =
            {
                new(x - shaft, rect.yMin), new(x + shaft, rect.yMin),
                new(x + shaft, y), new(rect.xMax, y), new(x, rect.yMax),
                new(rect.xMin, y), new(x - shaft, y)
            };
            // Overlay UI needs its own edge coverage; keep the fringe roughly one screen pixel wide.
            float halfFringe = edgeSoftness * 0.5f / Mathf.Max(0.0001f, canvas != null ? canvas.scaleFactor : 1f);
            Color transparent = color;
            transparent.a = 0f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 previous = (outline[i] - outline[(i + 6) % 7]).normalized;
                Vector2 next = (outline[(i + 1) % 7] - outline[i]).normalized;
                Vector2 a = new(-previous.y, previous.x);
                Vector2 b = new(-next.y, next.x);
                Vector2 inset = (a + b) * (halfFringe / Mathf.Max(0.01f, 1f + Vector2.Dot(a, b)));
                vertices.AddVert(outline[i] + inset, color, Vector2.zero);
                vertices.AddVert(outline[i] - inset, transparent, Vector2.zero);
            }
            // Solid shaft and head, using the inset vertices (even indices).
            vertices.AddTriangle(0, 2, 4);
            vertices.AddTriangle(0, 4, 12);
            vertices.AddTriangle(10, 12, 8);
            vertices.AddTriangle(12, 4, 8);
            vertices.AddTriangle(4, 6, 8);
            for (int i = 0; i < outline.Length; i++)
            {
                int current = i * 2;
                int next = ((i + 1) % 7) * 2;
                vertices.AddTriangle(current, current + 1, next + 1);
                vertices.AddTriangle(current, next + 1, next);
            }
        }
    }
}
