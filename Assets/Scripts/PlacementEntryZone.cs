using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageArea))]
    public sealed class PlacementEntryZone : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Width of the entry zone, measured in tiles inward from the stage's right edge. Larger values move the threshold left.")]
        private float tilesFromRight = 5f;
        [SerializeField] private Color color = new Color(0.85f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float fillOpacity = 0.035f;
        [SerializeField, Range(0f, 1f)] private float outlineOpacity = 0.25f;
        [SerializeField, Min(0.001f), Tooltip("Outline thickness in cells.")]
        private float lineWidth = 0.04f;
        [SerializeField, Min(0.01f), Tooltip("Dash length and gap in cells.")]
        private float dashLength = 0.35f;

        private StageArea stage;
        private GameObject visual;
        private Mesh mesh;
        private Material material;
        private Vector4 previousGeometry;
        private Color previousColor;
        private Vector3 previousStyle;
        private readonly List<Vector3> vertices = new();
        private readonly List<int> triangles = new();
        private readonly List<Color> colors = new();

        private float PlacementRightX => (stage.Columns * 0.5f -
            Mathf.Clamp(tilesFromRight, 0f, stage.Columns)) * stage.CellSize;

        public bool IsActivePlacementPosition(Vector3 worldPosition)
        {
            return !isActiveAndEnabled || stage == null ||
                transform.InverseTransformPoint(worldPosition).x <= PlacementRightX + 0.0001f;
        }

        public bool AllowsLock(Vector3 cellCenter)
        {
            return !isActiveAndEnabled || stage == null ||
                transform.InverseTransformPoint(cellCenter).x + stage.CellSize * 0.5f <=
                PlacementRightX + 0.0001f;
        }

        private void OnEnable()
        {
            stage = GetComponent<StageArea>();
            visual = new GameObject("Placement Entry Zone") { hideFlags = HideFlags.HideAndDontSave };
            visual.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Entry Zone Overlay", hideFlags = HideFlags.HideAndDontSave };
            material = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = Texture2D.whiteTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 90;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            DrawZone(true);
        }

        private void LateUpdate()
        {
            DrawZone(false);
        }

        private void DrawZone(bool force)
        {
            if (stage == null || mesh == null || stage.CellSize <= 0f)
            {
                return;
            }

            float size = stage.CellSize;
            float right = stage.Columns * size * 0.5f;
            float left = PlacementRightX;
            float halfHeight = stage.Rows * size * 0.5f;
            Vector4 geometry = new Vector4(left, right, halfHeight, size);
            Vector3 style = new Vector3(fillOpacity, outlineOpacity, lineWidth);
            if (!force && geometry == previousGeometry && color == previousColor && style == previousStyle &&
                previousDashLength == dashLength)
            {
                return;
            }

            previousGeometry = geometry;
            previousColor = color;
            previousStyle = style;
            previousDashLength = dashLength;
            vertices.Clear();
            triangles.Clear();
            colors.Clear();
            if (right > left)
            {
                Color fill = new Color(color.r, color.g, color.b, fillOpacity);
                AddQuad(new Vector2(left, -halfHeight), new Vector2(right, -halfHeight),
                    new Vector2(right, halfHeight), new Vector2(left, halfHeight), fill);
                Vector2 bottomLeft = new Vector2(left, -halfHeight);
                Vector2 topLeft = new Vector2(left, halfHeight);
                Vector2 bottomRight = new Vector2(right, -halfHeight);
                Vector2 topRight = new Vector2(right, halfHeight);
                AddDashedEdge(bottomLeft, topLeft, size);
                AddDashedEdge(topLeft, topRight, size);
                AddDashedEdge(topRight, bottomRight, size);
                AddDashedEdge(bottomRight, bottomLeft, size);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private float previousDashLength;

        private void AddDashedEdge(Vector2 start, Vector2 end, float size)
        {
            float length = Vector2.Distance(start, end);
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (lineWidth * size * 0.5f);
            float dash = Mathf.Max(0.01f, dashLength) * size;
            Color outline = new Color(color.r, color.g, color.b, outlineOpacity);
            for (float distance = 0f; distance < length; distance += dash * 2f)
            {
                Vector2 a = start + direction * distance;
                Vector2 b = start + direction * Mathf.Min(distance + dash, length);
                AddQuad(a - normal, b - normal, b + normal, a + normal, outline);
            }
        }

        private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) colors.Add(tint);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
        }

        private void OnDisable()
        {
            RemoveGenerated(visual);
            RemoveGenerated(mesh);
            RemoveGenerated(material);
        }

        private static void RemoveGenerated(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
