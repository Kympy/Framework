using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Framework
{
    public class GameUtil
    {
        public static T ToEnum<T>(int value) where T : unmanaged, Enum
        {
            return UnsafeUtility.As<int, T>(ref value);
        }

        public static int ToInt<T>(T enumValue) where T : unmanaged, Enum
        {
            return UnsafeUtility.As<T, int>(ref enumValue);
        }

        public static string GetProjectPath()
        {
            return System.IO.Path.GetDirectoryName(Application.dataPath);
        }

        public static T CreateMonoInstance<T>(bool dontDestroy = false) where T : MonoBehaviour
        {
            var obj = new GameObject(typeof(T).ToString()).AddComponent<T>();
            if (dontDestroy)
                UnityEngine.Object.DontDestroyOnLoad(obj);
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(Component component, bool active)
        {
            if (component == null) return;
            component.gameObject.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(Component component1, Component component2, bool active)
        {
            if (component1 != null)
                component1.gameObject.SetActive(active);
            if (component2 != null)
                component2.gameObject.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(Component component1, Component component2, Component component3, bool active)
        {
            if (component1 != null)
                component1.gameObject.SetActive(active);
            if (component2 != null)
                component2.gameObject.SetActive(active);
            if (component3 != null)
                component3.gameObject.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(Component component1, Component component2, Component component3, Component component4, bool active)
        {
            if (component1 != null)
                component1.gameObject.SetActive(active);
            if (component2 != null)
                component2.gameObject.SetActive(active);
            if (component3 != null)
                component3.gameObject.SetActive(active);
            if (component4 != null)
                component4.gameObject.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(Component component1, Component component2, Component component3, Component component4, Component component5, bool active)
        {
            if (component1 != null)
                component1.gameObject.SetActive(active);
            if (component2 != null)
                component2.gameObject.SetActive(active);
            if (component3 != null)
                component3.gameObject.SetActive(active);
            if (component4 != null)
                component4.gameObject.SetActive(active);
            if (component5 != null)
                component5.gameObject.SetActive(active);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(GameObject gameObject, bool active)
        {
            if (gameObject == null) return;
            gameObject.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(GameObject gameObject1, GameObject gameObject2, bool active)
        {
            if (gameObject1 != null)
                gameObject1.SetActive(active);
            if (gameObject2 != null)
                gameObject2.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(GameObject gameObject1, GameObject gameObject2, GameObject gameObject3, bool active)
        {
            if (gameObject1 != null)
                gameObject1.SetActive(active);
            if (gameObject2 != null)
                gameObject2.SetActive(active);
            if (gameObject3 != null)
                gameObject3.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(GameObject gameObject1, GameObject gameObject2, GameObject gameObject3, GameObject gameObject4, bool active)
        {
            if (gameObject1 != null)
                gameObject1.SetActive(active);
            if (gameObject2 != null)
                gameObject2.SetActive(active);
            if (gameObject3 != null)
                gameObject3.SetActive(active);
            if (gameObject4 != null)
                gameObject4.SetActive(active);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActiveSafe(GameObject gameObject1, GameObject gameObject2, GameObject gameObject3, GameObject gameObject4, GameObject gameObject5, bool active)
        {
            if (gameObject1 != null)
                gameObject1.SetActive(active);
            if (gameObject2 != null)
                gameObject2.SetActive(active);
            if (gameObject3 != null)
                gameObject3.SetActive(active);
            if (gameObject4 != null)
                gameObject4.SetActive(active);
            if (gameObject5 != null)
                gameObject5.SetActive(active);
        }
    }
}