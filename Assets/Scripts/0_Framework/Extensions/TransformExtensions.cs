using System.Buffers;
using UnityEngine;

namespace Framework.Extensions
{
    public static class TransformExtensions
    {
        public static void SetPositionX(this Transform t, float x)
        {
            Vector3 pos = t.position;
            pos.x = x;
            t.position = pos;
        }
        
        public static void SetPositionY(this Transform t, float y)
        {
            Vector3 pos = t.position;
            pos.y = y;
            t.position = pos;
        }
        
        public static void SetPositionZ(this Transform t, float z)
        {
            Vector3 pos = t.position;
            pos.z = z;
            t.position = pos;
        }
        
        public static void SetLocalPositionX(this Transform t, float x)
        {
            Vector3 pos = t.localPosition;
            pos.x = x;
            t.localPosition = pos;
        }
        
        public static void SetLocalPositionY(this Transform t, float y)
        {
            Vector3 pos = t.localPosition;
            pos.y = y;
            t.localPosition = pos;
        }
        
        public static void SetLocalPositionZ(this Transform t, float z)
        {
            Vector3 pos = t.localPosition;
            pos.z = z;
            t.localPosition = pos;
        }

        public static void Identity(this Transform t)
        {
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

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

        /// <summary>
        /// 첫 하위 계층의 자식들을 가져옴. 풀에서 배열을 가져오기에 반납을 꼭 해야함.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Transform[] GetChildrenRent(this Transform t)
        {
            int childCount = t.childCount;
            if (childCount == 0) return null;

            Transform[] result = ArrayPool<Transform>.Shared.Rent(childCount);
            for (int i = 0; i < childCount; i++)
            {
                result[i] = t.GetChild(i);
            }
            return result;
        }

        public static void ReturnChildrenArray(Transform[] array)
        {
            ArrayPool<Transform>.Shared.Return(array, clearArray: true);
        }

        public static T GetOrAddComponent<T>(this Transform t) where T : MonoBehaviour
        {
            if (t.TryGetComponent(out T target))
            {
                return target;
            }

            return t.gameObject.AddComponent<T>();
        }

        public static void DestroyAllChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
