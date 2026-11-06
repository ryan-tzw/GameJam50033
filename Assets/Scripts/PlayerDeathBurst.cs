using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PlayerDeathBurst : MonoBehaviour
    {
        [SerializeField, Range(4, 64)] private int particleCount = 18;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        [SerializeField, Min(0.1f), Tooltip("Final burst radius in player heights. A large radius keeps offscreen deaths visible.")]
        private float radius = 2.5f;
        [SerializeField, Range(0.01f, 1f), Tooltip("Final speed relative to initial speed.")]
        private float endSpeedFraction = 0.08f;
        [SerializeField, Range(0f, 0.3f)] private float speedVariation = 0.1f;
        [SerializeField, Min(0.01f), Tooltip("Smallest square size in player heights.")]
        private float minimumSize = 0.14f;
        [SerializeField, Min(0.01f), Tooltip("Largest square size in player heights.")]
        private float maximumSize = 0.22f;
        [SerializeField] private Color[] palette =
        {
            Color.white,
            new Color32(219, 205, 190, 255),
            new Color32(74, 109, 150, 255),
            new Color32(130, 184, 208, 255),
            new Color32(241, 212, 190, 255),
            new Color32(43, 47, 95, 255)
        };

        private ParticleSystem particles;

        public bool IsAlive => particles != null && particles.IsAlive();

        public void Play(float playerHeight, int sortingLayer, int sortingOrder)
        {
            particles = GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = duration;
            main.startLifetime = duration;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.maxParticles = particleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, endSpeedFraction));
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            var color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            particles.Play();

            // The symmetric slowdown curve averages its two end speeds over the lifetime.
            float speed = radius * playerHeight / (duration * (1f + endSpeedFraction) * 0.5f);
            float phase = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < particleCount; i++)
            {
                float angle = phase + i * Mathf.PI * 2f / particleCount;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var particle = new ParticleSystem.EmitParams
                {
                    position = transform.position + direction * (playerHeight * 0.04f),
                    velocity = direction * (speed * Random.Range(1f - speedVariation, 1f + speedVariation)),
                    startLifetime = duration,
                    startSize = Random.Range(minimumSize, maximumSize) * playerHeight,
                    startColor = palette != null && palette.Length > 0 ? palette[i % palette.Length] : Color.white,
                    rotation = 0f
                };
                particles.Emit(particle, 1);
            }
        }

        private void OnValidate()
        {
            maximumSize = Mathf.Max(minimumSize, maximumSize);
        }
    }
}
