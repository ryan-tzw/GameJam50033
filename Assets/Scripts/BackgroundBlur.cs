using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BackgroundBlur : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.BackgroundData defaultVisualBackground = new();
        private VisualSettings.BackgroundData visualBackground => visualSettings != null ? visualSettings.background : defaultVisualBackground;

        private SpriteRenderer[] layers;
        private MaterialPropertyBlock properties;
        private static readonly int BlurRadius = Shader.PropertyToID("_BlurRadius");
        private static readonly int TileHorizontally = Shader.PropertyToID("_TileHorizontally");

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
            ApplyBlur(visualBackground.blurRadius);
        }

        private void OnDisable()
        {
            ApplyBlur(0f);
        }

        private void OnValidate()
        {
            RefreshLayers();
        }

        private void RefreshLayers()
        {
            layers = GetComponentsInChildren<SpriteRenderer>(true);
            properties ??= new MaterialPropertyBlock();
            ApplyBlur(isActiveAndEnabled ? visualBackground.blurRadius : 0f);
        }

        private void ApplyBlur(float radius)
        {
            if (layers == null || properties == null)
            {
                return;
            }

            foreach (SpriteRenderer layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.GetPropertyBlock(properties);
                properties.SetFloat(BlurRadius, radius);
                properties.SetFloat(TileHorizontally, layer.drawMode == SpriteDrawMode.Tiled ? 1f : 0f);
                layer.SetPropertyBlock(properties);
            }
        }
    }
}
