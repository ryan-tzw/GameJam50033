using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Neverland/Settings/Gameplay")]
    public sealed class GameplaySettings : ScriptableObject
    {
        [Header("Stage")]
        public StageData stage = new();
        [Header("Bridge")]
        public BridgeData bridge = new();
        [Header("Collapse")]
        public CollapseData collapse = new();
        [Header("Tetromino")]
        public TetrominoData tetromino = new();
        [Header("Scoring")]
        public ScoringData scoring = new();
        [Header("Entry Zone")]
        public EntryZoneData entryZone = new();

        [Serializable]
        public sealed class StageData
        {
            [Min(1)]
            public int columns = 20;
            [Min(1)]
            public int rows = 10;
            [Min(0f)]
            public float screenPadding = 0.5f;
        }

        [Serializable]
        public sealed class BridgeData
        {
            [Min(0f), Tooltip("Normal leftward speed in stage cells per second.")]
            public float baseSpeed = 0.5f;
            [Min(0f), Tooltip("Maximum leftward speed when the bridge extends far ahead, in cells per second.")]
            public float maxSpeed = 3f;
            [InspectorName("Speed Up Start X"), Tooltip("Fixed X relative to the stage centre, in Unity units. Acceleration starts when the bridge's right edge passes this line.")]
            public float lowerLeadThreshold = 0f;
            [InspectorName("Maximum Speed X"), Tooltip("Fixed X relative to the stage centre, in Unity units. The target speed reaches maximum when the bridge's right edge reaches this line.")]
            public float upperLeadThreshold = 4f;
            [Min(0.01f), Tooltip("Approximate time in seconds to approach the target scrolling speed.")]
            public float smoothingTime = 1f;
        }

        [Serializable]
        public sealed class CollapseData
        {
            [Tooltip("X relative to the stage centre, in Unity units. Shaking starts when the piece's right edge reaches this line.")]
            public float warningX = -5.65f;
            [Tooltip("X relative to the stage centre, in Unity units. The piece falls when its right edge reaches this line. Must be left of Warning X.")]
            public float collapseX = -7.27f;
            [Min(0f), Tooltip("Seconds after the run starts before bridge shaking and collapse can begin.")]
            public float initialCollapseDelay = 2f;
            [Min(0f), Tooltip("Visual displacement at the warning line, in cells. Colliders stay still.")]
            public float initialShakeStrength = 0.01f;
            [Min(0f), Tooltip("Maximum visual displacement at the collapse line, in cells.")]
            public float shakeStrength = 0.06f;
            [Min(0f), Tooltip("Shake cycles per second.")]
            public float shakeFrequency = 25f;
            [Min(0.01f), Tooltip("Downward acceleration in cells per second squared.")]
            public float gravity = 30f;
            [Min(0f), Tooltip("Initial downward speed in cells per second.")]
            public float fallSpeed = 1.5f;
            [Min(0f), Tooltip("Maximum random horizontal speed in cells per second.")]
            public float horizontalDrift = 0.7f;
            [Min(0f), Tooltip("Maximum random spin in degrees per second.")]
            public float rotationSpeed = 120f;
        }

        [Serializable]
        public sealed class TetrominoData
        {
            [Min(0f), Tooltip("Extra leftward speed relative to the bridge, in cells per second.")]
            public float relativeMoveSpeed = 2f;
            [Min(1f), Tooltip("Speed multiplier while A is held.")]
            public float speedUpMultiplier = 3f;
            [Tooltip("Instantly place at the ghost position, or discard if no placement is available.")]
            public Key hardDropKey = Key.Space;
            [Min(0f), Tooltip("Seconds before held Up/Down or W/S starts repeating.")]
            public float verticalRepeatDelay = 0.25f;
            [Min(0.01f), Tooltip("Seconds between repeated vertical moves while a direction is held.")]
            public float verticalRepeatInterval = 0.08f;
        }

        [Serializable]
        public sealed class ScoringData
        {
            [Min(1), Tooltip("Points awarded for each full stage tile of forward progress.")]
            public int pointsPerTile = 10;
            [Min(1), InspectorName("Coin Points")]
            public int points = 50;
        }

        [Serializable]
        public sealed class EntryZoneData
        {
            [Min(0f), Tooltip("Width of the entry zone, measured in tiles inward from the stage's right edge. Larger values move the threshold left.")]
            public float tilesFromRight = 5f;
        }

        private void OnValidate()
        {
            stage.columns = Mathf.Max(1, stage.columns);
            stage.rows = Mathf.Max(1, stage.rows);
            stage.screenPadding = Mathf.Max(0f, stage.screenPadding);
            bridge.baseSpeed = Mathf.Max(0f, bridge.baseSpeed);
            bridge.maxSpeed = Mathf.Max(bridge.baseSpeed, bridge.maxSpeed);
            bridge.upperLeadThreshold = Mathf.Max(bridge.lowerLeadThreshold + 0.01f, bridge.upperLeadThreshold);
            bridge.smoothingTime = Mathf.Max(0.01f, bridge.smoothingTime);
            collapse.collapseX = Mathf.Min(collapse.collapseX, collapse.warningX - 0.01f);
            collapse.initialCollapseDelay = Mathf.Max(0f, collapse.initialCollapseDelay);
            collapse.initialShakeStrength = Mathf.Max(0f, collapse.initialShakeStrength);
            collapse.shakeStrength = Mathf.Max(collapse.initialShakeStrength, collapse.shakeStrength);
            tetromino.relativeMoveSpeed = Mathf.Max(0f, tetromino.relativeMoveSpeed);
            tetromino.speedUpMultiplier = Mathf.Max(1f, tetromino.speedUpMultiplier);
            tetromino.verticalRepeatDelay = Mathf.Max(0f, tetromino.verticalRepeatDelay);
            tetromino.verticalRepeatInterval = Mathf.Max(0.01f, tetromino.verticalRepeatInterval);
            scoring.pointsPerTile = Mathf.Max(1, scoring.pointsPerTile);
            scoring.points = Mathf.Max(1, scoring.points);
        }
    }
}
