using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public enum EDirection8
    {
        None = -1,
        
        Up = 0,
        UpRight = 1,
        Right = 2,
        DownRight = 3,
        Down = 4,
        DownLeft = 5,
        Left = 6,
        UpLeft = 7,
    }

    public static class Directions
    {
        public static Dictionary<EDirection8, Vector2> Direction2D8 = new()
        {
            { EDirection8.Up, new Vector2(0, 1) },
            { EDirection8.UpRight, new Vector2(1, 1) },
            { EDirection8.Right, new Vector2(1, 0) },
            { EDirection8.DownRight, new Vector2(1, -1) },
            { EDirection8.Down, new Vector2(0, -1) },
            { EDirection8.DownLeft, new Vector2(-1, -1) },
            { EDirection8.Left, new Vector2(-1, 0) },
            { EDirection8.UpLeft, new Vector2(-1, 1) },
        };
        
        public static Dictionary<EDirection8, Vector2> Direction2D4 = new()
        {
            { EDirection8.Up, new Vector2(0, 1) },
            { EDirection8.Down, new Vector2(0, -1) },
            { EDirection8.Left, new Vector2(-1, 0) },
            { EDirection8.Right, new Vector2(1, 0) }
        };
        
        public static EDirection8 GetDirection8(Vector2 input)
        {
            if (input == Vector2.zero) return EDirection8.None;
            
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;

            if (angle >= 337.5f || angle < 22.5f)
                return EDirection8.Right;
            if (angle >= 22.5f && angle < 67.5f)
                return EDirection8.UpRight;
            if (angle >= 67.5f && angle < 112.5f)
                return EDirection8.Up;
            if (angle >= 112.5f && angle < 157.5f)
                return EDirection8.UpLeft;
            if (angle >= 157.5f && angle < 202.5f)
                return EDirection8.Left;
            if (angle >= 202.5f && angle < 247.5f)
                return EDirection8.DownLeft;
            if (angle >= 247.5f && angle < 292.5f)
                return EDirection8.Down;
            if (angle >= 292.5f && angle < 337.5f)
                return EDirection8.DownRight;
            
            return EDirection8.None;
        }
    }

    public static class Direction8Extensions
    {
        // Forward 기준으로 해당 방향에 맞는 상대 벡터를 구함.

        // index 0~7 -> 각도 0도, 45도, ..., 315도
        public static Vector3 ToRelativeVector3D(this EDirection8 dir, Vector3 forward)
        {
            // forward 기준 회전축을 구함 (보통 위를 기준으로 회전함)
            Vector3 up = Vector3.up;

            // 방향 인덱스를 각도로 변환 (45도 간격)
            float angle = (int)dir * 45f;

            // forward를 angle만큼 회전
            Quaternion rot = Quaternion.AngleAxis(angle, up);
            return rot * forward.normalized;
        }

        public static Vector2 ToRelativeVector2D(this EDirection8 dir, Vector2 forward)
        {
            float angle = (int)dir * 45f; // 8방향, 각 45도 간격
            float rad = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            // 2D 회전 행렬
            return new Vector2(
                forward.x * cos - forward.y * sin,
                forward.x * sin + forward.y * cos
            ).normalized;
        }

        public static EDirection8 GetRelativeDirection2D(Vector2 forward, Vector2 direction)
        {
            // 두 벡터 모두 정규화
            forward.Normalize();
            direction.Normalize();

            // 상대각 (clockwise 기준, 0~360)
            float angle = Vector2.SignedAngle(forward, direction);

            // -180~180을 0~360으로 맞춤
            if (angle < 0) angle += 360f;

            // 8방향 (45도 간격) → 22.5도 offset 추가 후 45로 나누기
            int index = Mathf.RoundToInt(angle / 45f) % 8;

            return (EDirection8)index;
        }
    }
}