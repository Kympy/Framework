using System;
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
    }
}
