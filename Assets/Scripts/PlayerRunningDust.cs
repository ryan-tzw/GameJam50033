using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(CapsuleCollider2D))]
    public sealed class PlayerRunningDust : MonoBehaviour
    {
        [SerializeField, InspectorName("Burst Frames")] private Sprite[] frames;
        [SerializeField] private SpriteRenderer playerVisual;
        [SerializeField, Tooltip("Mirror the dust horizontally to change its facing direction.")]
        private bool flipX = true;
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;
        [SerializeField, Min(0.1f), Tooltip("Height of the initial puff in stage cells.")]
        private float burstSize = 0.8f;
        [SerializeField, Tooltip("Puff centre relative to the player's feet, in stage cells.")]
        private Vector2 offsetInCells = new Vector2(-0.35f, 0.15f);
        [SerializeField, Range(0f, 1f)] private float opacity = 0.7f;
        [Header("Spawn Gizmo")]
        [SerializeField] private Color spawnGizmoColor = Color.cyan;
        [SerializeField, Min(0.01f), Tooltip("Spawn marker radius in stage cells.")]
        private float spawnGizmoSize = 0.12f;

        // Reuse a few world-space puffs, so they trail behind instead of following the sprite.
        private readonly SpriteRenderer[] puffs = new SpriteRenderer[4];
        private readonly float[] ages = new float[4];
        private PlayerController player;
        private Animator animator;
        private CapsuleCollider2D capsule;
        private bool wasFastRunning;
        private int nextPuff;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            animator = playerVisual != null ? playerVisual.GetComponent<Animator>() : null;
            capsule = GetComponent<CapsuleCollider2D>();
            for (int i = 0; i < puffs.Length; i++)
            {
                GameObject puff = new GameObject("Running Dust");
                // Keep ownership in this scene even if the player was spawned into an additive scene.
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(puff, gameObject.scene);
                puffs[i] = puff.AddComponent<SpriteRenderer>();
                puffs[i].enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (player.StageArea == null || frames == null || frames.Length == 0 || Time.deltaTime <= 0f)
            {
                return;
            }

            for (int i = 0; i < puffs.Length; i++)
            {
                if (!puffs[i].enabled)
                {
                    continue;
                }

                ages[i] += Time.deltaTime;
                int frame = Mathf.FloorToInt(ages[i] * framesPerSecond);
                if (frame >= frames.Length)
                {
                    puffs[i].enabled = false;
                    continue;
                }

                puffs[i].sprite = frames[frame];
                puffs[i].flipX = flipX;
                puffs[i].transform.position += Vector3.right * (player.StageArea.BridgeVelocity * Time.deltaTime);
            }

            // Read after Animator evaluation, so the burst matches entry into the visible fast run.
            bool fastRunning = player.isActiveAndEnabled && animator != null && animator.isActiveAndEnabled &&
                animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.RunFast");
            if (fastRunning && !wasFastRunning)
            {
                Emit();
            }
            wasFastRunning = fastRunning;
        }

        private void Emit()
        {
            int index = nextPuff;
            nextPuff = (nextPuff + 1) % puffs.Length;
            ages[index] = 0f;
            SpriteRenderer puff = puffs[index];
            puff.sprite = frames[0];
            puff.flipX = flipX;
            puff.color = new Color(1f, 1f, 1f, opacity);
            puff.sortingLayerID = playerVisual != null ? playerVisual.sortingLayerID : 0;
            puff.sortingOrder = playerVisual != null ? playerVisual.sortingOrder - 1 : 0;
            float size = player.StageArea.CellSize;
            puff.transform.position = GetSpawnPosition(capsule.bounds, size);
            float scale = burstSize * size / frames[0].bounds.size.y;
            puff.transform.localScale = Vector3.one * scale;
            puff.enabled = true;
        }

        private Vector3 GetSpawnPosition(Bounds bounds, float cellSize)
        {
            return new Vector3(bounds.center.x + offsetInCells.x * cellSize,
                bounds.min.y + offsetInCells.y * cellSize, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            PlayerController controller = GetComponent<PlayerController>();
            CapsuleCollider2D playerCapsule = GetComponent<CapsuleCollider2D>();
            if (controller == null || controller.StageArea == null || playerCapsule == null || !playerCapsule.enabled)
            {
                return;
            }

            float cellSize = controller.StageArea.CellSize;
            Vector3 position = GetSpawnPosition(playerCapsule.bounds, cellSize);
            float radius = spawnGizmoSize * cellSize;
            Color previousColor = Gizmos.color;
            Gizmos.color = spawnGizmoColor;
            Gizmos.DrawWireSphere(position, radius);
            Gizmos.DrawLine(position - Vector3.right * radius, position + Vector3.right * radius);
            Gizmos.DrawLine(position - Vector3.up * radius, position + Vector3.up * radius);
            Gizmos.color = previousColor;
        }

        private void OnDisable()
        {
            wasFastRunning = false;
            foreach (SpriteRenderer puff in puffs)
            {
                if (puff != null)
                {
                    puff.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (SpriteRenderer puff in puffs)
            {
                if (puff != null)
                {
                    Destroy(puff.gameObject);
                }
            }
        }
    }
}
