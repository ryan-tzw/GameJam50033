using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class IslandChest : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private SpawnSettings spawnSettings;
        [SerializeField] private GameplaySettings gameplaySettings;
        internal static readonly HashSet<IslandChest> ActiveChests = new();
        internal StageArea StageArea { get; private set; }
        private Vector2Int cell;
        private SpriteRenderer visual;
        private BoxCollider2D trigger;
        private bool opened;
        private float elapsed;
        private RunScore runScore;

        private void OnEnable()
        {
            visual = GetComponent<SpriteRenderer>();
            trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            opened = false;
            elapsed = 0f;
            ActiveChests.Add(this);
        }

        private void OnDisable()
        {
            ActiveChests.Remove(this);
        }

        internal void Initialize(StageArea stage, Vector2Int gridCell)
        {
            StageArea = stage;
            cell = gridCell;
            foreach (RunScore candidate in FindObjectsByType<RunScore>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == gameObject.scene)
                {
                    runScore = candidate;
                    break;
                }
            }
            UpdateVisual();
        }

        internal Vector3 SpawnCheckPosition => StageArea.CellToWorldPosition(cell) +
            StageArea.transform.TransformVector((Vector3)visualSettings.island.chestOffsetInTiles * StageArea.CellSize);

        private void LateUpdate()
        {
            if (StageArea == null || Time.deltaTime <= 0f)
            {
                return;
            }

            if (opened)
            {
                elapsed += Time.deltaTime;
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            VisualSettings.IslandData settings = visualSettings.island;
            if (settings.chestFrames == null || settings.chestFrames.Length == 0)
            {
                return;
            }

            int frame = opened ? Mathf.Min(Mathf.FloorToInt(elapsed * settings.chestFramesPerSecond),
                settings.chestFrames.Length - 1) : 0;
            visual.sprite = settings.chestFrames[frame];
            Sprite closedSprite = settings.chestFrames[0];
            float scale = StageArea.CellSize * settings.chestSizeInTiles / closedSprite.bounds.size.y;
            transform.localScale = Vector3.one * scale;
            // Anchor the closed frame's bottom centre to the platform, regardless of sprite pivot.
            Vector3 pivotOffset = new Vector3(-closedSprite.bounds.center.x * scale,
                -StageArea.CellSize * 0.5f - closedSprite.bounds.min.y * scale, 0f);
            transform.position = SpawnCheckPosition + StageArea.transform.TransformVector(pivotOffset);
            trigger.size = settings.chestFrames[0].bounds.size;
            trigger.offset = settings.chestFrames[0].bounds.center;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (!opened && Time.timeScale > 0f && player != null && player.isActiveAndEnabled)
            {
                opened = true;
                elapsed = 0f;
                ReleaseRewards();
            }
        }

        private void ReleaseRewards()
        {
            if (runScore == null || spawnSettings == null || gameplaySettings == null)
            {
                Debug.LogError("Assign chest reward settings and ensure the scene has a RunScore.", this);
                return;
            }

            SpawnSettings.IslandsData rewards = spawnSettings.islands;
            int coins = Random.Range(rewards.minimumChestCoins, rewards.maximumChestCoins + 1);
            int gems = Random.Range(rewards.minimumChestGems, rewards.maximumChestGems + 1);
            Vector3 origin = visual.bounds.center;
            float delay = 0f;
            for (int i = 0; i < coins + gems; i++)
            {
                bool isGem = i >= coins;
                VisualSettings.CoinData collectible = isGem ? visualSettings.gem : visualSettings.coin;
                int points = isGem ? gameplaySettings.scoring.gemPoints : gameplaySettings.scoring.points;
                // Queue every reward immediately so death or collapse cannot cancel unspawned treasure.
                runScore.CollectCoin(points, origin, StageArea.CellSize * collectible.sizeInTiles,
                    collectible.frames, collectible.framesPerSecond, true, delay);
                float minimum = Mathf.Max(0f, visualSettings.coinFlight.minimumArrivalStagger);
                delay += Random.Range(minimum, Mathf.Max(minimum, visualSettings.coinFlight.maximumArrivalStagger));
            }
        }

        internal SpriteRenderer DetachForCollapse()
        {
            trigger.enabled = false;
            enabled = false;
            transform.SetParent(null, true);
            return visual;
        }
    }
}
