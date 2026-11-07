using System.Collections;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerDeathEffect : MonoBehaviour
    {
        [SerializeField] private PlayerSettings playerSettings;
        [SerializeField] private AudioSettings audioSettings;
        private static readonly PlayerSettings.DeathData defaultPlayerDeath = new();
        private PlayerSettings.DeathData playerDeath => playerSettings != null ? playerSettings.death : defaultPlayerDeath;
        private static readonly AudioSettings.PlayerData defaultAudioPlayer = new();
        private AudioSettings.PlayerData audioPlayer => audioSettings != null ? audioSettings.player : defaultAudioPlayer;
        [SerializeField] private SpriteRenderer visual;

        [Header("Audio")]
        [SerializeField, Tooltip("Scene-owned SFX source so playback continues after the player is disabled.")]
        private AudioSource audioSource;

        public IEnumerator Play()
        {
            Collider2D playerCollider = GetComponent<Collider2D>();
            float height = playerCollider.bounds.size.y;
            // Keep the origin at the dying character, including when that position is offscreen.
            Vector3 position = playerCollider.bounds.center;
            if (visual != null && playerDeath.flashMaterial != null && playerDeath.flashDuration > 0f)
            {
                visual.sharedMaterial = playerDeath.flashMaterial;
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                visual.GetPropertyBlock(properties);
                properties.SetFloat("_FlashAmount", 1f);
                visual.SetPropertyBlock(properties);
                yield return new WaitForSecondsRealtime(playerDeath.flashDuration);
            }

            if (audioSource != null && audioPlayer.deathSound != null)
            {
                if (audioSettings != null) audioSource.outputAudioMixerGroup = audioSettings.effectsMixer;
                audioSource.PlayOneShot(audioPlayer.deathSound, audioPlayer.deathVolume);
            }

            PlayerDeathBurst burst = null;
            if (playerDeath.burstPrefab != null)
            {
                burst = Instantiate(playerDeath.burstPrefab, position, Quaternion.identity);
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
