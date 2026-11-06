using UnityEngine;

namespace Jam50033
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class RunDifficulty : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float progressionDuration = 120f;
        [Header("Initial Hazard Probabilities")]
        [SerializeField, Range(0f, 1f)] private float initialOneHazardChance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float initialTwoHazardChance = 0.05f;
        [Header("Final Hazard Probabilities")]
        [SerializeField, Range(0f, 1f)] private float finalOneHazardChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float finalTwoHazardChance = 0.15f;
        [Header("Scrolling Speed")]
        [SerializeField, Min(1f), Tooltip("Maximum multiplier on bridge scrolling. Leave at 1 for unchanged speeds; use 1.5 for the planned progression.")]
        private float maximumSpeedMultiplier = 1f;
        [Header("Run State")]
        [SerializeField, Min(0f), Tooltip("Survival time in seconds. Resets each run; edit during Play mode to test later difficulty.")]
        private float elapsedTime;

        public float Factor => Mathf.SmoothStep(0f, 1f, elapsedTime / Mathf.Max(0.01f, progressionDuration));
        public float SpeedMultiplier => Mathf.Lerp(1f, maximumSpeedMultiplier, Factor);
        // Components are the probabilities for zero, one and two hazards, respectively.
        public Vector3 HazardProbabilities
        {
            get
            {
                float factor = Factor;
                float one = Mathf.Clamp01(Mathf.Lerp(initialOneHazardChance, finalOneHazardChance, factor));
                float two = Mathf.Clamp(Mathf.Lerp(initialTwoHazardChance, finalTwoHazardChance, factor), 0f, 1f - one);
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
            progressionDuration = Mathf.Max(0.01f, progressionDuration);
            initialOneHazardChance = Mathf.Clamp01(initialOneHazardChance);
            initialTwoHazardChance = Mathf.Clamp(initialTwoHazardChance, 0f, 1f - initialOneHazardChance);
            finalOneHazardChance = Mathf.Clamp01(finalOneHazardChance);
            finalTwoHazardChance = Mathf.Clamp(finalTwoHazardChance, 0f, 1f - finalOneHazardChance);
            maximumSpeedMultiplier = Mathf.Max(1f, maximumSpeedMultiplier);
            elapsedTime = Mathf.Max(0f, elapsedTime);
        }
    }
}
