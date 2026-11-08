using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Brick))]
    public sealed class HazardBrickVisuals : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.HazardData defaultVisualHazard = new();
        private VisualSettings.HazardData visualHazard => visualSettings != null ? visualSettings.hazard : defaultVisualHazard;

        private SpriteRenderer spriteRenderer;
        [SerializeField, HideInInspector] private Material originalMaterial;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock originalProperties;
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
            if (spriteRenderer.sharedMaterial != visualHazard.hazardMaterial)
            {
                originalMaterial = spriteRenderer.sharedMaterial;
            }
            properties = new MaterialPropertyBlock();
            originalProperties = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(originalProperties);
            hitFlashRemaining = 0f;
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            if (Application.IsPlaying(gameObject))
            {
                hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Time.unscaledDeltaTime);
            }

            ApplyVisuals();
        }

        public void FlashOnHit()
        {
            hitFlashRemaining = visualHazard.hitFlashDuration;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer == null || visualHazard.hazardMaterial == null || properties == null)
            {
                return;
            }

            spriteRenderer.sharedMaterial = visualHazard.hazardMaterial;
            spriteRenderer.GetPropertyBlock(properties);
            // A shared scaled clock synchronizes newly spawned hazards and freezes during pauses.
            float phase = Application.IsPlaying(gameObject)
                ? Mathf.Repeat(Time.time * visualHazard.pulseSpeed, 1f)
                : 0f;
            float pulse = 0.5f - Mathf.Cos(phase * Mathf.PI * 2f) * 0.5f;
            float flash = Mathf.Clamp01(hitFlashRemaining / Mathf.Max(0.01f, visualHazard.hitFlashDuration));
            properties.SetColor(GlowColor, Color.Lerp(visualHazard.outlineColor, Color.white, flash));
            properties.SetColor(BaseColor, visualHazard.baseColor);
            properties.SetColor(MarkColor, visualHazard.exclamationColor);
            properties.SetFloat(OutlineWidth, visualHazard.outlineWidth);
            float brightness = Mathf.Lerp(visualHazard.minimumBrightness, visualHazard.maximumBrightness, pulse);
            properties.SetFloat(GlowStrength, Mathf.Lerp(brightness, visualHazard.maximumBrightness, flash));
            properties.SetFloat(SymbolSize, visualHazard.symbolSize);
            properties.SetColor(SymbolColor, Color.Lerp(visualHazard.symbolColor, Color.white, flash));
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
            ApplyVisuals();
        }
    }
}
