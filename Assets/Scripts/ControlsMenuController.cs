using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    public sealed class ControlsMenuController : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private GameObject panel;
        [SerializeField] private SlicedQueuePanel frame;
        [SerializeField] private Text heading;
        [SerializeField] private Text subtitle;
        [SerializeField] private Text[] descriptions;
        [SerializeField] private Text[] keyLetters;
        [SerializeField] private KeyArrowGraphic[] keyArrows;
        [SerializeField] private SlicedQueuePanel[] keyFrames;
        [SerializeField] private SlicedQueuePanel[] callouts;
        [SerializeField] private Image[] decorations;
        [SerializeField] private SlicedQueuePanel escapeKeyFrame;
        [SerializeField] private Text escapeKeyText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private GameObject[] sourceWidgets;
        private static ControlsMenuController active;
        private bool[] sourceStates;
        private bool open, opening, closing;
        public static bool IsOpen => active != null && active.open;

        private void Awake()
        {
            panel.SetActive(false);
            // Generate rounded panel art and load glyphs before the player opens this page.
            ApplyStyle();
            foreach (Text text in panel.GetComponentsInChildren<Text>(true))
            {
                if (text.font != null && text.font.dynamic)
                    text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            }
        }

        public void Open()
        {
            if (IsOpen || AudioMenuController.IsOpen || StripSceneTransition.IsRunning) return;
            sourceStates = new bool[sourceWidgets.Length];
            for (int i = 0; i < sourceWidgets.Length; i++)
            {
                sourceStates[i] = sourceWidgets[i].activeSelf;
                sourceWidgets[i].SetActive(false);
            }
            active = this;
            open = opening = true;
            closing = false;
        }

        public void Back()
        {
            if (open) closing = true;
        }

        private void Update()
        {
            if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
        }

        private void LateUpdate()
        {
            if (returnButton != null) StyleEntryButton();
            if (!open) return;
            ApplyStyle();
            if (opening)
            {
                opening = false;
                panel.SetActive(true);
                backButton.Select();
            }
            if (closing && (Keyboard.current == null ||
                (!Keyboard.current.spaceKey.isPressed && !Keyboard.current.escapeKey.isPressed))) Close();
        }

        private void ApplyStyle()
        {
            VisualSettings.ControlsPageData style = visualSettings.controlsPage;
            // The pause widgets (including their dimmer) are hidden while this page is open.
            panel.GetComponent<Image>().color = PauseMenuController.IsPaused
                ? visualSettings.pauseMenu.overlayColor : Color.clear;
            var art = visualSettings.nextPreview;
            frame.GetComponent<RectTransform>().sizeDelta = style.panelSize;
            Color fill = style.panelColor;
            fill.a = style.panelOpacity;
            frame.Configure(fill, style.outlineColor, style.outlineWidth, style.cornerRadius, 0f, art.panelFillSprite, art.panelOutlineSprite);
            heading.font = style.headingFont;
            heading.fontSize = style.headingSize;
            heading.color = visualSettings.menuButtons.textColor;
            StyleText(subtitle, style.descriptionSize);
            foreach (Text text in descriptions) StyleText(text, style.descriptionSize);
            foreach (Text key in keyLetters)
            {
                StyleText(key, style.keyFontSize);
                key.color = style.keyInkColor;
            }
            foreach (KeyArrowGraphic arrow in keyArrows)
            {
                arrow.color = style.keyInkColor;
                arrow.rectTransform.sizeDelta = style.arrowSize;
                arrow.EdgeSoftness = style.arrowEdgeSoftness;
            }
            for (int i = 0; i < keyFrames.Length; i++)
            {
                SlicedQueuePanel key = keyFrames[i];
                key.GetComponent<RectTransform>().sizeDelta = new Vector2(i == 6 ? style.spaceKeyWidth : style.keySize, style.keySize);
                key.Configure(style.keyColor, style.outlineColor, 2f, style.keyCornerRadius, 0f, art.panelFillSprite, art.panelOutlineSprite);
            }
            foreach (SlicedQueuePanel callout in callouts)
            {
                fill = style.panelColor;
                fill.a = style.calloutOpacity;
                callout.Configure(fill, style.outlineColor, 2f, style.keyCornerRadius, 0f, art.panelFillSprite, art.panelOutlineSprite);
            }
            foreach (Image decoration in decorations) decoration.color = style.outlineColor;
            escapeKeyFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(style.escapeKeyWidth, style.keySize);
            escapeKeyFrame.Configure(style.keyColor, style.outlineColor, 2f, style.keyCornerRadius, 0f, art.panelFillSprite, art.panelOutlineSprite);
            StyleText(escapeKeyText, style.keyFontSize);
            escapeKeyText.color = style.keyInkColor;
            var backStyle = visualSettings.menuButtons;
            backButton.GetComponent<RectTransform>().sizeDelta = style.backButtonSize;
            fill = backStyle.panelColor;
            fill.a = backStyle.panelOpacity;
            backButton.GetComponent<SlicedQueuePanel>().Configure(fill, backStyle.outlineColor, backStyle.outlineWidth,
                backStyle.cornerRadius, 0f, art.panelFillSprite, art.panelOutlineSprite);
            StyleText(backButton.GetComponentInChildren<Text>(), style.backFontSize);
            backButton.colors = backStyle.colors;
        }

        private void StyleText(Text text, int size)
        {
            text.font = visualSettings.menuButtons.font;
            text.fontSize = size;
            text.fontStyle = visualSettings.menuButtons.fontStyle;
            text.color = visualSettings.menuButtons.textColor;
        }

        private void StyleEntryButton()
        {
            bool icon = returnButton.GetComponent<MenuButtonStyle>().IsIconButton;
            StyleText(returnButton.GetComponentInChildren<Text>(), icon
                ? visualSettings.pauseMenu.captionFontSize : visualSettings.menuButtons.fontSize);
            returnButton.colors = visualSettings.menuButtons.colors;
        }

        private void Close()
        {
            if (!open) return;
            open = opening = closing = false;
            if (active == this) active = null;
            panel.SetActive(false);
            for (int i = 0; i < sourceWidgets.Length; i++)
                if (sourceWidgets[i] != null) sourceWidgets[i].SetActive(sourceStates[i]);
            if (returnButton != null && returnButton.isActiveAndEnabled) returnButton.Select();
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            if (panel == null) return;
            // Prewarmed children may never have been activated, so their OnDestroy may not run.
            foreach (SlicedQueuePanel artwork in panel.GetComponentsInChildren<SlicedQueuePanel>(true))
                artwork.ReleaseGenerated();
        }
    }
}
