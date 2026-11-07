using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class QueueBorderGap : BaseMeshEffect
    {
        [SerializeField] private float gapWidth;
        private readonly List<UIVertex> source = new();
        private readonly List<UIVertex> result = new();
        private readonly List<UIVertex> polygon = new();

        public void SetGap(float width)
        {
            width = Mathf.Max(0f, width);
            if (Mathf.Approximately(width, gapWidth)) return;
            gapWidth = width;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || gapWidth <= 0f) return;
            Image image = (Image)graphic;
            if (image.sprite == null || image.type != Image.Type.Sliced) return;
            Vector4 inner = DataUtility.GetInnerUV(image.sprite);
            source.Clear();
            result.Clear();
            mesh.GetUIVertexStream(source);
            float centre = image.GetPixelAdjustedRect().center.x;
            for (int i = 0; i < source.Count; i += 3)
            {
                UIVertex a = source[i], b = source[i + 1], c = source[i + 2];
                // UVs identify Unity's bottom-middle slice. Every corner and other edge is untouched.
                if (!BottomMiddle(a, inner) || !BottomMiddle(b, inner) || !BottomMiddle(c, inner))
                {
                    result.Add(a);
                    result.Add(b);
                    result.Add(c);
                    continue;
                }
                KeepSide(a, b, c, centre - gapWidth * 0.5f, true);
                KeepSide(a, b, c, centre + gapWidth * 0.5f, false);
            }
            mesh.Clear();
            mesh.AddUIVertexTriangleStream(result);
        }

        private static bool BottomMiddle(UIVertex vertex, Vector4 inner)
        {
            const float tolerance = 0.00001f;
            return vertex.uv0.x >= inner.x - tolerance && vertex.uv0.x <= inner.z + tolerance &&
                vertex.uv0.y <= inner.y + tolerance;
        }

        private void KeepSide(UIVertex a, UIVertex b, UIVertex c, float boundary, bool left)
        {
            polygon.Clear();
            UIVertex previous = c;
            bool previousInside = left ? previous.position.x <= boundary : previous.position.x >= boundary;
            for (int i = 0; i < 3; i++)
            {
                UIVertex current = i == 0 ? a : i == 1 ? b : c;
                bool inside = left ? current.position.x <= boundary : current.position.x >= boundary;
                if (inside != previousInside)
                {
                    float t = (boundary - previous.position.x) / (current.position.x - previous.position.x);
                    UIVertex intersection = previous;
                    intersection.position = Vector3.LerpUnclamped(previous.position, current.position, t);
                    intersection.uv0 = Vector4.LerpUnclamped(previous.uv0, current.uv0, t);
                    intersection.color = Color32.Lerp(previous.color, current.color, t);
                    polygon.Add(intersection);
                }
                if (inside) polygon.Add(current);
                previous = current;
                previousInside = inside;
            }
            for (int i = 1; i < polygon.Count - 1; i++)
            {
                result.Add(polygon[0]);
                result.Add(polygon[i]);
                result.Add(polygon[i + 1]);
            }
        }
    }
}
