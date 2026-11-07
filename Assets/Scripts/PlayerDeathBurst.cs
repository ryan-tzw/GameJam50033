using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PlayerDeathBurst : MonoBehaviour
    {
        [SerializeField] private PlayerSettings playerSettings;
        private static readonly PlayerSettings.DeathBurstData defaultPlayerDeathBurst = new();
        private PlayerSettings.DeathBurstData playerDeathBurst => playerSettings != null ? playerSettings.deathBurst : defaultPlayerDeathBurst;

        private ParticleSystem particles;

        public bool IsAlive => particles != null && particles.IsAlive();

        public void Play(float playerHeight, int sortingLayer, int sortingOrder)
        {
            particles = GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = playerDeathBurst.duration;
            main.startLifetime = playerDeathBurst.duration;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.maxParticles = playerDeathBurst.particleCount;
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
                AnimationCurve.EaseInOut(0f, 1f, 1f, playerDeathBurst.endSpeedFraction));
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
            float speed = playerDeathBurst.radius * playerHeight / (playerDeathBurst.duration * (1f + playerDeathBurst.endSpeedFraction) * 0.5f);
            float phase = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < playerDeathBurst.particleCount; i++)
            {
                float angle = phase + i * Mathf.PI * 2f / playerDeathBurst.particleCount;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var particle = new ParticleSystem.EmitParams
                {
                    position = transform.position + direction * (playerHeight * 0.04f),
                    velocity = direction * (speed * Random.Range(1f - playerDeathBurst.speedVariation, 1f + playerDeathBurst.speedVariation)),
                    startLifetime = playerDeathBurst.duration,
                    startSize = Random.Range(playerDeathBurst.minimumSize, playerDeathBurst.maximumSize) * playerHeight,
                    startColor = playerDeathBurst.palette != null && playerDeathBurst.palette.Length > 0 ? playerDeathBurst.palette[i % playerDeathBurst.palette.Length] : Color.white,
                    rotation = 0f
                };
                particles.Emit(particle, 1);
            }
        }

    }
}
