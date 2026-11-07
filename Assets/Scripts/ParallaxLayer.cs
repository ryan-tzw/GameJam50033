using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.ParallaxData defaultVisualParallax = new();
        [SerializeField, HideInInspector] private int layerIndex;
        private VisualSettings.ParallaxData visualParallax => visualSettings != null && layerIndex >= 0 && layerIndex < visualSettings.parallax.Length ? visualSettings.parallax[layerIndex] : defaultVisualParallax;
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private StageArea stageArea;

        private SpriteRenderer spriteRenderer;
        private float scrollOffset;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            scrollOffset = 0f;
            FitToCamera();
        }

        private void LateUpdate()
        {
            if (Application.IsPlaying(gameObject) && visualParallax.scrolling)
            {
                float displacement = stageArea != null
                    ? stageArea.BridgeFrameDisplacement
                    : -visualParallax.menuScrollSpeed * Time.deltaTime;
                scrollOffset += displacement * visualParallax.scrollMultiplier;
            }

            FitToCamera();
        }

        private void FitToCamera()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null || backgroundCamera == null ||
                !backgroundCamera.orthographic)
            {
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float height = backgroundCamera.orthographicSize * 2f;
            float width = height * backgroundCamera.aspect;
            // One shared cover scale preserves the alignment of the full-frame artwork.
            float scale = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * visualParallax.scaleMultiplier;
            float tileWidth = spriteSize.x * scale;
            scrollOffset = visualParallax.scrolling ? Mathf.Repeat(scrollOffset, tileWidth) : 0f;
            Vector3 centre = backgroundCamera.transform.position;
            transform.position = new Vector3(centre.x + visualParallax.offset.x + scrollOffset, centre.y + visualParallax.offset.y, transform.position.z);
            transform.localScale = Vector3.one * scale;
            spriteRenderer.drawMode = visualParallax.scrolling ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            // Three complete repeats cover the viewport throughout an entire wrap.
            spriteRenderer.size = new Vector2(spriteSize.x * (visualParallax.scrolling ? 3f : 1f), spriteSize.y);
            Color color = spriteRenderer.color;
            color.a = visualParallax.opacity;
            spriteRenderer.color = color;
        }

        private void OnValidate()
        {
            FitToCamera();
        }
    }
}
