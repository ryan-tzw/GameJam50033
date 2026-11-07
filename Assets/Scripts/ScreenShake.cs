using UnityEngine;
using UnityEngine.Rendering;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.CameraShakeData defaultVisualCameraShake = new();
        private VisualSettings.CameraShakeData visualCameraShake => visualSettings != null ? visualSettings.cameraShake : defaultVisualCameraShake;
        [SerializeField] private StageArea stageArea;

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
            if (!isActiveAndEnabled || stageArea == null || visualCameraShake.strength <= 0f)
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
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, visualCameraShake.duration));
            float amplitude = visualCameraShake.strength * stageArea.CellSize * (1f - progress) * (1f - progress);
            float phase = elapsed * visualCameraShake.frequency * Mathf.PI * 2f;
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
