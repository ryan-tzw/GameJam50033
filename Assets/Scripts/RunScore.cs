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
        private int displayTarget;
        private OdometerScore odometer;
        private bool UseCommaSeparators => visualSettings == null || visualSettings.scorePanel.useCommaSeparators;

        public bool HasFlyingCoins => coinAnimation != null && coinAnimation.HasFlyingCoins;
        public bool HasPendingScoreFeedback => HasFlyingCoins || (odometer != null && odometer.IsAnimating);

        public int Score => tilesPassed * gameplayScoring.pointsPerTile + bonusPoints;

        private void OnEnable()
        {
            // Keep pickups connected when an existing scene gains the animation child.
            if (coinAnimation == null)
            {
                coinAnimation = GetComponentInChildren<CoinScoreAnimation>(true);
            }
            if (Application.IsPlaying(gameObject) && scoreLabel != null)
            {
                odometer = scoreLabel.GetComponent<OdometerScore>();
                if (odometer == null) odometer = scoreLabel.gameObject.AddComponent<OdometerScore>();
                odometer.Configure(visualSettings);
            }
        }

        private void Start()
        {
            startingColumn = GetPlayerColumn();
            tilesPassed = 0;
            bonusPoints = 0;
            pendingCoinPoints = 0;
            displayTarget = 0;
            odometer?.SetValue(0, UseCommaSeparators, instant: true);
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
                int previous = tilesPassed;
                tilesPassed = passed;
                AwardMilestones(previous, passed);
                RefreshLabels();
            }
        }

        private void AwardMilestones(int previous, int passed)
        {
            if (gameplayScoring.milestones == null) return;
            // Process every crossed tile, including several milestones in a single update.
            for (int distance = previous + 1; distance <= passed; distance++)
            {
                GameplaySettings.MilestoneData best = null;
                foreach (GameplaySettings.MilestoneData tier in gameplayScoring.milestones)
                {
                    if (tier == null || tier.intervalInTiles <= 0 || tier.bonus <= 0 || distance % tier.intervalInTiles != 0) continue;
                    if (best == null || tier.intervalInTiles > best.intervalInTiles ||
                        (tier.intervalInTiles == best.intervalInTiles && tier.bonus > best.bonus)) best = tier;
                }
                if (best == null) continue;
                AwardMilestone(distance, best);
            }
        }

        private void AwardMilestone(int distance, GameplaySettings.MilestoneData tier)
        {
            bonusPoints += tier.bonus;
            coinAnimation?.ShowMilestone(distance, tier.bonus, tier.emphasize);
            if (audioSettings != null) PlayCollectibleSound(audioSettings.milestoneClip, audioSettings.milestoneVolume);
        }

#if UNITY_EDITOR
        public GameplaySettings.MilestoneData[] TestMilestones => gameplayScoring.milestones;
        public bool CanTestMilestones => Application.IsPlaying(gameObject) && isActiveAndEnabled && !finished && Time.timeScale > 0f;

        public void TestMilestone(int index)
        {
            if (!CanTestMilestones || TestMilestones == null || index < 0 || index >= TestMilestones.Length) return;
            GameplaySettings.MilestoneData tier = TestMilestones[index];
            if (tier == null || tier.intervalInTiles <= 0 || tier.bonus <= 0) return;
            // Repeatable feedback testing leaves real distance and automatic milestone tracking untouched.
            AwardMilestone(tier.intervalInTiles, tier);
            RefreshLabels();
        }
#endif

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
            displayTarget = Score - pendingCoinPoints;
            // Retain this Text as the shared font, layout and coin-flight anchor; reels render its value.
            if (scoreLabel != null) scoreLabel.text = displayTarget.ToString(format, CultureInfo.InvariantCulture);
            odometer?.SetValue(displayTarget, UseCommaSeparators);

            if (finalScoreLabel != null)
            {
                finalScoreLabel.text = text;
            }
        }

    }
}
