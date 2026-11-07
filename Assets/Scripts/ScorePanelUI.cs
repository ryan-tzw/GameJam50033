using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScorePanelUI : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private SlicedQueuePanel panel;
        [SerializeField] private Text score;
        [SerializeField] private Text label;
        private float width;

        private void OnEnable()
        {
            width = 0f;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (visualSettings == null || panel == null || score == null || label == null) return;
            VisualSettings.ScorePanelData settings = visualSettings.scorePanel;
            float desired = Mathf.Max(settings.minimumWidth, score.preferredWidth + settings.horizontalPadding * 2f,
                settings.labelWidth + settings.cornerRadius * 2f + settings.outlineWidth * 2f);
            // Growing only during a run prevents proportional digits and score pulses from wobbling the frame.
            width = Application.isPlaying ? Mathf.Max(width, desired) : desired;
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -settings.topMargin);
            rect.sizeDelta = new Vector2(width, settings.height);

            score.rectTransform.anchorMin = score.rectTransform.anchorMax = score.rectTransform.pivot = Vector2.one * 0.5f;
            score.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            score.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width - settings.horizontalPadding * 2f), settings.height - 12f);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Vector2.one * 0.5f;
            label.rectTransform.anchoredPosition = new Vector2(0f, -settings.height * 0.5f);
            label.rectTransform.sizeDelta = new Vector2(settings.labelWidth, label.fontSize * 1.8f);
            Color fill = settings.panelColor;
            fill.a = settings.panelOpacity;
            panel.Configure(fill, settings.outlineColor, settings.outlineWidth, settings.cornerRadius, settings.labelWidth,
                visualSettings.nextPreview.panelFillSprite, visualSettings.nextPreview.panelOutlineSprite);
        }
    }
}
