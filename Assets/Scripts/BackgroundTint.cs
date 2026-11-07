using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BackgroundTint : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.BackgroundData defaultVisualBackground = new();
        private VisualSettings.BackgroundData visualBackground => visualSettings != null ? visualSettings.background : defaultVisualBackground;

        private SpriteRenderer[] layers;

        private void OnEnable()
        {
            RefreshLayers();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshLayers();
        }

        private void LateUpdate()
        {
            ApplyTint(visualBackground.tintStrength);
        }

        private void OnDisable()
        {
            ApplyTint(0f);
        }

        private void OnValidate()
        {
            RefreshLayers();
        }

        private void RefreshLayers()
        {
            layers = GetComponentsInChildren<SpriteRenderer>(true);
            ApplyTint(isActiveAndEnabled ? visualBackground.tintStrength : 0f);
        }

        private void ApplyTint(float strength)
        {
            if (layers == null)
            {
                return;
            }

            Color tint = Color.Lerp(Color.white, visualBackground.tintColour, strength);
            foreach (SpriteRenderer layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                // ParallaxLayer owns opacity; tint only changes the background's RGB channels.
                tint.a = layer.color.a;
                layer.color = tint;
            }
        }
    }
}
