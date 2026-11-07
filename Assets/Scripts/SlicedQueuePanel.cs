using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SlicedQueuePanel : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image outlineImage;
        [SerializeField] private QueueBorderGap borderGap;
        private Sprite generatedFill, generatedOutline;
        private Texture2D fillTexture, outlineTexture;
        private float cachedRadius = -1f, cachedWidth = -1f;

        public void Configure(Color fill, Color border, float width, float radius, float gap,
            Sprite fillSprite, Sprite outlineSprite)
        {
            if (fillImage == null || outlineImage == null || borderGap == null) return;
            Rect rect = ((RectTransform)transform).rect;
            float panelWidth = Mathf.Max(0f, rect.width);
            float panelHeight = Mathf.Max(0f, rect.height);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(panelWidth, panelHeight) * 0.5f);
            width = Mathf.Clamp(width, 0f, Mathf.Min(panelWidth, panelHeight) * 0.5f);

            // The supplied artwork covers the default style. Regenerate only when Inspector tuning
            // changes its radius/thickness; rendering still uses Unity's standard sliced Images.
            if (!Mathf.Approximately(radius, 14f) || !Mathf.Approximately(width, 2f))
            {
                if (generatedFill == null || !Mathf.Approximately(radius, cachedRadius) || !Mathf.Approximately(width, cachedWidth))
                {
                    ReleaseGenerated();
                    BuildArtwork(radius, width);
                    cachedRadius = radius;
                    cachedWidth = width;
                }
                fillSprite = generatedFill;
                outlineSprite = generatedOutline;
            }
            fillImage.sprite = fillSprite;
            fillImage.color = fill;

            // One sliced Image keeps all four corners continuous. Only its straight bottom edge is split.
            outlineImage.sprite = outlineSprite;
            outlineImage.color = width > 0f ? border : Color.clear;
            borderGap.SetGap(gap);
        }

        private void BuildArtwork(float radius, float width)
        {
            const float resolution = 4f;
            float halfSize = Mathf.Max(32f, radius + width + 2f);
            int pixels = Mathf.CeilToInt(halfSize * 2f * resolution);
            var fill = new Color32[pixels * pixels];
            var border = new Color32[fill.Length];
            for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / resolution - halfSize, (y + 0.5f) / resolution - halfSize);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (halfSize - radius);
                float distance = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude +
                    Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float outer = Mathf.Clamp01(0.5f - distance * resolution);
                float inner = Mathf.Clamp01(0.5f - (distance + width) * resolution);
                fill[y * pixels + x] = new Color(1f, 1f, 1f, outer);
                border[y * pixels + x] = new Color(1f, 1f, 1f, Mathf.Max(0f, outer - inner));
            }
            float slice = Mathf.Ceil((radius + width + 1f) * resolution);
            fillTexture = Texture(pixels, fill);
            outlineTexture = Texture(pixels, border);
            generatedFill = Sprite.Create(fillTexture, new Rect(0f, 0f, pixels, pixels), Vector2.one * 0.5f,
                100f * resolution, 0, SpriteMeshType.FullRect, Vector4.one * slice);
            generatedOutline = Sprite.Create(outlineTexture, new Rect(0f, 0f, pixels, pixels), Vector2.one * 0.5f,
                100f * resolution, 0, SpriteMeshType.FullRect, Vector4.one * slice);
            generatedFill.hideFlags = generatedOutline.hideFlags = HideFlags.HideAndDontSave;
        }

        private static Texture2D Texture(int pixels, Color32[] colors)
        {
            var texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
        }

        private void OnDisable() => ReleaseGenerated();

        private void ReleaseGenerated()
        {
            Release(generatedFill);
            Release(generatedOutline);
            Release(fillTexture);
            Release(outlineTexture);
            generatedFill = generatedOutline = null;
            fillTexture = outlineTexture = null;
        }

        private static void Release(Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
