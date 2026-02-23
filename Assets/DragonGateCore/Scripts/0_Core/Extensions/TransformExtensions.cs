using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    public static class TransformExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionX(this Transform t, float x)
        {
            Vector3 pos = t.position;
            pos.x = x;
            t.position = pos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionY(this Transform t, float y)
        {
            Vector3 pos = t.position;
            pos.y = y;
            t.position = pos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZ(this Transform t, float z)
        {
            Vector3 pos = t.position;
            pos.z = z;
            t.position = pos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalPositionX(this Transform t, float x)
        {
            Vector3 pos = t.localPosition;
            pos.x = x;
            t.localPosition = pos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalPositionY(this Transform t, float y)
        {
            Vector3 pos = t.localPosition;
            pos.y = y;
            t.localPosition = pos;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalPositionZ(this Transform t, float z)
        {
            Vector3 pos = t.localPosition;
            pos.z = z;
            t.localPosition = pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Identity(this Transform t)
        {
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LocalIdentity(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        
        public static Transform[] GetChildren(this Transform t)
        {
            int childCount = t.childCount;
            if (childCount == 0) return null;

            Transform[] result = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                result[i] = t.GetChild(i);
            }
            return result;
        }
        
        // 첫 하위 계층의 자식들을 가져옴.
        public static void GetChildren(this Transform t, List<Transform> @out)
        {
            int childCount = t.childCount;
            if (childCount == 0)
            {
                return;
            }
            
            @out ??= new List<Transform>(childCount);
            if (@out.Count > 0)
            {
                @out.Clear();
            }
            
            for (int i = 0; i < childCount; i++)
            {
                @out.Add(t.GetChild(i));
            }
        }

        public static void DestroyAllChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(t.GetChild(i).gameObject);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZero(this Transform transform)
        {
            transform.position = Vector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalPositionZero(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRotationIdentity(this Transform transform)
        {
            transform.rotation = Quaternion.identity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLocalRotationIdentity(this Transform transform)
        {
            transform.localRotation = Quaternion.identity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset(this Transform transform, bool ignoreScale = false)
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (ignoreScale == false)
                transform.localScale = Vector3.one;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetLocal(this Transform transform, bool ignoreScale = false)
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (ignoreScale == false)
                transform.localScale = Vector3.one;
        }

        public static Vector3 GetForwardFrom(this Transform self, Transform from)
        {
            var direction = self.position - from.position;
            direction.y = 0;
            direction.Normalize();
            return direction;
        }

        public static Vector3 GetForwardTo(this Transform self, Transform to)
        {
            var direction = to.position - self.position;
            direction.y = 0;
            direction.Normalize();
            return direction;
        }
    }
}
