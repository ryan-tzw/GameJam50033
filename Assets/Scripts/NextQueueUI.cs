using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class NextQueueUI : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private TetrominoSpawner spawner;
        [SerializeField] private SlicedQueuePanel panel;
        [SerializeField] private Text label;
        [SerializeField] private Image[] tiles;
        [SerializeField] private QueuePanelGraphic[] hazardOverlays;

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (visualSettings == null || panel == null || label == null || tiles == null || tiles.Length != 20 ||
                hazardOverlays == null || hazardOverlays.Length != 20) return;
            VisualSettings.NextPreviewData settings = visualSettings.nextPreview;
            RectTransform root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = Vector2.one;
            root.pivot = Vector2.one;
            root.anchoredPosition = new Vector2(-settings.margin.x, -settings.margin.y);
            root.sizeDelta = settings.panelSize;
            Color fill = settings.panelColor;
            fill.a = settings.panelOpacity;
            panel.Configure(fill, settings.outlineColor, settings.outlineWidth, settings.cornerRadius,
                settings.labelWidth, settings.panelFillSprite, settings.panelOutlineSprite);
            label.text = "//   NEXT   //";
            label.font = settings.font;
            label.fontSize = settings.labelFontSize;
            label.color = settings.outlineColor;
            label.rectTransform.anchoredPosition = new Vector2(0f, -settings.panelSize.y * 0.5f);
            label.rectTransform.sizeDelta = new Vector2(settings.labelWidth, settings.labelFontSize * 1.8f);

            for (int slot = 0; slot < 5; slot++)
            {
                bool queued = Application.isPlaying && spawner != null && spawner.Upcoming.Count > slot;
                Tetromino.Shape shape = queued ? spawner.Upcoming[slot].shape : (Tetromino.Shape)slot;
                int mask = queued ? spawner.Upcoming[slot].hazardMask : 0;
                Vector2Int minimum = Tetromino.GetShapeCell(shape, 0, 0);
                Vector2Int maximum = minimum;
                for (int i = 1; i < 4; i++)
                {
                    Vector2Int cell = Tetromino.GetShapeCell(shape, i, 0);
                    minimum = Vector2Int.Min(minimum, cell);
                    maximum = Vector2Int.Max(maximum, cell);
                }
                Vector2 centre = ((Vector2)minimum + maximum) * 0.5f;
                float slotWidth = (settings.panelSize.x - 36f) / 5f;
                Vector2 position = new Vector2((slot - 2) * slotWidth, 3f);
                for (int i = 0; i < 4; i++)
                {
                    bool hazard = (mask & (1 << i)) != 0;
                    int index = slot * 4 + i;
                    Image tile = tiles[index];
                    if (tile == null || hazardOverlays[index] == null) continue;
                    tile.rectTransform.sizeDelta = Vector2.one * settings.tileSize;
                    tile.rectTransform.anchoredPosition = position +
                        ((Vector2)Tetromino.GetShapeCell(shape, i, 0) - centre) * (settings.tileSize + settings.tileGap);
                    tile.sprite = settings.blockSprite;
                    tile.color = hazard ? visualSettings.hazard.baseColor : ShapeColor(shape);
                    hazardOverlays[index].gameObject.SetActive(hazard);
                    if (hazard)
                    {
                        hazardOverlays[index].Configure(Color.clear, visualSettings.hazard.outlineColor,
                            1f, 0f, 0f, true, visualSettings.hazard.symbolColor,
                            visualSettings.hazard.exclamationColor);
                    }
                }
            }
        }

        private Color ShapeColor(Tetromino.Shape shape)
        {
            VisualSettings.TetrominoData colors = visualSettings.tetromino;
            return shape switch
            {
                Tetromino.Shape.I => colors.iColor,
                Tetromino.Shape.O => colors.oColor,
                Tetromino.Shape.T => colors.tColor,
                Tetromino.Shape.S => colors.sColor,
                Tetromino.Shape.Z => colors.zColor,
                Tetromino.Shape.J => colors.jColor,
                _ => colors.lColor
            };
        }
    }
}
