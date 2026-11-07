using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    // Rounded preview frame and warning-symbol overlays; block textures use standard UI Images.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class QueuePanelGraphic : MaskableGraphic
    {
        [SerializeField] private Color borderColor;
        [SerializeField] private float borderWidth;
        [SerializeField] private float radius;
        [SerializeField] private float bottomGap;
        [SerializeField] private bool hazard;
        [SerializeField] private Color symbolColor;
        [SerializeField] private Color markColor;

        public void Configure(Color fill, Color border, float width, float corner, float gap,
            bool isHazard = false, Color symbol = default, Color mark = default)
        {
            if (color == fill && borderColor == border && borderWidth == width && radius == corner &&
                bottomGap == gap && hazard == isHazard && symbolColor == symbol && markColor == mark) return;
            color = fill;
            borderColor = border;
            borderWidth = width;
            radius = corner;
            bottomGap = gap;
            hazard = isHazard;
            symbolColor = symbol;
            markColor = mark;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float halfStroke = Mathf.Max(0f, borderWidth) * 0.5f;
            rect = new Rect(rect.xMin + halfStroke, rect.yMin + halfStroke,
                Mathf.Max(0f, rect.width - borderWidth), Mathf.Max(0f, rect.height - borderWidth));
            float corner = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            const int steps = 8;
            const int count = 4 * (steps + 1);
            Vector2[] points = new Vector2[count];
            for (int side = 0; side < 4; side++)
            {
                Vector2 centre = new Vector2(side < 2 ? rect.xMax - corner : rect.xMin + corner,
                    side == 0 || side == 3 ? rect.yMin + corner : rect.yMax - corner);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (-90f + side * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                    points[side * (steps + 1) + step] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * corner;
                }
            }

            mesh.AddVert(rect.center, color, Vector2.zero);
            for (int i = 0; i < count; i++) mesh.AddVert(points[i], color, Vector2.zero);
            for (int i = 0; i < count; i++)
            {
                mesh.AddTriangle(0, i + 1, (i + 1) % count + 1);
                if (borderWidth <= 0f) continue;
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];
                if (i == count - 1 && bottomGap > 0f)
                {
                    float gap = Mathf.Min(bottomGap * 0.5f, rect.width * 0.5f - corner);
                    Stroke(mesh, a, new Vector2(rect.center.x - gap, a.y), borderColor, borderWidth);
                    Stroke(mesh, new Vector2(rect.center.x + gap, b.y), b, borderColor, borderWidth);
                }
                else Stroke(mesh, a, b, borderColor, borderWidth);
            }

            if (!hazard) return;
            Vector2 centrePoint = rect.center;
            float size = Mathf.Min(rect.width, rect.height);
            int triangle = mesh.currentVertCount;
            mesh.AddVert(centrePoint + new Vector2(0f, size * 0.35f), symbolColor, Vector2.zero);
            mesh.AddVert(centrePoint + new Vector2(-size * 0.36f, -size * 0.3f), symbolColor, Vector2.zero);
            mesh.AddVert(centrePoint + new Vector2(size * 0.36f, -size * 0.3f), symbolColor, Vector2.zero);
            mesh.AddTriangle(triangle, triangle + 1, triangle + 2);
            Stroke(mesh, centrePoint + Vector2.up * size * 0.16f,
                centrePoint - Vector2.up * size * 0.05f, markColor, size * 0.1f);
            Stroke(mesh, centrePoint + new Vector2(-size * 0.05f, -size * 0.19f),
                centrePoint + new Vector2(size * 0.05f, -size * 0.19f), markColor, size * 0.1f);
        }

        private static void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, Color tint, float width)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal, tint, Vector2.zero);
            mesh.AddVert(a + normal, tint, Vector2.zero);
            mesh.AddVert(b + normal, tint, Vector2.zero);
            mesh.AddVert(b - normal, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
