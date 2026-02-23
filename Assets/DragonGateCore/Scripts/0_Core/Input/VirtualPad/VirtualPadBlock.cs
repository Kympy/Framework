
using UnityEngine;

namespace DragonGate
{

    public class VirtualPadBlock : MonoBehaviour
    {
        [SerializeField] private BetterButton leftTouchArea;
        [SerializeField] private RectTransform movementPad;
        [SerializeField] private RectTransform movementHandle;

        // 필요 시만 사용
        public RectTransform LeftTouchAreaRect => leftTouchArea.GetComponent<RectTransform>();
        public BetterButton LeftTouchAreaButton => leftTouchArea;


        public Vector3 GetMovementPadPosition()
        {
            return movementPad.position;
        }

        public float GetMovementPadRadius()
        {
            return movementPad.rect.width * 0.5f;
        }

        public void SetMovementPadPosition(Vector2 screenPosition)
        {
            movementPad.transform.position = screenPosition;
        }

        public void SetHandlePosition(Vector3 position)
        {
            movementHandle.transform.position = position;
        }

        public void ResetHandlePosition()
        {
            movementHandle.transform.localPosition = Vector2.zero;
        }
    }
}