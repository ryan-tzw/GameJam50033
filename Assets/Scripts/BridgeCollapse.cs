using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class BridgeCollapse : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField, Tooltip("X relative to the stage centre, in Unity units. Shaking starts when the piece's right edge reaches this line.")]
        private float warningX = -5.65f;
        [SerializeField, Tooltip("X relative to the stage centre, in Unity units. The piece falls when its right edge reaches this line. Must be left of Warning X.")]
        private float collapseX = -7.27f;
        [SerializeField, Min(0f), Tooltip("Seconds after the run starts before bridge shaking and collapse can begin.")]
        private float initialCollapseDelay = 2f;
        [SerializeField] private Color warningThresholdColor = new Color(1f, 0.85f, 0.1f, 0.8f);
        [SerializeField] private Color thresholdColor = new Color(1f, 0.5f, 0.2f, 0.8f);
        [Header("Warning Shake")]
        [SerializeField, Min(0f), Tooltip("Visual displacement at the warning line, in cells. Colliders stay still.")]
        private float initialShakeStrength = 0.01f;
        [SerializeField, Min(0f), Tooltip("Maximum visual displacement at the collapse line, in cells.")]
        private float shakeStrength = 0.06f;
        [SerializeField, Min(0f), Tooltip("Shake cycles per second.")]
        private float shakeFrequency = 25f;
        [Header("Falling Tiles")]
        [SerializeField, Min(0.01f), Tooltip("Downward acceleration in cells per second squared.")]
        private float gravity = 30f;
        [SerializeField, Min(0f), Tooltip("Initial downward speed in cells per second.")]
        private float fallSpeed = 1.5f;
        [SerializeField, Min(0f), Tooltip("Maximum random horizontal speed in cells per second.")]
        private float horizontalDrift = 0.7f;
        [SerializeField, Min(0f), Tooltip("Maximum random spin in degrees per second.")]
        private float rotationSpeed = 120f;

        private sealed class Warning
        {
            public Transform Root;
            public Brick[] Bricks;
            public SpriteRenderer[] Originals;
            public SpriteRenderer[] Visuals;
            public float Elapsed;
            public float Phase;
        }

        private sealed class FallingTile
        {
            public SpriteRenderer Visual;
            public Vector3 Velocity;
            public float Spin;
        }

        private readonly List<Warning> warnings = new();
        private readonly List<FallingTile> fallingTiles = new();
        private readonly HashSet<Transform> warningRoots = new();
        private MaterialPropertyBlock visualProperties;
        private float collapseDelayRemaining;

        private void Awake()
        {
            collapseDelayRemaining = initialCollapseDelay;
        }

        private void LateUpdate()
        {
            // Hit flashes must reach warning copies even while gameplay is paused.
            foreach (Warning warning in warnings)
            {
                for (int i = 0; i < warning.Visuals.Length; i++)
                {
                    if (warning.Originals[i] != null && warning.Visuals[i] != null)
                    {
                        warning.Originals[i].GetPropertyBlock(visualProperties);
                        warning.Visuals[i].SetPropertyBlock(visualProperties);
                    }
                }
            }

            if (Time.timeScale <= 0f || stageArea == null || stageArea.CellSize <= 0f || Time.deltaTime <= 0f)
            {
                return;
            }

            if (collapseDelayRemaining > 0f)
            {
                collapseDelayRemaining -= Time.deltaTime;
                if (collapseDelayRemaining > 0f)
                {
                    return;
                }
            }

            UpdateFallingTiles();
            FindUnstablePieces();
            UpdateWarnings();
        }

        private void FindUnstablePieces()
        {
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if ((owner == null && brick.StageArea != stageArea) ||
                    (owner != null && (!owner.IsPlaced || owner.StageArea != stageArea)))
                {
                    continue;
                }

                Transform root = owner != null ? owner.transform : brick.transform;
                if (warningRoots.Contains(root))
                {
                    continue;
                }

                Brick[] bricks = owner != null ? owner.GetComponentsInChildren<Brick>() : new[] { brick };
                if (GetRightEdgeX(bricks) > warningX)
                {
                    continue;
                }

                BeginWarning(root, bricks);
            }
        }

        private float GetRightEdgeX(Brick[] bricks)
        {
            float rightEdge = float.NegativeInfinity;
            foreach (Brick tile in bricks)
            {
                if (tile == null)
                {
                    continue;
                }

                float x = stageArea.transform.InverseTransformPoint(tile.transform.position).x;
                rightEdge = Mathf.Max(rightEdge, x + stageArea.CellSize * 0.5f);
            }

            return rightEdge;
        }

        private void BeginWarning(Transform root, Brick[] bricks)
        {
            visualProperties ??= new MaterialPropertyBlock();
            Warning warning = new Warning
            {
                Root = root,
                Bricks = bricks,
                Originals = new SpriteRenderer[bricks.Length],
                Visuals = new SpriteRenderer[bricks.Length],
                Phase = Random.Range(0f, Mathf.PI * 2f)
            };

            for (int i = 0; i < bricks.Length; i++)
            {
                SpriteRenderer original = bricks[i].GetComponent<SpriteRenderer>();
                GameObject visualObject = new GameObject("Collapsing Tile");
                visualObject.layer = original.gameObject.layer;
                visualObject.transform.SetParent(bricks[i].transform, false);
                SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
                visual.sprite = original.sprite;
                visual.sharedMaterial = original.sharedMaterial;
                visual.color = original.color;
                visual.flipX = original.flipX;
                visual.flipY = original.flipY;
                visual.sortingLayerID = original.sortingLayerID;
                visual.sortingOrder = original.sortingOrder;
                original.GetPropertyBlock(visualProperties);
                visual.SetPropertyBlock(visualProperties);
                // Only the visual copy shakes; the real brick keeps its grid position and collider.
                original.enabled = false;
                warning.Originals[i] = original;
                warning.Visuals[i] = visual;
            }

            warningRoots.Add(root);
            warnings.Add(warning);
        }

        private void UpdateWarnings()
        {
            for (int i = warnings.Count - 1; i >= 0; i--)
            {
                Warning warning = warnings[i];
                warning.Elapsed += Time.deltaTime;
                float rightEdge = GetRightEdgeX(warning.Bricks);
                // Shake strength follows distance to collapse, rather than a countdown.
                float progress = Mathf.InverseLerp(warningX, collapseX, rightEdge);
                float strength = Mathf.Lerp(initialShakeStrength, shakeStrength, progress);
                float phase = warning.Elapsed * shakeFrequency * Mathf.PI * 2f + warning.Phase;
                Vector3 offset = stageArea.transform.TransformVector(new Vector3(
                    Mathf.Sin(phase), Mathf.Cos(phase * 1.3f), 0f) * (strength * stageArea.CellSize));

                for (int j = 0; j < warning.Visuals.Length; j++)
                {
                    if (warning.Visuals[j] != null && warning.Bricks[j] != null)
                    {
                        warning.Visuals[j].transform.position = warning.Bricks[j].transform.position + offset;
                    }
                }

                if (rightEdge > collapseX)
                {
                    continue;
                }

                for (int j = 0; j < warning.Bricks.Length; j++)
                {
                    Brick brick = warning.Bricks[j];
                    SpriteRenderer visual = warning.Visuals[j];
                    if (brick == null || visual == null)
                    {
                        continue;
                    }

                    // Detach before destroying the logical piece; preserve the shaken world pose.
                    visual.transform.SetParent(null, true);
                    brick.enabled = false;
                    brick.GetComponent<BoxCollider2D>().enabled = false;
                    fallingTiles.Add(new FallingTile
                    {
                        Visual = visual,
                        Velocity = new Vector3(Random.Range(-horizontalDrift, horizontalDrift), -fallSpeed, 0f) * stageArea.CellSize,
                        Spin = Random.Range(-rotationSpeed, rotationSpeed)
                    });
                }

                if (warning.Root != null)
                {
                    Destroy(warning.Root.gameObject);
                }

                warningRoots.Remove(warning.Root);
                warnings.RemoveAt(i);
            }
        }

        private void UpdateFallingTiles()
        {
            Camera camera = Camera.main;
            for (int i = fallingTiles.Count - 1; i >= 0; i--)
            {
                FallingTile tile = fallingTiles[i];
                if (tile.Visual == null)
                {
                    fallingTiles.RemoveAt(i);
                    continue;
                }

                tile.Velocity.y -= gravity * stageArea.CellSize * Time.deltaTime;
                tile.Visual.transform.position += tile.Velocity * Time.deltaTime;
                tile.Visual.transform.Rotate(0f, 0f, tile.Spin * Time.deltaTime);

                if (camera == null)
                {
                    continue;
                }

                float depth = camera.WorldToViewportPoint(tile.Visual.transform.position).z;
                float bottom = camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)).y;
                if (tile.Visual.bounds.max.y < bottom - stageArea.CellSize)
                {
                    Destroy(tile.Visual.gameObject);
                    fallingTiles.RemoveAt(i);
                }
            }
        }

        private void OnDisable()
        {
            foreach (Warning warning in warnings)
            {
                for (int i = 0; i < warning.Visuals.Length; i++)
                {
                    if (warning.Originals[i] != null)
                    {
                        warning.Originals[i].enabled = true;
                    }

                    if (warning.Visuals[i] != null)
                    {
                        Destroy(warning.Visuals[i].gameObject);
                    }
                }
            }

            foreach (FallingTile tile in fallingTiles)
            {
                if (tile.Visual != null)
                {
                    Destroy(tile.Visual.gameObject);
                }
            }

            warnings.Clear();
            warningRoots.Clear();
            fallingTiles.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (stageArea == null)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            DrawThresholdLine(warningX, warningThresholdColor);
            DrawThresholdLine(collapseX, thresholdColor);
            Gizmos.color = previousColor;
        }

        private void DrawThresholdLine(float threshold, Color color)
        {
            float halfHeight = stageArea.Rows * stageArea.CellSize * 0.5f;
            Gizmos.color = color;
            Gizmos.DrawLine(stageArea.transform.TransformPoint(new Vector3(threshold, -halfHeight, 0f)),
                stageArea.transform.TransformPoint(new Vector3(threshold, halfHeight, 0f)));
        }

        private void OnValidate()
        {
            collapseX = Mathf.Min(collapseX, warningX - 0.01f);
            initialCollapseDelay = Mathf.Max(0f, initialCollapseDelay);
            initialShakeStrength = Mathf.Max(0f, initialShakeStrength);
            shakeStrength = Mathf.Max(initialShakeStrength, shakeStrength);
        }
    }
}
