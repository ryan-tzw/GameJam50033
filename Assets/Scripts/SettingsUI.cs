using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SettingsUI : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField, HideInInspector] private TextBinding[] texts = Array.Empty<TextBinding>();
        [SerializeField, HideInInspector] private ButtonBinding[] buttons = Array.Empty<ButtonBinding>();
        [SerializeField, HideInInspector] private OutlineBinding[] outlines = Array.Empty<OutlineBinding>();
        [SerializeField, HideInInspector] private ImageBinding[] images = Array.Empty<ImageBinding>();

        [Serializable] public sealed class TextBinding { public Text target; public int style; public bool menuHint; }
        [Serializable] public sealed class ButtonBinding { public Button target; public int style; }
        [Serializable] public sealed class OutlineBinding { public Outline target; public int style; }
        [Serializable] public sealed class ImageBinding { public Image target; public int style; }

        private void OnEnable() => ApplySettings();
        private void LateUpdate() => ApplySettings();
        private void OnValidate() => ApplySettings();

        private void ApplySettings()
        {
            if (visualSettings == null) return;

            foreach (TextBinding binding in texts)
            {
                if (binding.target == null || binding.style < 0 || binding.style >= visualSettings.texts.Length) continue;
                Text target = binding.target;
                VisualSettings.TextData style = visualSettings.texts[binding.style];
                VisualSettings.MenuButtonData menu = visualSettings.menuButtons;
                if (binding.menuHint)
                {
                    target.font = menu.hintFont != null ? menu.hintFont : style.font;
                    target.fontSize = menu.hintFontSize;
                    target.fontStyle = menu.hintFontStyle;
                    target.color = menu.hintColor;
                    target.text = menu.hintText;
                    continue;
                }
                MenuButtonStyle buttonStyle = target.GetComponentInParent<MenuButtonStyle>(true);
                if (buttonStyle != null)
                {
                    target.font = menu.font != null ? menu.font : style.font;
                    target.fontSize = buttonStyle.IsGameOverButton ? visualSettings.gameOverMenu.fontSize : menu.fontSize;
                    target.fontStyle = menu.fontStyle;
                    target.color = menu.textColor;
                    target.text = style.text;
                    continue;
                }
                target.font = style.font;
                target.fontSize = style.fontSize;
                target.fontStyle = style.fontStyle;
                target.color = style.color;
                target.alignment = style.alignment;
                target.lineSpacing = style.lineSpacing;
                // Score labels keep the value written by RunScore.
                if (!style.dynamicText) target.text = style.text;
            }

            foreach (ButtonBinding binding in buttons)
            {
                if (binding.target == null) continue;
                if (binding.target.GetComponent<MenuButtonStyle>() != null)
                {
                    binding.target.colors = visualSettings.menuButtons.colors;
                    continue;
                }
                if (binding.style < 0 || binding.style >= visualSettings.buttons.Length) continue;
                binding.target.colors = visualSettings.buttons[binding.style].colors;
            }

            foreach (OutlineBinding binding in outlines)
            {
                if (binding.target == null || binding.style < 0 || binding.style >= visualSettings.outlines.Length) continue;
                VisualSettings.OutlineData style = visualSettings.outlines[binding.style];
                if (binding.target.effectColor != style.color) binding.target.effectColor = style.color;
                if (binding.target.effectDistance != style.distance) binding.target.effectDistance = style.distance;
                if (binding.target.useGraphicAlpha != style.useGraphicAlpha) binding.target.useGraphicAlpha = style.useGraphicAlpha;
            }

            foreach (ImageBinding binding in images)
            {
                if (binding.target == null || binding.style < 0 || binding.style >= visualSettings.images.Length) continue;
                VisualSettings.ImageData style = visualSettings.images[binding.style];
                binding.target.sprite = style.sprite;
                binding.target.color = style.color;
            }
        }
    }
}
