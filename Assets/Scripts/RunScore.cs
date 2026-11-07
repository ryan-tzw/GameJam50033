using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class RunScore : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private PlayerController player;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text finalScoreLabel;
        [SerializeField, Min(1), Tooltip("Points awarded for each full stage tile of forward progress.")]
        private int pointsPerTile = 10;

        private float startingColumn;
        private int tilesPassed;
        private bool finished;
        private int bonusPoints;

        public int Score => tilesPassed * pointsPerTile + bonusPoints;

        private void Start()
        {
            startingColumn = GetPlayerColumn();
            tilesPassed = 0;
            bonusPoints = 0;
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
                scoreLabel.text = text;
            }

            if (finalScoreLabel != null)
            {
                finalScoreLabel.text = text;
            }
        }

        private void OnValidate()
        {
            pointsPerTile = Mathf.Max(1, pointsPerTile);
        }
    }
}
