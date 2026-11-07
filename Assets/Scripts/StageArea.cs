using UnityEngine;
using UnityEngine.Rendering;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class StageArea : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private VisualSettings visualSettings;
        private static readonly GameplaySettings.StageData defaultGameplayStage = new();
        private GameplaySettings.StageData gameplayStage => gameplaySettings != null ? gameplaySettings.stage : defaultGameplayStage;
        private static readonly VisualSettings.StageOutlineData defaultVisualStageOutline = new();
        private VisualSettings.StageOutlineData visualStageOutline => visualSettings != null ? visualSettings.stageOutline : defaultVisualStageOutline;

        [SerializeField] private LineRenderer topOutline;
        [SerializeField] private LineRenderer rightOutline;
        [SerializeField] private LineRenderer bottomOutline;
        [SerializeField] private LineRenderer leftOutline;
        [SerializeField] private BridgeController bridge;

        private float DashLength => visualStageOutline.dashLength;

        private Material outlineMaterial;
        private Texture2D dashTexture;

        public int Columns => gameplayStage.columns;
        public int Rows => gameplayStage.rows;
        public Transform BridgeRoot => bridge != null ? bridge.transform : null;
        public float BridgeOffset => bridge != null ? bridge.Offset : 0f;
        public float BridgeFrameDisplacement => bridge != null && bridge.isActiveAndEnabled ? bridge.FrameDisplacement : 0f;
        public float BridgeVelocity => bridge != null && bridge.isActiveAndEnabled ? -bridge.ScrollSpeed * CellSize : 0f;

        public Vector3 CellToWorldPosition(Vector2Int cell)
        {
            float size = CellSize;
            return transform.TransformPoint(new Vector3(
                (cell.x + 0.5f - gameplayStage.columns * 0.5f) * size + BridgeOffset,
                (cell.y + 0.5f - gameplayStage.rows * 0.5f) * size, 0f));
        }

        public Vector2Int WorldPositionToCell(Vector3 worldPosition)
        {
            float size = CellSize;
            if (size <= 0f)
            {
                return Vector2Int.zero;
            }

            Vector3 position = transform.InverseTransformPoint(worldPosition);
            position.x -= BridgeOffset;
            position /= size;
            return new Vector2Int(
                Mathf.RoundToInt(position.x + gameplayStage.columns * 0.5f - 0.5f),
                Mathf.RoundToInt(position.y + gameplayStage.rows * 0.5f - 0.5f));
        }

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
                float availableWidth = Mathf.Max(0.01f, visibleWidth - gameplayStage.screenPadding * 2f);
                float availableHeight = Mathf.Max(0.01f, visibleHeight - gameplayStage.screenPadding * 2f);
                return Mathf.Min(availableWidth / gameplayStage.columns, availableHeight / gameplayStage.rows);
            }
        }

        private void OnEnable()
        {
            PrepareOutline();
            DrawOutline();
        }

        private void Update()
        {
            PrepareOutline();
            DrawOutline();
        }

        private void OnValidate()
        {
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
            outline.startWidth = visualStageOutline.lineWidth;
            outline.endWidth = visualStageOutline.lineWidth;
            Color outlineColor = new(1f, 1f, 1f, visualStageOutline.outlineOpacity);
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
            float halfWidth = gameplayStage.columns * cellSize * 0.5f;
            float halfHeight = gameplayStage.rows * cellSize * 0.5f;

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
