using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Brick))]
    public sealed class HazardBrickVisuals : MonoBehaviour
    {
        [SerializeField] private Material hazardMaterial;
        [SerializeField] private Color baseColor = new Color32(0xE6, 0x5C, 0x63, 0xFF);
        [Header("Pulsing Glow")]
        [SerializeField, ColorUsage(false, true)] private Color outlineColor = new Color(1f, 0.45f, 0.08f);
        [SerializeField, Range(0.005f, 0.15f), Tooltip("Outline thickness as a fraction of one tile.")]
        private float outlineWidth = 0.055f;
        [SerializeField, Min(0f)] private float minimumBrightness = 1.25f;
        [SerializeField, Min(0f)] private float maximumBrightness = 3.5f;
        [SerializeField, Min(0f), Tooltip("Complete pulses per second.")]
        private float pulseSpeed = 1.5f;
        [Header("Warning Symbol")]
        [SerializeField, Range(0.2f, 1f)] private float symbolSize = 0.95f;
        [SerializeField, ColorUsage(true, true)] private Color symbolColor = new Color(1f, 0.8f, 0.15f);
        [SerializeField] private Color exclamationColor = new Color(0.03f, 0.015f, 0.02f);
        [Header("Hit Flash")]
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;

        private SpriteRenderer spriteRenderer;
        [SerializeField, HideInInspector] private Material originalMaterial;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock originalProperties;
        private float phase;
        private float hitFlashRemaining;

        private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int MarkColor = Shader.PropertyToID("_MarkColor");
        private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int GlowStrength = Shader.PropertyToID("_GlowStrength");
        private static readonly int SymbolSize = Shader.PropertyToID("_SymbolSize");
        private static readonly int SymbolColor = Shader.PropertyToID("_SymbolColor");

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sharedMaterial != hazardMaterial)
            {
                originalMaterial = spriteRenderer.sharedMaterial;
            }
            properties = new MaterialPropertyBlock();
            originalProperties = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(originalProperties);
            phase = 0f;
            hitFlashRemaining = 0f;
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            if (Application.IsPlaying(gameObject))
            {
                hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Time.unscaledDeltaTime);
            }

            if (Application.IsPlaying(gameObject) && Time.timeScale > 0f)
            {
                // Scaled time keeps the pulse still when game over pauses gameplay.
                phase = Mathf.Repeat(phase + Time.deltaTime * pulseSpeed, 1f);
            }

            ApplyVisuals();
        }

        public void FlashOnHit()
        {
            hitFlashRemaining = hitFlashDuration;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer == null || hazardMaterial == null || properties == null)
            {
                return;
            }

            spriteRenderer.sharedMaterial = hazardMaterial;
            spriteRenderer.GetPropertyBlock(properties);
            float pulse = 0.5f - Mathf.Cos(phase * Mathf.PI * 2f) * 0.5f;
            float flash = Mathf.Clamp01(hitFlashRemaining / Mathf.Max(0.01f, hitFlashDuration));
            properties.SetColor(GlowColor, Color.Lerp(outlineColor, Color.white, flash));
            properties.SetColor(BaseColor, baseColor);
            properties.SetColor(MarkColor, exclamationColor);
            properties.SetFloat(OutlineWidth, outlineWidth);
            float brightness = Mathf.Lerp(minimumBrightness, maximumBrightness, pulse);
            properties.SetFloat(GlowStrength, Mathf.Lerp(brightness, maximumBrightness, flash));
            properties.SetFloat(SymbolSize, symbolSize);
            properties.SetColor(SymbolColor, Color.Lerp(symbolColor, Color.white, flash));
            spriteRenderer.SetPropertyBlock(properties);
        }

        private void OnDisable()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sharedMaterial = originalMaterial;
                spriteRenderer.SetPropertyBlock(originalProperties);
            }
        }

        private void OnValidate()
        {
            minimumBrightness = Mathf.Max(0f, minimumBrightness);
            maximumBrightness = Mathf.Max(minimumBrightness, maximumBrightness);
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
            ApplyVisuals();
        }
    }
}
