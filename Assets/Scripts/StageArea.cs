using UnityEngine;
using UnityEngine.Rendering;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class StageArea : MonoBehaviour
    {
        [SerializeField, Min(1)] private int columns = 20;
        [SerializeField, Min(1)] private int rows = 10;
        [SerializeField, Min(0f)] private float screenPadding = 0.5f;
        [SerializeField, Range(0.01f, 0.1f)] private float lineWidth = 0.05f;
        [SerializeField, Range(0f, 1f)] private float outlineOpacity = 0.25f;
        [SerializeField] private LineRenderer topOutline;
        [SerializeField] private LineRenderer rightOutline;
        [SerializeField] private LineRenderer bottomOutline;
        [SerializeField] private LineRenderer leftOutline;

        private const float DashLength = 0.2f;

        private Material outlineMaterial;
        private Texture2D dashTexture;

        public float CellSize
        {
            get
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null || !mainCamera.orthographic)
                {
                    return 0f;
                }

                float visibleHeight = mainCamera.orthographicSize * 2f;
                float visibleWidth = visibleHeight * mainCamera.aspect;
                float availableWidth = Mathf.Max(0.01f, visibleWidth - screenPadding * 2f);
                float availableHeight = Mathf.Max(0.01f, visibleHeight - screenPadding * 2f);
                return Mathf.Min(availableWidth / columns, availableHeight / rows);
            }
        }

        private void OnEnable()
        {
            PrepareOutline();
            DrawOutline();
        }

        private void Update()
        {
            DrawOutline();
        }

        private void OnValidate()
        {
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);
            screenPadding = Mathf.Max(0f, screenPadding);
            lineWidth = Mathf.Max(0.01f, lineWidth);
            outlineOpacity = Mathf.Clamp01(outlineOpacity);

            if (!isActiveAndEnabled)
            {
                return;
            }

            PrepareOutline();
            DrawOutline();
        }

        private void PrepareOutline()
        {
            if (outlineMaterial == null)
            {
                CreateOutlineMaterial();
            }

            ConfigureOutline(topOutline);
            ConfigureOutline(rightOutline);
            ConfigureOutline(bottomOutline);
            ConfigureOutline(leftOutline);
        }

        private void ConfigureOutline(LineRenderer outline)
        {
            if (outline == null)
            {
                return;
            }

            outline.sharedMaterial = outlineMaterial;
            outline.loop = false;
            outline.useWorldSpace = false;
            outline.positionCount = 2;
            outline.startWidth = lineWidth;
            outline.endWidth = lineWidth;
            Color outlineColor = new(1f, 1f, 1f, outlineOpacity);
            outline.startColor = outlineColor;
            outline.endColor = outlineColor;
            outline.textureMode = LineTextureMode.Tile;
            outline.textureScale = new Vector2(1f / (DashLength * 2f), 1f);
            outline.alignment = LineAlignment.View;
            outline.numCapVertices = 0;
            outline.numCornerVertices = 0;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.sortingOrder = 100;
        }

        private void DrawOutline()
        {
            Camera mainCamera = Camera.main;

            if (topOutline == null || rightOutline == null ||
                bottomOutline == null || leftOutline == null ||
                mainCamera == null || !mainCamera.orthographic)
            {
                return;
            }

            float cellSize = CellSize;
            float halfWidth = columns * cellSize * 0.5f;
            float halfHeight = rows * cellSize * 0.5f;

            topOutline.SetPosition(0, new Vector3(-halfWidth, halfHeight, 0f));
            topOutline.SetPosition(1, new Vector3(halfWidth, halfHeight, 0f));
            rightOutline.SetPosition(0, new Vector3(halfWidth, halfHeight, 0f));
            rightOutline.SetPosition(1, new Vector3(halfWidth, -halfHeight, 0f));
            bottomOutline.SetPosition(0, new Vector3(halfWidth, -halfHeight, 0f));
            bottomOutline.SetPosition(1, new Vector3(-halfWidth, -halfHeight, 0f));
            leftOutline.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0f));
            leftOutline.SetPosition(1, new Vector3(-halfWidth, halfHeight, 0f));
        }

        private void CreateOutlineMaterial()
        {
            dashTexture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
            {
                name = "Stage Outline Dashes",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave
            };
            dashTexture.SetPixels(new[] { Color.white, Color.clear });
            dashTexture.Apply();

            Shader spriteShader = Shader.Find("Sprites/Default");
            outlineMaterial = new Material(spriteShader)
            {
                name = "Stage Outline Material",
                mainTexture = dashTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void OnDisable()
        {
            RemoveGeneratedMaterial(topOutline);
            RemoveGeneratedMaterial(rightOutline);
            RemoveGeneratedMaterial(bottomOutline);
            RemoveGeneratedMaterial(leftOutline);

            DestroyGeneratedObject(outlineMaterial);
            DestroyGeneratedObject(dashTexture);
            outlineMaterial = null;
            dashTexture = null;
        }

        private void RemoveGeneratedMaterial(LineRenderer outline)
        {
            if (outline != null && outline.sharedMaterial == outlineMaterial)
            {
                outline.sharedMaterial = null;
            }
        }

        private static void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedObject);
            }
            else
            {
                DestroyImmediate(generatedObject);
            }
        }
    }
}
