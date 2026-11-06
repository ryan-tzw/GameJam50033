using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Brick), typeof(HazardBrickVisuals))]
    public sealed class HazardBrick : MonoBehaviour
    {
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HitPlayer(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HitPlayer(collision.collider);
        }

        private void HitPlayer(Collider2D other)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Tetromino owner = GetComponentInParent<Tetromino>();
            if (owner != null && !owner.IsPlaced)
            {
                return;
            }

            PlayerHitReaction reaction = other.GetComponentInParent<PlayerHitReaction>();
            if (reaction != null)
            {
                reaction.Hit(GetComponent<HazardBrickVisuals>());
            }
        }
    }
}
