using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [CreateAssetMenu(fileName = "VisualSettings", menuName = "Neverland/Settings/Visual")]
    public sealed class VisualSettings : ScriptableObject
    {
        [Header("Scene Transition")]
        public TransitionData sceneTransition = new();
        [Header("Title Outline, Glow and Shadow")]
        public TitleStyleData titleStyle = new();
        [Header("Stage Outline")]
        public StageOutlineData stageOutline = new();
        [Header("Bridge Gizmos")]
        public BridgeGizmosData bridgeGizmos = new();
        [Header("Collapse Gizmos")]
        public CollapseGizmosData collapseGizmos = new();
        [Header("Tetromino")]
        public TetrominoData tetromino = new();
        [Header("Next Piece Preview")]
        public NextPreviewData nextPreview = new();
        [Header("Hold Piece Preview")]
        public HoldPreviewData holdPreview = new();
        [Header("Score Panel")]
        public ScorePanelData scorePanel = new();
        [Header("Menu Button Panels")]
        public MenuButtonData menuButtons = new();
        [Header("Pause Menu")]
        public PauseMenuData pauseMenu = new();
        [Header("Audio Menu")]
        public AudioMenuData audioMenu = new();
        [Header("Score Milestones")]
        public MilestoneData milestones = new();
        [Header("Coin")]
        public CoinData coin = new();
        [Header("Gem")]
        public CoinData gem = new();
        [Header("Floating Islands and Chests")]
        public IslandData island = new();
        [Header("Coin Flight")]
        public CoinFlightData coinFlight = new();
        [Header("Natural Entry Buffer")]
        public EntryZoneData entryZone = new();
        [Header("No-Build Zone")]
        public NoBuildZoneData noBuildZone = new();
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
        [Tooltip("Presets for buttons without MenuButtonStyle. The themed menu buttons use Menu Button Panels above.")]
        public ButtonData[] buttons = Array.Empty<ButtonData>();
        [Header("UI Outlines")]
        public OutlineData[] outlines = Array.Empty<OutlineData>();
        [Header("UI Images")]
        public ImageData[] images = Array.Empty<ImageData>();

        [Serializable]
        public sealed class AudioMenuData
        {
            public Vector2 headingPosition = new Vector2(0f, 190f);
            public Vector2 rowSize = new Vector2(600f, 64f);
            public float firstRowY = 70f;
            [Min(1f)] public float rowSpacing = 80f;
            public float backButtonY = -200f;
            [Min(1)] public int fontSize = 24;
            public Color trackColor = new Color(0.3f, 0.27f, 0.43f, 1f);
            public Color fillColor = new Color(0.84f, 0.82f, 1f, 1f);
        }

        [Serializable]
        public sealed class PauseMenuData
        {
            public Color overlayColor = new Color(0.025f, 0.045f, 0.08f, 0.8f);
            public Font headingFont;
            [Min(1)] public int headingSize = 70;
            public Color headingColor = new Color(1f, 0.95f, 0.86f, 1f);
            public Vector2 headingPosition = new Vector2(0f, 165f);
            [Tooltip("Resume button Y in Canvas UI units.")]
            public float firstButtonY = 35f;
            [Header("Icon Row")]
            public Sprite audioIcon;
            public Sprite restartIcon;
            public Sprite mainMenuIcon;
            [Min(1f)] public float iconButtonSize = 72f;
            [Min(0f)] public float iconButtonGap = 32f;
            public float iconRowY = -75f;
            [Min(1f)] public float iconSize = 48f;
            public Color iconColor = new Color(1f, 0.95f, 0.84f, 1f);
            [Min(1)] public int captionFontSize = 16;
            [Min(0f)] public float captionGap = 8f;
        }

        [Serializable]
        public sealed class MenuButtonData
        {
            [Header("Shape")]
            [Tooltip("Button width and height in Canvas UI units.")]
            public Vector2 size = new Vector2(280f, 72f);
            public Color panelColor = new Color(0.1f, 0.07f, 0.24f, 1f);
            [Range(0f, 1f)] public float panelOpacity = 0.85f;
            public Color outlineColor = new Color(0.84f, 0.82f, 1f, 1f);
            [Min(0f)] public float outlineWidth = 3f;
            [Min(0f)] public float cornerRadius = 18f;
            [Header("Labels")]
            public Font font;
            [Min(1)] public int fontSize = 36;
            public FontStyle fontStyle;
            public Color textColor = new Color(1f, 0.95f, 0.84f, 1f);
            [Header("Hover and Keyboard Selection")]
            public ColorBlock colors = new ColorBlock
            {
                normalColor = new Color(0.7f, 0.7f, 0.85f, 0.8f),
                highlightedColor = Color.white,
                selectedColor = Color.white,
                pressedColor = new Color(0.8f, 0.75f, 1f, 1f),
                disabledColor = new Color(0.4f, 0.4f, 0.5f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            [Header("Title Screen Hint")]
            public string hintText = "//  PRESS SPACE  //";
            public Font hintFont;
            [Min(1)] public int hintFontSize = 22;
            public FontStyle hintFontStyle = FontStyle.Italic;
            public Color hintColor = new Color(0.84f, 0.82f, 1f, 0.95f);
        }

        [Serializable]
        public sealed class ScorePanelData
        {
            [Min(0f), Tooltip("Seconds for a digit reel to reach its next value. Zero makes changes instant.")]
            public float rollDuration = 0.2f;
            public bool rollUp = true;
            [Range(0.8f, 1.6f), Tooltip("Reel line spacing and clipping height relative to the score font size.")]
            public float digitHeightMultiplier = 1.15f;
            [Min(0f), Tooltip("Extra horizontal space between digits in Canvas UI units.")]
            public float digitSpacing = 0f;
            public bool useCommaSeparators = true;
            [Min(1f), Tooltip("Minimum width in Canvas units. The panel grows to fit larger scores.")]
            public float minimumWidth = 180f;
            [Min(0f)] public float horizontalPadding = 24f;
            [Min(1f)] public float height = 82f;
            public float topMargin = 20f;
            [Min(0f), Tooltip("Inset from the left edge in Canvas UI units.")]
            public float leftMargin = 16f;
            public Color panelColor = new Color(0.06f, 0.04f, 0.13f, 1f);
            [Range(0f, 1f)] public float panelOpacity = 0.45f;
            public Color outlineColor = new Color(0.84f, 0.82f, 1f, 1f);
            [Min(0f)] public float outlineWidth = 2f;
            [Min(0f)] public float cornerRadius = 14f;
            [Min(1f)] public float labelWidth = 120f;
        }

        [Serializable]
        public sealed class MilestoneData
        {
            [Min(0.05f)] public float duration = 1f;
            [Tooltip("Offset from the score number in Canvas UI units.")]
            public Vector2 offset = new Vector2(0f, -85f);
            [Min(1)] public int fontSize = 28;
            public Color headingColor = new Color(0.84f, 0.82f, 1f, 1f);
            public Color bonusColor = new Color(1f, 0.95f, 0.86f, 1f);
            [Min(0f), Tooltip("Popup upward travel in Canvas UI units.")]
            public float rise = 12f;
            [Min(1f), Tooltip("Popup size and score pulse multiplier for emphasized tiers.")]
            public float majorScale = 1.2f;
        }

        [Serializable]
        public class NextPreviewData
        {
            public Font font;
            public Sprite blockSprite;
            public Sprite panelFillSprite;
            public Sprite panelOutlineSprite;
            [Tooltip("Panel width and height in Canvas units.")]
            public Vector2 panelSize = new Vector2(400f, 82f);
            [Tooltip("Canvas UI units. NEXT: inset from the top-right. HOLD: X is the gap before NEXT; Y is the top inset.")]
            public Vector2 margin = new Vector2(18f, 20f);
            public Color panelColor = new Color(0.06f, 0.04f, 0.13f, 1f);
            [Range(0f, 1f)] public float panelOpacity = 0.45f;
            public Color outlineColor = new Color(0.84f, 0.82f, 1f, 1f);
            [Min(0f)] public float outlineWidth = 2f;
            [Min(0f)] public float cornerRadius = 14f;
            [Min(1f)] public float tileSize = 14f;
            [Min(0f)] public float tileGap = 0f;
            [Min(1)] public int labelFontSize = 14;
            [Min(1f)] public float labelWidth = 120f;
        }

        [Serializable]
        public sealed class HoldPreviewData : NextPreviewData
        {
            [Range(0f, 1f), Tooltip("Opacity multiplier for the pocketed piece while hold is locked.")]
            public float lockedOpacity = 0.4f;

            public HoldPreviewData()
            {
                panelSize = new Vector2(160f, 64f);
                margin = new Vector2(16f, 16f);
            }
        }

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
            [Min(0.1f), Tooltip("Collectible height in stage tiles.")]
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
            [Header("Chest Scatter")]
            [Min(0.01f), Tooltip("Seconds before the first reward starts attraction. Staggered rewards keep falling longer.")]
            public float scatterDuration = 0.15f;
            [Min(0f), Tooltip("Initial launch speed in Canvas units per second, with 20% random variation.")]
            public float scatterLaunchSpeed = 450f;
            [Range(0f, 180f), Tooltip("Full launch cone in degrees, centred on upward.")]
            public float scatterSpread = 120f;
            [Min(0f), Tooltip("Downward acceleration in Canvas units per second squared, including the staggered wait.")]
            public float scatterGravity = 1800f;
            [Min(0f), Tooltip("Minimum time between treasure starting attraction, in seconds.")]
            public float minimumArrivalStagger = 0.03f;
            [Min(0f)] public float maximumArrivalStagger = 0.06f;
        }

        [Serializable]
        public sealed class NoBuildZoneData
        {
            public Color color = new Color(0.8f, 0.7f, 1f, 1f);
            [Range(0f, 1f)] public float fillOpacity = 0.07f;
            [Range(0f, 1f)] public float lineOpacity = 0.45f;
            [Min(0.001f), Tooltip("Solid boundary thickness in Unity units.")]
            public float lineWidth = 0.025f;
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

        [Serializable]
        public sealed class TitleStyleData
        {
            public Color outlineColor = new Color(0.65f, 0.38f, 0.12f, 1f);
            [Range(0f, 1f)] public float outlineWidth = 0.08f;
            public Color glowColor = new Color(1f, 0.7f, 0.3f, 0.18f);
            [Range(0f, 1f)] public float glowSize = 0.25f;
            public Color shadowColor = new Color(0.03f, 0.02f, 0.07f, 0.95f);
            [Tooltip("TextMeshPro shadow offset in normalized font-material units, independent of the gameplay grid.")]
            public Vector2 shadowOffset = new Vector2(0.25f, -0.4f);
            [Range(0f, 1f), Tooltip("Expands the solid shadow before softening its edges. TextMeshPro normalized material units.")]
            public float shadowSpread = 0.45f;
            [Range(0f, 1f)] public float shadowSoftness = 0.65f;
        }

        [Serializable]
        public sealed class TransitionData
        {
            public Color color = new Color(0.055f, 0.045f, 0.13f, 1f);
            [Range(1, 64)] public int stripCount = 12;
            [Min(0.01f), Tooltip("Seconds for each strip to cover or reveal the screen.")]
            public float duration = 0.28f;
            [Min(0f), Tooltip("Delay in seconds between neighbouring horizontal strips.")]
            public float stripStagger = 0.01f;
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
