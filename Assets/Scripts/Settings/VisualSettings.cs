using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "VisualSettings", menuName = "Neverland/Settings/Visual")]
    public sealed class VisualSettings : ScriptableObject
    {
        [Header("Stage Outline")]
        public StageOutlineData stageOutline = new();
        [Header("Bridge Gizmos")]
        public BridgeGizmosData bridgeGizmos = new();
        [Header("Collapse Gizmos")]
        public CollapseGizmosData collapseGizmos = new();
        [Header("Tetromino")]
        public TetrominoData tetromino = new();
        [Header("Coin")]
        public CoinData coin = new();
        [Header("Floating Islands and Chests")]
        public IslandData island = new();
        [Header("Coin Flight")]
        public CoinFlightData coinFlight = new();
        [Header("Entry Zone")]
        public EntryZoneData entryZone = new();
        [Header("Hazard")]
        public HazardData hazard = new();
        [Header("Camera Shake")]
        public CameraShakeData cameraShake = new();
        [Header("Background")]
        public BackgroundData background = new();
        [Header("Background Layers")]
        public ParallaxData[] parallax = Array.Empty<ParallaxData>();
        [Header("UI Text")]
        public TextData[] texts = Array.Empty<TextData>();
        [Header("UI Buttons")]
        public ButtonData[] buttons = Array.Empty<ButtonData>();
        [Header("UI Outlines")]
        public OutlineData[] outlines = Array.Empty<OutlineData>();
        [Header("UI Images")]
        public ImageData[] images = Array.Empty<ImageData>();

        [Serializable]
        public sealed class TextData
        {
            public string name;
            public Font font;
            public int fontSize;
            public FontStyle fontStyle;
            public Color color = Color.white;
            public TextAnchor alignment;
            public float lineSpacing = 1f;
            [TextArea] public string text;
            [Tooltip("Keep gameplay-generated text, such as the current score.")]
            public bool dynamicText;
        }

        [Serializable]
        public sealed class ButtonData
        {
            public string name;
            public ColorBlock colors = ColorBlock.defaultColorBlock;
        }

        [Serializable]
        public sealed class OutlineData
        {
            public string name;
            public Color color;
            public Vector2 distance;
            public bool useGraphicAlpha;
        }

        [Serializable]
        public sealed class ImageData
        {
            public string name;
            public Sprite sprite;
            public Color color;
        }

        [Serializable]
        public sealed class StageOutlineData
        {
            [Min(0.01f), Tooltip("Stage outline dash length in Unity units.")]
            public float dashLength = 0.2f;
            [Range(0.01f, 0.1f)]
            public float lineWidth = 0.05f;
            [Range(0f, 1f)]
            public float outlineOpacity = 0.25f;
        }

        [Serializable]
        public sealed class BridgeGizmosData
        {
            public Color lowerThresholdColor = new Color(1f, 0.85f, 0.1f, 0.8f);
            public Color upperThresholdColor = new Color(1f, 0.25f, 0.2f, 0.8f);
        }

        [Serializable]
        public sealed class CollapseGizmosData
        {
            public Color warningThresholdColor = new Color(1f, 0.85f, 0.1f, 0.8f);
            public Color thresholdColor = new Color(1f, 0.5f, 0.2f, 0.8f);
        }

        [Serializable]
        public sealed class TetrominoData
        {
            [Range(0f, 1f)]
            public float ghostOpacity = 0.2f;
            public Color invalidGhostColor = new Color32(0xA5, 0x1D, 0x35, 0xFF);
            [Min(0.01f)]
            public float rejectShakeDuration = 0.12f;
            [Min(0f), Tooltip("Visual shake distance in cells; the logical piece position stays fixed.")]
            public float rejectShakeStrength = 0.08f;
            [Min(1f)]
            public float rejectShakeFrequency = 30f;
            public Color iColor = new Color32(0x28, 0xC8, 0xDF, 0xFF);
            public Color oColor = new Color32(0xE9, 0xC8, 0x42, 0xFF);
            public Color tColor = new Color32(0xA6, 0x6A, 0xE3, 0xFF);
            public Color sColor = new Color32(0x56, 0xC7, 0x77, 0xFF);
            public Color zColor = new Color32(0xEE, 0x7B, 0x82, 0xFF);
            public Color jColor = new Color32(0x52, 0x7B, 0xDA, 0xFF);
            public Color lColor = new Color32(0xE9, 0x9A, 0x47, 0xFF);
        }

        [Serializable]
        public sealed class IslandData
        {
            public Color brickColor = new Color32(0x56, 0xC7, 0x77, 0xFF);
            public Sprite[] chestFrames;
            [Min(1f)] public float chestFramesPerSecond = 8f;
            [Min(0.1f), Tooltip("Height of the chest sprite in tiles.")]
            public float chestSizeInTiles = 1f;
            [Tooltip("Chest offset from the top middle brick, in tiles.")]
            public Vector2 chestOffsetInTiles;
        }

        [Serializable]
        public sealed class CoinData
        {
            public Sprite[] frames;
            [Min(1f)]
            public float framesPerSecond = 8f;
            [Min(0.1f), Tooltip("Coin diameter in stage tiles.")]
            public float sizeInTiles = 0.65f;
        }

        [Serializable]
        public sealed class CoinFlightData
        {
            public Image flyingCoinPrefab;
            [Min(0.05f)]
            public float flightDuration = 0.5f;
            [Min(0f), Tooltip("Curve bend in Canvas units.")]
            public float curvature = 100f;
            public AnimationCurve flightProgress = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 3f, 3f));
            [Range(0f, 1f)]
            public float arrivalScale = 0.1f;
            [Min(0f)]
            public float pulseStrength = 0.15f;
            [Min(0.01f)]
            public float pulseDuration = 0.18f;
            [Min(0.05f)]
            public float popupDuration = 0.55f;
            public Vector2 popupOffset = new Vector2(90f, 0f);
            [Min(0f), Tooltip("Popup travel distance in Canvas units along its chosen direction.")]
            public float popupRise = 40f;
            [Range(0f, 180f), Tooltip("Full spread in degrees, centred on upward travel.")]
            public float popupConeAngle = 120f;
            [Range(0f, 90f), Tooltip("Maximum final tilt in degrees. Rotation follows the travel direction.")]
            public float popupMaxRotation = 25f;
            [Min(1)]
            public int popupFontSize = 26;
            public Color popupColor = new Color(1f, 0.85f, 0.4f);
        }

        [Serializable]
        public sealed class EntryZoneData
        {
            public Color color = new Color(0.85f, 0.85f, 1f, 1f);
            [Range(0f, 1f)]
            public float fillOpacity = 0.035f;
            [Range(0f, 1f)]
            public float outlineOpacity = 0.25f;
            [Min(0.001f), Tooltip("Outline thickness in cells.")]
            public float lineWidth = 0.04f;
            [Min(0.01f), Tooltip("Dash length and gap in cells.")]
            public float dashLength = 0.35f;
        }

        [Serializable]
        public sealed class HazardData
        {
            public Material hazardMaterial;
            public Color baseColor = new Color32(0xE6, 0x5C, 0x63, 0xFF);
            [ColorUsage(false, true)]
            public Color outlineColor = new Color(1f, 0.45f, 0.08f);
            [Range(0.005f, 0.15f), Tooltip("Outline thickness as a fraction of one tile.")]
            public float outlineWidth = 0.055f;
            [Min(0f)]
            public float minimumBrightness = 1.25f;
            [Min(0f)]
            public float maximumBrightness = 3.5f;
            [Min(0f), Tooltip("Complete pulses per second.")]
            public float pulseSpeed = 1.5f;
            [Range(0.2f, 1f)]
            public float symbolSize = 0.95f;
            [ColorUsage(true, true)]
            public Color symbolColor = new Color(1f, 0.8f, 0.15f);
            public Color exclamationColor = new Color(0.03f, 0.015f, 0.02f);
            [Min(0.01f)]
            public float hitFlashDuration = 0.12f;
        }

        [Serializable]
        public sealed class CameraShakeData
        {
            [Min(0f), Tooltip("Maximum camera shake offset in stage cells.")]
            public float strength = 0.08f;
            [Min(0.01f)]
            public float duration = 0.12f;
            [Min(1f)]
            public float frequency = 30f;
        }

        [Serializable]
        public sealed class BackgroundData
        {
            public Color tintColour = new Color(0.12f, 0.16f, 0.28f, 1f);
            [Range(0f, 1f), Tooltip("0 shows the original artwork; 1 applies the full tint colour.")]
            public float tintStrength = 0.25f;
            [Range(0f, 8f), Tooltip("Background softness in screen pixels. Set to 0 for the original sharp artwork.")]
            public float blurRadius = 4f;
        }

        [Serializable]
        public sealed class ParallaxData
        {
            public string name;
            [Tooltip("Repeat this layer horizontally as it scrolls.")]
            public bool scrolling;
            [Range(0f, 1f), Tooltip("Fraction of bridge scrolling applied to this layer. Larger values feel closer.")]
            public float scrollMultiplier = 0.2f;
            [Min(0f), Tooltip("World units per second before the layer multiplier, when no stage is assigned (for menus).")]
            public float menuScrollSpeed = 0.6f;
            [Range(0f, 1f)]
            public float opacity = 1f;
            [Min(0.1f)]
            public float scaleMultiplier = 1f;
            [Tooltip("World-space adjustment from the camera centre.")]
            public Vector2 offset;
        }

        private void OnValidate()
        {
            stageOutline.lineWidth = Mathf.Max(0.01f, stageOutline.lineWidth);
            stageOutline.outlineOpacity = Mathf.Clamp01(stageOutline.outlineOpacity);
            tetromino.ghostOpacity = Mathf.Clamp01(tetromino.ghostOpacity);
            hazard.minimumBrightness = Mathf.Max(0f, hazard.minimumBrightness);
            hazard.maximumBrightness = Mathf.Max(hazard.minimumBrightness, hazard.maximumBrightness);
            hazard.pulseSpeed = Mathf.Max(0f, hazard.pulseSpeed);
            stageOutline.dashLength = Mathf.Max(0.01f, stageOutline.dashLength);
            foreach (ParallaxData layer in parallax)
            {
                layer.scaleMultiplier = Mathf.Max(0.1f, layer.scaleMultiplier);
            }
        }
    }
}
