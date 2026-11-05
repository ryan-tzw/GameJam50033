using UnityEngine;

namespace Jam50033
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class BridgeController : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField, Min(0f), Tooltip("Leftward bridge speed in stage cells per second.")]
        private float bridgeSpeed = 0.5f;

        private float distanceInCells;

        public float Offset => transform.localPosition.x;
        public float FrameDisplacement { get; private set; }

        private void Update()
        {
            if (stageArea == null)
            {
                FrameDisplacement = 0f;
                return;
            }

            distanceInCells += bridgeSpeed * Time.deltaTime;
            float nextOffset = -distanceInCells * stageArea.CellSize;
            FrameDisplacement = nextOffset - Offset;
            transform.localPosition = new Vector3(nextOffset, 0f, 0f);
        }

        private void OnValidate()
        {
            bridgeSpeed = Mathf.Max(0f, bridgeSpeed);
        }
    }
}
