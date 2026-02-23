using System.Runtime.CompilerServices;
using UnityEngine;

namespace DragonGate
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            if (go.TryGetComponent(out T target))
            {
                return target;
            }
            return go.AddComponent<T>();
        }
        
        public static void DestroyAllChildren(this GameObject go)
        {
            Transform t = go.transform;
            int childCount = t.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Object.Destroy(t.GetChild(i).gameObject);
            }
        }
        
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;

            Transform t = go.transform;
            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                t.GetChild(i).gameObject.SetLayerRecursively(layer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnityNull(this UnityEngine.Object unityObject)
        {
            return unityObject == null;
        }
    }
}
