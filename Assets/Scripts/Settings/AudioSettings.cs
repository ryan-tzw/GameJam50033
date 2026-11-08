using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Neverland/Settings/Audio")]
    public sealed class AudioSettings : ScriptableObject
    {
        [Header("Menu")]
        public MenuData menu = new();
        [Header("Player")]
        public PlayerData player = new();
        [Header("Tetromino")]
        public TetrominoData tetromino = new();
        [Header("Collectibles")]
        public CollectibleData collectibles = new();
        [Header("Mixer Routing")]
        public AudioMixerGroup effectsMixer;
        public AudioMixerGroup musicMixer;
        [Header("Music")]
        public AudioClip titleMusic;
        [Range(0f, 1f)] public float titleMusicVolume = 0.7f;
        public AudioClip gameMusic;
        [Range(0f, 1f)] public float gameMusicVolume = 0.25f;
        [Range(0f, 1f), Tooltip("Music volume multiplier while the pause menu is open.")]
        public float pausedMusicMultiplier = 0.35f;
        [Header("Game Over Music")]
        public AudioClip gameOverMusic;
        [Range(0f, 1f)] public float gameOverMusicVolume = 0.4f;
        [Header("Score Milestones")]
        public AudioClip milestoneClip;
        [Range(0f, 1f)] public float milestoneVolume = 0.7f;

        [Serializable]
        public sealed class MenuData
        {
            public AudioClip selectClip;
            [Range(0f, 1f)] public float selectVolume = 0.7f;
            public AudioClip gameStartClip;
            [Range(0f, 1f)] public float gameStartVolume = 0.7f;
        }

        [Serializable]
        public sealed class CollectibleData
        {
            public AudioClip pickupClip;
            [Range(0f, 1f)] public float pickupVolume = 0.7f;
            public AudioClip arrivalClip;
            [Range(0f, 1f)] public float arrivalVolume = 0.7f;
            public AudioClip chestOpenClip;
            [Range(0f, 1f)] public float chestOpenVolume = 0.7f;
        }

        [Serializable]
        public sealed class PlayerData
        {
            [Header("Hit & Death")]
            public AudioClip hitSound;
            [Range(0f, 1f)]
            public float hitVolume = 1f;
            public AudioClip deathSound;
            [Range(0f, 1f)]
            public float deathVolume = 1f;
            [Header("Running")]
            public AudioClip stepSound;
            [Range(0f, 1f)]
            public float stepVolume = 0.3f;
            [Min(0.01f), Tooltip("Seconds between running footstep sounds. Larger values play less often.")]
            public float stepInterval = 0.33333334f;
            [Header("Climbing")]
            public AudioClip climbSound;
            [Range(0f, 1f)]
            public float climbVolume = 0.5f;
            [Min(0.01f), Tooltip("Seconds between climbing sounds while moving. Larger values play less often.")]
            public float climbInterval = 0.375f;
            [Header("Landing")]
            public AudioClip landingSound;
            [Range(0f, 1f)]
            public float landingVolume = 0.7f;
        }

        [Serializable]
        public sealed class TetrominoData
        {
            public AudioClip moveClip;
            [Range(0f, 1f)]
            public float moveVolume = 0.7f;
            public AudioClip rotateClip;
            [Range(0f, 1f)]
            public float rotateVolume = 0.7f;
            public AudioClip softDropClip;
            [Range(0f, 1f)]
            public float softDropVolume = 0.5f;
            public AudioClip hardDropClip;
            [Range(0f, 1f)]
            public float hardDropVolume = 0.8f;
            public AudioClip rejectClip;
            [Range(0f, 1f)]
            public float rejectVolume = 0.5f;
            public AudioClip[] connectClips = new AudioClip[0];
            [Range(0f, 1f)]
            public float connectVolume = 0.8f;
        }
        private void OnValidate()
        {
            player.stepInterval = Mathf.Max(0.01f, player.stepInterval);
            player.climbInterval = Mathf.Max(0.01f, player.climbInterval);
        }
    }
}
