using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageArea))]
    public sealed class PlacementEntryZone : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private VisualSettings visualSettings;
        private static readonly GameplaySettings.EntryZoneData defaultGameplayEntryZone = new();
        private GameplaySettings.EntryZoneData gameplayEntryZone => gameplaySettings != null ? gameplaySettings.entryZone : defaultGameplayEntryZone;
        private static readonly VisualSettings.EntryZoneData defaultVisualEntryZone = new();
        private VisualSettings.EntryZoneData visualEntryZone => visualSettings != null ? visualSettings.entryZone : defaultVisualEntryZone;
        private static readonly VisualSettings.NoBuildZoneData defaultNoBuildZone = new();
        private VisualSettings.NoBuildZoneData noBuildStyle => visualSettings != null ? visualSettings.noBuildZone : defaultNoBuildZone;

        private StageArea stage;
        private GameObject visual;
        private Mesh mesh;
        private Mesh fillMesh;
        private Material material;
        private Text entryLabel, noBuildLabel;
        private Vector4 previousGeometry;
        private Color previousColor;
        private Vector3 previousStyle;
        private Vector4 previousNoBuildStyle;
        private Color previousNoBuildColor;
        private readonly List<Vector3> vertices = new();
        private readonly List<int> triangles = new();
        private readonly List<int> fillTriangles = new();
        private readonly List<Color> colors = new();

        private float PlacementRightX => (stage.Columns * 0.5f -
            Mathf.Clamp(gameplayEntryZone.tilesFromRight, 0f, stage.Columns)) * stage.CellSize;

        public bool IsActivePlacementPosition(Vector3 worldPosition)
        {
            return !isActiveAndEnabled || stage == null ||
                transform.InverseTransformPoint(worldPosition).x <= PlacementRightX + 0.0001f;
        }

        public bool IsBuildPositionAllowed(Vector3 worldRightEdge)
        {
            return !isActiveAndEnabled || worldRightEdge.x <= gameplayEntryZone.noBuildX + 0.0001f;
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
            // Boundaries remain above zone fills (-10), but behind gameplay and ghosts (-1).
            renderer.sortingOrder = -9;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Fills sit above the background (-100 to -95) and behind gameplay, including ghosts (-1).
            GameObject fillVisual = new GameObject("Zone Fills") { hideFlags = HideFlags.HideAndDontSave };
            fillVisual.transform.SetParent(visual.transform, false);
            fillMesh = new Mesh { name = "Zone Fills", hideFlags = HideFlags.HideAndDontSave };
            fillVisual.AddComponent<MeshFilter>().sharedMesh = fillMesh;
            MeshRenderer fillRenderer = fillVisual.AddComponent<MeshRenderer>();
            fillRenderer.sharedMaterial = material;
            fillRenderer.sortingOrder = -10;
            fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fillRenderer.receiveShadows = false;
            entryLabel = CreateLabel("Entry Zone Label");
            noBuildLabel = CreateLabel("No-Build Zone Label");
            DrawZone(true);
        }

        private Text CreateLabel(string name)
        {
            GameObject label = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Text))
                { hideFlags = HideFlags.HideAndDontSave };
            label.transform.SetParent(visual.transform, false);
            Canvas canvas = label.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = -8;
            Text text = label.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.fontSize = 48;
            text.rectTransform.sizeDelta = new Vector2(1200f, 120f);
            return text;
        }

        private void UpdateLabel(Text text, VisualSettings.ZoneLabelData settings, Color color, float boundary, bool zoneVisible)
        {
            text.enabled = settings.visible && zoneVisible && settings.font != null;
            if (!text.enabled) return;
            text.text = settings.text;
            text.font = settings.font;
            color.a = settings.opacity;
            text.color = color;
            // Keep label size and its gap in world units while the boundary follows its existing zone rule.
            Vector3 position = transform.TransformPoint(new Vector3(boundary, 0f, 0f)) +
                transform.right * (settings.rightOffset + settings.fontSize * 0.5f) + transform.up * settings.verticalOffset;
            text.rectTransform.SetPositionAndRotation(position, transform.rotation * Quaternion.Euler(0f, 0f, 90f));
            text.rectTransform.localScale = Vector3.one * (settings.fontSize / (48f * Mathf.Abs(transform.lossyScale.x)));
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
            Vector3 style = new Vector3(visualEntryZone.fillOpacity, visualEntryZone.outlineOpacity, visualEntryZone.lineWidth);
            float buildLeft = transform.InverseTransformPoint(new Vector3(gameplayEntryZone.noBuildX,
                transform.position.y, transform.position.z)).x;
            UpdateLabel(entryLabel, visualEntryZone.label, visualEntryZone.color, left, left < right);
            UpdateLabel(noBuildLabel, noBuildStyle.label, noBuildStyle.color, buildLeft, buildLeft < right);
            Vector4 buildStyle = new Vector4(buildLeft, noBuildStyle.fillOpacity,
                noBuildStyle.lineOpacity, noBuildStyle.lineWidth);
            if (!force && geometry == previousGeometry && visualEntryZone.color == previousColor && style == previousStyle &&
                buildStyle == previousNoBuildStyle && noBuildStyle.color == previousNoBuildColor &&
                previousDashLength == visualEntryZone.dashLength)
            {
                return;
            }

            previousGeometry = geometry;
            previousColor = visualEntryZone.color;
            previousStyle = style;
            previousDashLength = visualEntryZone.dashLength;
            previousNoBuildStyle = buildStyle;
            previousNoBuildColor = noBuildStyle.color;
            vertices.Clear();
            triangles.Clear();
            fillTriangles.Clear();
            colors.Clear();
            if (right > left)
            {
                Color fill = new Color(visualEntryZone.color.r, visualEntryZone.color.g, visualEntryZone.color.b, visualEntryZone.fillOpacity);
                AddQuad(new Vector2(left, -halfHeight), new Vector2(right, -halfHeight),
                    new Vector2(right, halfHeight), new Vector2(left, halfHeight), fill, isFill: true);
                Vector2 bottomLeft = new Vector2(left, -halfHeight);
                Vector2 topLeft = new Vector2(left, halfHeight);
                Vector2 bottomRight = new Vector2(right, -halfHeight);
                Vector2 topRight = new Vector2(right, halfHeight);
                AddDashedEdge(bottomLeft, topLeft, size);
                AddDashedEdge(topLeft, topRight, size);
                AddDashedEdge(topRight, bottomRight, size);
                AddDashedEdge(bottomRight, bottomLeft, size);
            }

            // Only one solid boundary: the dashed rectangle still identifies the natural buffer.
            if (buildLeft < right)
            {
                Color fill = noBuildStyle.color;
                fill.a = noBuildStyle.fillOpacity;
                AddQuad(new Vector2(buildLeft, -halfHeight), new Vector2(right, -halfHeight),
                    new Vector2(right, halfHeight), new Vector2(buildLeft, halfHeight), fill, isFill: true);
                Color line = noBuildStyle.color;
                line.a = noBuildStyle.lineOpacity;
                float halfWidth = noBuildStyle.lineWidth * 0.5f / transform.lossyScale.x;
                AddQuad(new Vector2(buildLeft - halfWidth, -halfHeight), new Vector2(buildLeft + halfWidth, -halfHeight),
                    new Vector2(buildLeft + halfWidth, halfHeight), new Vector2(buildLeft - halfWidth, halfHeight), line);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            fillMesh.Clear();
            fillMesh.SetVertices(vertices);
            fillMesh.SetColors(colors);
            fillMesh.SetTriangles(fillTriangles, 0);
            fillMesh.RecalculateBounds();
        }

        private float previousDashLength;

        private void AddDashedEdge(Vector2 start, Vector2 end, float size)
        {
            float length = Vector2.Distance(start, end);
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (visualEntryZone.lineWidth * size * 0.5f);
            float dash = Mathf.Max(0.01f, visualEntryZone.dashLength) * size;
            Color outline = new Color(visualEntryZone.color.r, visualEntryZone.color.g, visualEntryZone.color.b, visualEntryZone.outlineOpacity);
            for (float distance = 0f; distance < length; distance += dash * 2f)
            {
                Vector2 a = start + direction * distance;
                Vector2 b = start + direction * Mathf.Min(distance + dash, length);
                AddQuad(a - normal, b - normal, b + normal, a + normal, outline);
            }
        }

        private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint, bool isFill = false)
        {
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) colors.Add(tint);
            List<int> target = isFill ? fillTriangles : triangles;
            target.Add(first); target.Add(first + 1); target.Add(first + 2);
            target.Add(first); target.Add(first + 2); target.Add(first + 3);
        }

        private void OnDisable()
        {
            RemoveGenerated(visual);
            RemoveGenerated(mesh);
            RemoveGenerated(fillMesh);
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
