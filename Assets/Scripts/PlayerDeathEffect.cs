using System.Collections;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerDeathEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Material flashMaterial;
        [SerializeField] private PlayerDeathBurst burstPrefab;
        [SerializeField, Min(0f)] private float flashDuration = 0.035f;

        public IEnumerator Play()
        {
            Collider2D playerCollider = GetComponent<Collider2D>();
            float height = playerCollider.bounds.size.y;
            // Keep the origin at the dying character, including when that position is offscreen.
            Vector3 position = playerCollider.bounds.center;
            if (visual != null && flashMaterial != null && flashDuration > 0f)
            {
                visual.sharedMaterial = flashMaterial;
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                visual.GetPropertyBlock(properties);
                properties.SetFloat("_FlashAmount", 1f);
                visual.SetPropertyBlock(properties);
                yield return new WaitForSecondsRealtime(flashDuration);
            }

            PlayerDeathBurst burst = null;
            if (burstPrefab != null)
            {
                burst = Instantiate(burstPrefab, position, Quaternion.identity);
                burst.Play(height, visual != null ? visual.sortingLayerID : 0,
                    visual != null ? visual.sortingOrder + 1 : 1);
            }

            // GameOverController runs this coroutine so the burst can outlive the player object.
            gameObject.SetActive(false);
            while (burst != null && burst.IsAlive)
            {
                yield return null;
            }
        }
    }
}
