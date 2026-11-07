using System;
using UnityEngine;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Neverland/Settings/Player")]
    public sealed class PlayerSettings : ScriptableObject
    {
        [Header("Movement")]
        public MovementData movement = new();
        [Header("Hurt")]
        public HurtData hurt = new();
        [Header("Death")]
        public DeathData death = new();
        [Header("Death Burst")]
        public DeathBurstData deathBurst = new();
        [Header("Dust")]
        public DustData dust = new();
        [Header("Fall Death")]
        public FallDeathData fallDeath = new();

        [Serializable]
        public sealed class MovementData
        {
            [Min(0.1f)]
            public float heightInCells = 2f;
            [Min(0.1f)]
            public float widthInCells = 0.8f;
            [Min(0f), Tooltip("Height removed from the top of the collider, in cells. Keeps the feet and sprite size unchanged.")]
            public float headClearance = 0.1f;
            [Tooltip("Desired horizontal world position. The bridge scrolls beneath this position.")]
            public float targetX;
            [Min(0f), Tooltip("Horizontal recovery velocity per unit of displacement.")]
            public float followStrength = 6f;
            [Min(0f), Tooltip("Maximum horizontal recovery speed, in stage cells per second.")]
            public float maxRecoverySpeed = 4f;
            [Min(0f), Tooltip("Horizontal acceleration toward bridge velocity while falling, in Unity units per second squared. Target X catch-up pauses until landing.")]
            public float airDriftAcceleration = 2f;
            [Min(0.001f), Tooltip("Distance below the capsule to check for support, in cells.")]
            public float groundProbeDistance = 0.08f;
            public LayerMask groundLayers = ~0;
            [Header("Climbing")]
            [Min(0.1f), Tooltip("Upward speed while the upper half touches the wall, in stage cells per second.")]
            public float climbSpeed = 2.5f;
            [Min(0.1f), Tooltip("Upward speed when only the lower half touches the wall, in stage cells per second.")]
            public float stepSpeed = 4f;
            [Range(0.01f, 0.5f), Tooltip("Distance ahead to detect a climbable wall, in cells.")]
            public float wallProbeDistance = 0.15f;
            [Range(0, 1), Tooltip("Empty wall tiles the player can climb past. Set to 0 to require a continuous wall.")]
            public int climbGapAllowance = 1;
            [Header("Landing")]
            [Min(0.1f), Tooltip("Minimum free-fall distance that causes a landing stagger, in stage cells.")]
            public float staggerFallDistance = 3f;
            [Min(0.01f), Tooltip("Seconds spent recovering while moving with the bridge.")]
            public float staggerDuration = 0.375f;
            public AnimationClip staggerClip;
            [Header("Momentum")]
            [Range(0f, 1f)]
            public float initialMomentum;
            [Min(0.1f), Tooltip("Seconds of flat running to build from zero to full momentum.")]
            public float momentumBuildTime = 5f;
            [Min(0f), Tooltip("Momentum lost per second while climbing or stepping.")]
            public float climbMomentumDecay = 0.35f;
            [Min(0f)]
            public float fallMomentumDecay = 0.25f;
            [Min(0f)]
            public float staggerMomentumDecay = 0.5f;
            [Range(0f, 1f)]
            public float landingMomentumPenalty = 0.3f;
            [Range(0f, 1f), Tooltip("Additional bridge speed at full momentum while running. 0.1 means 10%.")]
            public float maximumMomentumSpeedBonus = 0.1f;
            [Header("Running Animation")]
            [Min(0.01f), Tooltip("Actual bridge scrolling speed, in cells per second, that switches to fast running and dust.")]
            public float fastRunStartSpeed = 1.2f;
            [Min(0f), Tooltip("Switch back to the slow run below this speed, in cells per second.")]
            public float fastRunStopSpeed = 1.1f;
            public Sprite standingSprite;
        }

        [Serializable]
        public sealed class HurtData
        {
            public AnimationClip hurtClip;
            public Material flashMaterial;
            [Min(0.01f), Tooltip("How long to hold the hit pose before finishing the reaction.")]
            public float hurtDuration = 0.375f;
            [Min(0.01f)]
            public float whiteFlashDuration = 0.1f;
            [Min(0f), Tooltip("Maximum sprite displacement as a fraction of the player's collider height.")]
            public float shakeStrength = 0.025f;
            [Min(0f), Tooltip("Shake cycles per second.")]
            public float shakeFrequency = 25f;
            [Min(0f), Tooltip("Distance away from the hazard, in stage cells.")]
            public float knockbackDistance = 1.5f;
            [Min(0.01f), Tooltip("Seconds to complete knockback, capped by the hurt reaction duration.")]
            public float knockbackDuration = 0.15f;
        }

        [Serializable]
        public sealed class DeathData
        {
            public Material flashMaterial;
            public PlayerDeathBurst burstPrefab;
            [Min(0f)]
            public float flashDuration = 0.035f;
        }

        [Serializable]
        public sealed class DeathBurstData
        {
            [Range(4, 64)]
            public int particleCount = 18;
            [Min(0.01f)]
            public float duration = 0.35f;
            [Min(0.1f), Tooltip("Final burst radius in player heights. A large radius keeps offscreen deaths visible.")]
            public float radius = 2.5f;
            [Range(0.01f, 1f), Tooltip("Final speed relative to initial speed.")]
            public float endSpeedFraction = 0.08f;
            [Range(0f, 0.3f)]
            public float speedVariation = 0.1f;
            [Min(0.01f), Tooltip("Smallest square size in player heights.")]
            public float minimumSize = 0.14f;
            [Min(0.01f), Tooltip("Largest square size in player heights.")]
            public float maximumSize = 0.22f;
            public Color[] palette =
        {
            Color.white,
            new Color32(219, 205, 190, 255),
            new Color32(74, 109, 150, 255),
            new Color32(130, 184, 208, 255),
            new Color32(241, 212, 190, 255),
            new Color32(43, 47, 95, 255)
        };
        }

        [Serializable]
        public sealed class DustData
        {
            [InspectorName("Burst Frames")]
            public Sprite[] frames;
            [Tooltip("Mirror the dust horizontally to change its facing direction.")]
            public bool flipX = true;
            [Min(1f)]
            public float framesPerSecond = 12f;
            [Min(0.1f), Tooltip("Height of the initial puff in stage cells.")]
            public float burstSize = 0.8f;
            [Tooltip("Puff centre relative to the player's feet, in stage cells.")]
            public Vector2 offsetInCells = new Vector2(-0.35f, 0.15f);
            [Range(0f, 1f)]
            public float opacity = 0.7f;
            public Color spawnGizmoColor = Color.cyan;
            [Min(0.01f), Tooltip("Spawn marker radius in stage cells.")]
            public float spawnGizmoSize = 0.12f;
        }

        [Serializable]
        public sealed class FallDeathData
        {
            [Min(0f), Tooltip("Extra distance below the camera, in world units, before the run ends.")]
            public float fallMargin = 0.25f;
        }

        private void OnValidate()
        {
            movement.fastRunStartSpeed = Mathf.Max(0.01f, movement.fastRunStartSpeed);
            movement.fastRunStopSpeed = Mathf.Clamp(movement.fastRunStopSpeed, 0f, movement.fastRunStartSpeed - 0.01f);
            movement.heightInCells = Mathf.Max(0.1f, movement.heightInCells);
            movement.widthInCells = Mathf.Clamp(movement.widthInCells, 0.1f, movement.heightInCells);
            movement.headClearance = Mathf.Clamp(movement.headClearance, 0f, movement.heightInCells - movement.widthInCells);
            movement.followStrength = Mathf.Max(0f, movement.followStrength);
            movement.maxRecoverySpeed = Mathf.Max(0f, movement.maxRecoverySpeed);
            movement.airDriftAcceleration = Mathf.Max(0f, movement.airDriftAcceleration);
            movement.groundProbeDistance = Mathf.Max(0.001f, movement.groundProbeDistance);
            movement.climbSpeed = Mathf.Max(0.1f, movement.climbSpeed);
            movement.stepSpeed = Mathf.Max(0.1f, movement.stepSpeed);
            movement.wallProbeDistance = Mathf.Clamp(movement.wallProbeDistance, 0.01f, 0.5f);
            movement.staggerFallDistance = Mathf.Max(0.1f, movement.staggerFallDistance);
            movement.staggerDuration = Mathf.Max(0.01f, movement.staggerDuration);
            deathBurst.maximumSize = Mathf.Max(deathBurst.minimumSize, deathBurst.maximumSize);
        }
    }
}
