using UnityEngine;

namespace Jam50033
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class RunDifficulty : MonoBehaviour
    {
        [SerializeField] private SpawnSettings spawnSettings;
        private static readonly SpawnSettings.DifficultyData defaultSpawnDifficulty = new();
        private SpawnSettings.DifficultyData spawnDifficulty => spawnSettings != null ? spawnSettings.difficulty : defaultSpawnDifficulty;

        [Header("Run State")]
        [SerializeField, Min(0f), Tooltip("Survival time in seconds. Resets each run; edit during Play mode to test later difficulty.")]
        private float elapsedTime;

        public float Factor => Mathf.SmoothStep(0f, 1f, elapsedTime / Mathf.Max(0.01f, spawnDifficulty.progressionDuration));
        public float SpeedMultiplier => Mathf.Lerp(1f, spawnDifficulty.maximumSpeedMultiplier, Factor);
        // Components are the probabilities for zero, one and two hazards, respectively.
        public Vector3 HazardProbabilities
        {
            get
            {
                float factor = Factor;
                float one = Mathf.Clamp01(Mathf.Lerp(spawnDifficulty.initialOneHazardChance, spawnDifficulty.finalOneHazardChance, factor));
                float two = Mathf.Clamp(Mathf.Lerp(spawnDifficulty.initialTwoHazardChance, spawnDifficulty.finalTwoHazardChance, factor), 0f, 1f - one);
                return new Vector3(1f - one - two, one, two);
            }
        }

        private void Awake()
        {
            elapsedTime = 0f;
        }

        private void Update()
        {
            if (Time.timeScale > 0f)
            {
                elapsedTime += Time.deltaTime;
            }
        }

        public int RollHazardCount()
        {
            Vector3 probabilities = HazardProbabilities;
            float roll = Random.value;
            if (probabilities.y > 0f && roll <= probabilities.y)
            {
                return 1;
            }

            if (probabilities.z > 0f && roll <= probabilities.y + probabilities.z)
            {
                return 2;
            }

            return 0;
        }

        private void OnValidate()
        {
            elapsedTime = Mathf.Max(0f, elapsedTime);
        }
    }
}
