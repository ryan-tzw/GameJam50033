using UnityEngine;
using UnityEngine.Rendering;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField, Min(0f), Tooltip("Maximum camera shake offset in stage cells.")]
        private float strength = 0.08f;
        [SerializeField, Min(0.01f)] private float duration = 0.12f;
        [SerializeField, Min(1f)] private float frequency = 30f;

        private Camera targetCamera;
        private float elapsed;
        private bool shaking;
        private bool renderOffsetApplied;
        private Vector2 offset;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        public void Play()
        {
            if (!isActiveAndEnabled || stageArea == null || strength <= 0f)
            {
                return;
            }

            elapsed = 0f;
            shaking = true;
        }

        private void LateUpdate()
        {
            if (!shaking)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float amplitude = strength * stageArea.CellSize * (1f - progress) * (1f - progress);
            float phase = elapsed * frequency * Mathf.PI * 2f;
            offset = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 0.83f)) * amplitude;
            if (progress >= 1f)
            {
                shaking = false;
                offset = Vector2.zero;
            }
        }

        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != targetCamera || !shaking)
            {
                return;
            }

            // Shake rendering only: camera-based stage sizing, spawns and bounds stay fixed.
            camera.worldToCameraMatrix = Matrix4x4.Translate(new Vector3(-offset.x, -offset.y, 0f)) *
                camera.worldToCameraMatrix;
            renderOffsetApplied = true;
        }

        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == targetCamera && renderOffsetApplied)
            {
                camera.ResetWorldToCameraMatrix();
                renderOffsetApplied = false;
            }
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
            if (targetCamera != null && renderOffsetApplied)
            {
                targetCamera.ResetWorldToCameraMatrix();
            }

            renderOffsetApplied = false;
            shaking = false;
            offset = Vector2.zero;
        }
    }
}
