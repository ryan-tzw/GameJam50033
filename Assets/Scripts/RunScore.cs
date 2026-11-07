using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class RunScore : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
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
            bool scatter = false, float attractionDelay = 0f)
        {
            if (finished || Time.timeScale <= 0f || points <= 0)
            {
                return;
            }

            // Secure the reward immediately; only its HUD presentation waits for arrival.
            bonusPoints += points;
            if (coinAnimation != null && coinAnimation.TryFly(position, height, frames, fps, points,
                () => RevealCoinPoints(points), scatter, attractionDelay))
            {
                pendingCoinPoints += points;
            }
            RefreshLabels();
        }

        private void RevealCoinPoints(int points)
        {
            pendingCoinPoints = Mathf.Max(0, pendingCoinPoints - points);
            RefreshLabels();
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
            string text = Score.ToString();
            if (scoreLabel != null)
            {
                scoreLabel.text = (Score - pendingCoinPoints).ToString();
            }

            if (finalScoreLabel != null)
            {
                finalScoreLabel.text = text;
            }
        }

    }
}
