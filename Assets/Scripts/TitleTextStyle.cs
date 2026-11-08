using TMPro;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TitleTextStyle : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private int textStyle = 10;
        private TextMeshProUGUI title;

        private void OnEnable() => ApplySettings();
        private void LateUpdate() => ApplySettings();
        private void OnValidate() => ApplySettings();

        private void ApplySettings()
        {
            if (visualSettings == null) return;
            if (title == null) title = GetComponent<TextMeshProUGUI>();
            if (title == null) return;
            Material material = title.fontSharedMaterial;
            if (material == null) return;

            bool changed = false;
            if (textStyle >= 0 && textStyle < visualSettings.texts.Length)
            {
                VisualSettings.TextData text = visualSettings.texts[textStyle];
                title.text = text.text;
                title.fontSize = text.fontSize;
                title.fontSizeMax = text.fontSize;
                title.color = Color.white;
                SetColor(material, "_FaceColor", text.color, ref changed);
            }

            // A dedicated title material keeps these effects separate from every other UI label.
            VisualSettings.TitleStyleData style = visualSettings.titleStyle;
            SetColor(material, "_OutlineColor", style.outlineColor, ref changed);
            SetFloat(material, "_OutlineWidth", style.outlineWidth, ref changed);
            SetColor(material, "_GlowColor", style.glowColor, ref changed);
            SetFloat(material, "_GlowOuter", style.glowSize, ref changed);
            SetColor(material, "_UnderlayColor", style.shadowColor, ref changed);
            SetFloat(material, "_UnderlayOffsetX", style.shadowOffset.x, ref changed);
            SetFloat(material, "_UnderlayOffsetY", style.shadowOffset.y, ref changed);
            SetFloat(material, "_UnderlayDilate", style.shadowSpread, ref changed);
            SetFloat(material, "_UnderlaySoftness", style.shadowSoftness, ref changed);
            if (!changed) return;
            title.UpdateMeshPadding();
            title.SetMaterialDirty();
        }

        private static void SetColor(Material material, string property, Color value, ref bool changed)
        {
            if (material.GetColor(property) == value) return;
            material.SetColor(property, value);
            changed = true;
        }

        private static void SetFloat(Material material, string property, float value, ref bool changed)
        {
            if (Mathf.Approximately(material.GetFloat(property), value)) return;
            material.SetFloat(property, value);
            changed = true;
        }
    }
}
