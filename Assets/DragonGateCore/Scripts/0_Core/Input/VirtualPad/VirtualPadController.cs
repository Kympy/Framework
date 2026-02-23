
using UnityEngine;

namespace DragonGate
{
    public class VirtualPadController
    {
        private VirtualPadBlock _virtualPadBlock;

        // 이동 조작 패드 Rect 의 초기 포지션
        private Vector2 _movementPadInitialPosition;
        private Vector2 _movementStartPosition;

        private float _padRadius;

        // 현재 정규화 된 입력 벡터
        private Vector3 _normalizedInputVector;

        // 각 입력 벡터에 따른 반환 프로퍼티
        public Vector3 GetNormalizedInputVector3XZ =>
            new Vector3(_normalizedInputVector.x, 0, _normalizedInputVector.y);

        public Vector2 GetNormalizedInputVector2XZ => new Vector2(_normalizedInputVector.x, _normalizedInputVector.y);
        public Vector3 GetNormalizedInputVector3XY => _normalizedInputVector;
        public Vector2 GetNormalizedInputVector2XY => new Vector2(_normalizedInputVector.x, _normalizedInputVector.y);

        public delegate void OnMovementDelegate(Vector3 inputVector);

        public OnMovementDelegate OnMovement = null;

        public VirtualPadController(VirtualPadBlock virtualPadBlock)
        {
            _virtualPadBlock = virtualPadBlock;
            // 조작 패드 초기 포지션
            _movementPadInitialPosition = virtualPadBlock.GetMovementPadPosition();
            _padRadius = virtualPadBlock.GetMovementPadRadius();

            // LeftArea 크기 조정
            RectTransform rt = virtualPadBlock.LeftTouchAreaRect;
            rt.offsetMax = new Vector2(-Screen.width * 0.5f, 0);

            virtualPadBlock.LeftTouchAreaButton.OnLeftDown.AddListener(OnMovementPadPressed);
            virtualPadBlock.LeftTouchAreaButton.OnLeftClick.AddListener(OnMovementPadRelease);
            virtualPadBlock.LeftTouchAreaButton.OnDragEvent.AddListener(OnUpdateHandle);
        }

        private void OnMovementPadPressed()
        {
            _movementStartPosition = _virtualPadBlock.LeftTouchAreaButton.LastDownPosition;
            _virtualPadBlock.SetMovementPadPosition(_movementStartPosition);
            _virtualPadBlock.ResetHandlePosition();
        }

        private void OnMovementPadRelease()
        {
            _normalizedInputVector = Vector3.zero;
            _virtualPadBlock.SetMovementPadPosition(_movementPadInitialPosition);
            _virtualPadBlock.ResetHandlePosition();
        }

        private void OnUpdateHandle(Vector2 inputPosition)
        {
            Vector3 direction = inputPosition - _movementStartPosition;
            // 입력 길이의 제곱이(sqr) 반지름 제곱보다 크다면 -> 가동범위 길이 제한
            if (direction.sqrMagnitude >= _padRadius * _padRadius)
            {
                direction = direction.normalized * _padRadius;
            }

            _virtualPadBlock.SetHandlePosition((Vector3)_movementStartPosition + direction);

            Vector3 normalized = direction / _padRadius;
            _normalizedInputVector = normalized;

            OnMovement?.Invoke(_normalizedInputVector);
        }
    }
}