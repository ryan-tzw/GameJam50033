using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SlicedQueuePanel))]
    public sealed class MenuButtonStyle : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private bool squareIcon;
        [SerializeField] private bool gameOverButton;
        public bool IsIconButton => squareIcon;
        public bool IsGameOverButton => gameOverButton;
        private SlicedQueuePanel panel;

        private void OnEnable()
        {
            // Button selection now tints the border; keep the fill free of its old colour tint.
            GetComponent<Image>()?.canvasRenderer.SetColor(Color.white);
            ApplySettings();
        }

        private void LateUpdate() => ApplySettings();
        private void OnValidate() => ApplySettings();

        private void ApplySettings()
        {
            if (visualSettings == null) return;
            if (panel == null) panel = GetComponent<SlicedQueuePanel>();
            VisualSettings.MenuButtonData style = visualSettings.menuButtons;
            RectTransform rect = (RectTransform)transform;
            Vector2 size = gameOverButton ? visualSettings.gameOverMenu.buttonSize
                : squareIcon ? Vector2.one * visualSettings.pauseMenu.iconButtonSize : style.size;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, size.x));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, size.y));
            Color fill = style.panelColor;
            fill.a = style.panelOpacity;
            panel.Configure(fill, style.outlineColor, style.outlineWidth, style.cornerRadius, 0f,
                visualSettings.nextPreview.panelFillSprite, visualSettings.nextPreview.panelOutlineSprite);
        }
    }
}
