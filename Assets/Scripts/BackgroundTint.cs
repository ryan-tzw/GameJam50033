using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BackgroundTint : MonoBehaviour
    {
        [SerializeField] private Color tintColour = new Color(0.12f, 0.16f, 0.28f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("0 shows the original artwork; 1 applies the full tint colour.")]
        private float tintStrength = 0.25f;

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
            ApplyTint(tintStrength);
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
            ApplyTint(isActiveAndEnabled ? tintStrength : 0f);
        }

        private void ApplyTint(float strength)
        {
            if (layers == null)
            {
                return;
            }

            Color tint = Color.Lerp(Color.white, tintColour, strength);
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
