using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BackgroundBlur : MonoBehaviour
    {
        [SerializeField, Range(0f, 8f), Tooltip("Background softness in screen pixels. Set to 0 for the original sharp artwork.")]
        private float blurRadius = 4f;

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
            ApplyBlur(blurRadius);
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
            ApplyBlur(isActiveAndEnabled ? blurRadius : 0f);
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
