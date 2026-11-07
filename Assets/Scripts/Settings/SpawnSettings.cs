using System;
using UnityEngine;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "SpawnSettings", menuName = "Neverland/Settings/Spawn")]
    public sealed class SpawnSettings : ScriptableObject
    {
        [Header("Spawn Clearance")]
        [Min(0f), Tooltip("Empty tiles between newly spawned coin/hazard groups and existing bricks or coins. Does not restrict manual placement.")]
        public float minimumGroupGapInTiles = 1f;
        [Header("Pieces")]
        public PiecesData pieces = new();
        [Header("Difficulty")]
        public DifficultyData difficulty = new();
        [Header("Hazard Clusters")]
        public HazardClustersData hazardClusters = new();
        [Header("Coins")]
        public CoinsData coins = new();

        [Serializable]
        public sealed class PiecesData
        {
            public Tetromino tetrominoPrefab;
            public Brick hazardBrickPrefab;
            [Min(0)]
            public int hazardFreePieces = 5;
            [Min(0), Tooltip("Starting row, counted from the bottom of the stage.")]
            public int spawnRow = 5;
            [Min(0f), Tooltip("Space from the stage's right edge, measured in cells.")]
            public float spawnInset = 0.5f;
            [Min(0f), Tooltip("Move the spawn area farther right, measured in cells.")]
            public float spawnRightOffset = 1.5f;
            public Color spawnGizmoColor = new Color(0.3f, 1f, 1f, 0.7f);
        }

        [Serializable]
        public sealed class DifficultyData
        {
            [Min(0.01f)]
            public float progressionDuration = 120f;
            [Range(0f, 1f)]
            public float initialOneHazardChance = 0.075f;
            [Range(0f, 1f)]
            public float initialTwoHazardChance = 0.025f;
            [Range(0f, 1f)]
            public float finalOneHazardChance = 0.125f;
            [Range(0f, 1f)]
            public float finalTwoHazardChance = 0.075f;
            [Min(1f), Tooltip("Maximum multiplier on bridge scrolling. Leave at 1 for unchanged speeds; use 1.5 for the planned progression.")]
            public float maximumSpeedMultiplier = 1f;
        }

        [Serializable]
        public sealed class HazardClustersData
        {
            public Tetromino tetrominoPrefab;
            public Brick hazardBrickPrefab;
            [Min(0f)]
            public float initialDelay = 8f;
            [Min(0.1f)]
            public float minimumSpawnInterval = 6f;
            [Min(0.1f)]
            public float maximumSpawnInterval = 10f;
            [Min(0)]
            public int minimumSpawnRow = 1;
            [Min(0)]
            public int maximumSpawnRow = 12;
            [Min(0f), Tooltip("Distance beyond the camera's right edge, in cells.")]
            public float spawnOffset = 2f;
            public Color spawnGizmoColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        }

        [Serializable]
        public sealed class CoinsData
        {
            public CoinPickup coinPrefab;
            [Min(0f)]
            public float initialDelay = 1f;
            [Min(0.1f)]
            public float minimumSpawnInterval = 3f;
            [Min(0.1f)]
            public float maximumSpawnInterval = 5f;
            [Min(0)]
            public int minimumSpawnRow = 3;
            [Min(0)]
            public int maximumSpawnRow = 12;
            [Min(0f), Tooltip("Distance beyond the camera's right edge, in tiles.")]
            public float spawnOffset = 2f;
            [Min(0f)]
            public float lineWeight = 50f;
            [Min(0f)]
            public float singleOrPairWeight = 25f;
            [Min(0f)]
            public float tetrominoWeight = 15f;
            [Min(1)]
            public int minimumLineLength = 3;
            [Min(1)]
            public int maximumLineLength = 6;
        }

        private void OnValidate()
        {
            minimumGroupGapInTiles = Mathf.Max(0f, minimumGroupGapInTiles);
            pieces.spawnRow = Mathf.Max(0, pieces.spawnRow);
            pieces.hazardFreePieces = Mathf.Max(0, pieces.hazardFreePieces);
            pieces.spawnInset = Mathf.Max(0f, pieces.spawnInset);
            pieces.spawnRightOffset = Mathf.Max(0f, pieces.spawnRightOffset);
            difficulty.progressionDuration = Mathf.Max(0.01f, difficulty.progressionDuration);
            difficulty.initialOneHazardChance = Mathf.Clamp01(difficulty.initialOneHazardChance);
            difficulty.initialTwoHazardChance = Mathf.Clamp(difficulty.initialTwoHazardChance, 0f, 1f - difficulty.initialOneHazardChance);
            difficulty.finalOneHazardChance = Mathf.Clamp01(difficulty.finalOneHazardChance);
            difficulty.finalTwoHazardChance = Mathf.Clamp(difficulty.finalTwoHazardChance, 0f, 1f - difficulty.finalOneHazardChance);
            difficulty.maximumSpeedMultiplier = Mathf.Max(1f, difficulty.maximumSpeedMultiplier);
            hazardClusters.minimumSpawnInterval = Mathf.Max(0.1f, hazardClusters.minimumSpawnInterval);
            hazardClusters.maximumSpawnInterval = Mathf.Max(hazardClusters.minimumSpawnInterval, hazardClusters.maximumSpawnInterval);
            hazardClusters.maximumSpawnRow = Mathf.Max(hazardClusters.minimumSpawnRow, hazardClusters.maximumSpawnRow);
            coins.minimumSpawnInterval = Mathf.Max(0.1f, coins.minimumSpawnInterval);
            coins.maximumSpawnInterval = Mathf.Max(coins.minimumSpawnInterval, coins.maximumSpawnInterval);
            coins.maximumSpawnRow = Mathf.Max(coins.minimumSpawnRow, coins.maximumSpawnRow);
            coins.minimumLineLength = Mathf.Max(1, coins.minimumLineLength);
            coins.maximumLineLength = Mathf.Max(coins.minimumLineLength, coins.maximumLineLength);
        }
    }
}
