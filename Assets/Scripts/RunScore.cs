using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class RunScore : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private AudioSettings audioSettings;
        private AudioSource collectibleAudio;
        private static readonly GameplaySettings.ScoringData defaultGameplayScoring = new();
        private GameplaySettings.ScoringData gameplayScoring => gameplaySettings != null ? gameplaySettings.scoring : defaultGameplayScoring;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text finalScoreLabel;

        [SerializeField] private CoinScoreAnimation coinAnimation;

        private float startingColumn;
        private int tilesPassed;
        private bool finished;
        private int bonusPoints;
        private int pendingCoinPoints;
        private bool formattedWithCommas;
        private bool UseCommaSeparators => visualSettings == null || visualSettings.scorePanel.useCommaSeparators;

        public bool HasFlyingCoins => coinAnimation != null && coinAnimation.HasFlyingCoins;

        public int Score => tilesPassed * gameplayScoring.pointsPerTile + bonusPoints;

        private void OnEnable()
        {
            // Keep pickups connected when an existing scene gains the animation child.
            if (coinAnimation == null)
            {
                coinAnimation = GetComponentInChildren<CoinScoreAnimation>(true);
            }
        }

        private void Start()
        {
            startingColumn = GetPlayerColumn();
            tilesPassed = 0;
            bonusPoints = 0;
            pendingCoinPoints = 0;
            finished = false;
            RefreshLabels();
        }

        private void LateUpdate()
        {
            if (formattedWithCommas != UseCommaSeparators)
            {
                RefreshLabels();
            }
            if (!finished && Time.timeScale > 0f && player != null && player.isActiveAndEnabled)
            {
                RecordProgress();
            }
        }

        private float GetPlayerColumn()
        {
            if (stageArea == null || player == null || stageArea.CellSize <= 0f)
            {
                return 0f;
            }

            float localX = stageArea.transform.InverseTransformPoint(player.transform.position).x;
            return (localX - stageArea.BridgeOffset) / stageArea.CellSize;
        }

        private void RecordProgress()
        {
            int passed = Mathf.Max(0, Mathf.FloorToInt(GetPlayerColumn() - startingColumn));
            // Keep the furthest progress so being pushed back and catching up cannot score twice.
            if (passed > tilesPassed)
            {
                tilesPassed = passed;
                RefreshLabels();
            }
        }

        public void AddPoints(int points)
        {
            if (finished || Time.timeScale <= 0f || points <= 0)
            {
                return;
            }

            bonusPoints += points;
            RefreshLabels();
        }

        public void CollectCoin(int points, Vector3 position, float height, Sprite[] frames, float fps,
            bool scatter = false, float attractionDelay = 0f, bool worldPickup = false)
        {
            if (finished || Time.timeScale <= 0f || points <= 0)
            {
                return;
            }

            // Secure the reward immediately; only its HUD presentation waits for arrival.
            bonusPoints += points;
            if (worldPickup && audioSettings != null)
            {
                PlayCollectibleSound(audioSettings.collectibles.pickupClip, audioSettings.collectibles.pickupVolume);
            }
            if (coinAnimation != null && coinAnimation.TryFly(position, height, frames, fps, points,
                arrived => RevealCoinPoints(points, arrived), scatter, attractionDelay))
            {
                pendingCoinPoints += points;
            }
            RefreshLabels();
        }

        private void RevealCoinPoints(int points, bool arrived)
        {
            pendingCoinPoints = Mathf.Max(0, pendingCoinPoints - points);
            RefreshLabels();
            if (arrived && audioSettings != null)
            {
                PlayCollectibleSound(audioSettings.collectibles.arrivalClip, audioSettings.collectibles.arrivalVolume);
            }
        }

        public void PlayChestOpenSound()
        {
            if (audioSettings != null)
            {
                PlayCollectibleSound(audioSettings.collectibles.chestOpenClip, audioSettings.collectibles.chestOpenVolume);
            }
        }

        private void PlayCollectibleSound(AudioClip clip, float volume)
        {
            if (clip == null || volume <= 0f) return;
            // Keep playback on the score object: the collected world coin is destroyed immediately.
            if (collectibleAudio == null)
            {
                collectibleAudio = gameObject.AddComponent<AudioSource>();
                collectibleAudio.playOnAwake = false;
                collectibleAudio.spatialBlend = 0f;
            }
            collectibleAudio.outputAudioMixerGroup = audioSettings.effectsMixer;
            collectibleAudio.PlayOneShot(clip, volume);
        }

        public void FinishRun()
        {
            if (!finished)
            {
                RecordProgress();
                finished = true;
            }

            // Refresh again when the previously hidden game-over UI opens.
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            formattedWithCommas = UseCommaSeparators;
            string format = formattedWithCommas ? "N0" : "0";
            string text = Score.ToString(format, CultureInfo.InvariantCulture);
            if (scoreLabel != null)
            {
                scoreLabel.text = (Score - pendingCoinPoints).ToString(format, CultureInfo.InvariantCulture);
            }

            if (finalScoreLabel != null)
            {
                finalScoreLabel.text = text;
            }
        }

    }
}
