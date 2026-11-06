using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private StageArea stageArea;
        [SerializeField, Tooltip("Repeat this layer horizontally as it scrolls.")]
        private bool scrolling;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of bridge scrolling applied to this layer. Larger values feel closer.")]
        private float scrollMultiplier = 0.2f;
        [SerializeField, Range(0f, 1f)] private float opacity = 1f;
        [SerializeField, Min(0.1f)] private float scaleMultiplier = 1f;
        [SerializeField, Tooltip("World-space adjustment from the camera centre.")]
        private Vector2 offset;

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
            if (Application.IsPlaying(gameObject) && scrolling && stageArea != null)
            {
                scrollOffset += stageArea.BridgeFrameDisplacement * scrollMultiplier;
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
            float scale = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * scaleMultiplier;
            float tileWidth = spriteSize.x * scale;
            scrollOffset = scrolling ? Mathf.Repeat(scrollOffset, tileWidth) : 0f;
            Vector3 centre = backgroundCamera.transform.position;
            transform.position = new Vector3(centre.x + offset.x + scrollOffset, centre.y + offset.y, transform.position.z);
            transform.localScale = Vector3.one * scale;
            spriteRenderer.drawMode = scrolling ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            // Three complete repeats cover the viewport throughout an entire wrap.
            spriteRenderer.size = new Vector2(spriteSize.x * (scrolling ? 3f : 1f), spriteSize.y);
            Color color = spriteRenderer.color;
            color.a = opacity;
            spriteRenderer.color = color;
        }

        private void OnValidate()
        {
            scaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
            FitToCamera();
        }
    }
}
