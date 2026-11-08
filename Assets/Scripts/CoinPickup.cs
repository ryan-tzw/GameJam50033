using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class CoinPickup : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private VisualSettings visualSettings;
        private static readonly GameplaySettings.ScoringData defaultGameplayScoring = new();
        private GameplaySettings.ScoringData gameplayScoring => gameplaySettings != null ? gameplaySettings.scoring : defaultGameplayScoring;
        private static readonly VisualSettings.CoinData defaultVisualCoin = new();
        private VisualSettings.CoinData visualCoin => visualSettings != null ? visualSettings.coin : defaultVisualCoin;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private RunScore runScore;

        private Vector2Int cell;
        private SpriteRenderer visual;
        private bool cellInitialized;
        private bool collected;
        internal static readonly HashSet<CoinPickup> ActiveCoins = new();
        internal StageArea StageArea => stageArea;
        internal Vector3 SpawnCheckPosition => cellInitialized && stageArea != null
            ? stageArea.CellToWorldPosition(cell) : transform.position;

        private void OnEnable()
        {
            ActiveCoins.Add(this);
            visual = GetComponent<SpriteRenderer>();
            GetComponent<CircleCollider2D>().isTrigger = true;
            cellInitialized = false;
            collected = false;
            FindStage();
        }

        private void OnDisable()
        {
            ActiveCoins.Remove(this);
        }

        private void FindStage()
        {
            if (stageArea != null || !gameObject.scene.IsValid())
            {
                return;
            }

            // Only use the coin's own scene; prefab editing must not reference the open game scene.
            foreach (StageArea candidate in FindObjectsByType<StageArea>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == gameObject.scene)
                {
                    stageArea = candidate;
                    break;
                }
            }
        }

        private void Start()
        {
            if (!Application.IsPlaying(gameObject) || cellInitialized || stageArea == null)
            {
                return;
            }

            // Scene-placed test coins join the same moving grid as generated coins.
            cell = stageArea.WorldPositionToCell(transform.position);
            cellInitialized = true;
            transform.SetParent(stageArea.BridgeRoot, true);
            if (runScore == null)
            {
                foreach (RunScore candidate in FindObjectsByType<RunScore>(FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.scene == gameObject.scene)
                    {
                        runScore = candidate;
                        break;
                    }
                }
            }
        }

        public void Initialize(StageArea stage, RunScore score, Vector2Int gridCell)
        {
            stageArea = stage;
            runScore = score;
            cell = gridCell;
            cellInitialized = true;
            UpdateVisual();
        }

        private void LateUpdate()
        {
            FindStage();
            if (stageArea == null || collected)
            {
                return;
            }

            UpdateVisual();
            if (!Application.IsPlaying(gameObject))
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera != null && camera.WorldToViewportPoint(transform.position).x < -0.1f)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateVisual()
        {
            if (visualCoin.frames != null && visualCoin.frames.Length > 0)
            {
                // A shared clock keeps even newly spawned coins on the same animation frame.
                float time = Application.IsPlaying(gameObject) ? Time.time : 0f;
                visual.sprite = visualCoin.frames[Mathf.FloorToInt(time * visualCoin.framesPerSecond) % visualCoin.frames.Length];
            }

            if (Application.IsPlaying(gameObject) && cellInitialized)
            {
                transform.position = stageArea.CellToWorldPosition(cell);
            }
            if (visual.sprite != null)
            {
                float scale = stageArea.CellSize * visualCoin.sizeInTiles / visual.sprite.bounds.size.y;
                transform.localScale = Vector3.one * scale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (!Application.IsPlaying(gameObject) || collected || runScore == null || Time.timeScale <= 0f || player == null || !player.isActiveAndEnabled)
            {
                return;
            }

            collected = true;
            runScore.CollectCoin(gameplayScoring.points, transform.position, visual.bounds.size.y,
                visualCoin.frames, visualCoin.framesPerSecond, worldPickup: true);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
