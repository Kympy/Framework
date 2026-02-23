using System;
using UnityEngine;

namespace DragonGate
{
    public static class TypeExtensions
    {
        public static bool IsComponent(this Type type)
        {
            return typeof(Component).IsAssignableFrom(type);
        }

        public static bool IsMaterial(this Type type)
        {
            return type == typeof(Material);
        }

        public static bool IsTexture(this Type type)
        {
            return type == typeof(Texture) || type == typeof(Texture2D) || type == typeof(RenderTexture);
        }

        public static bool IsRenderTexture(this Type type)
        {
            return type == typeof(RenderTexture);
        }

        public static bool IsTexture2D(this Type type)
        {
            return type == typeof(Texture2D);
        }

        public static bool IsAudioClip(this Type type)
        {
            return type == typeof(AudioClip);
        }

        public static bool IsAnimationClip(this Type type)
        {
            return type == typeof(AnimationClip);
        }

        public static bool IsTextAsset(this Type type)
        {
            return type == typeof(TextAsset);
        }

        public static bool IsShader(this Type type)
        {
            return type == typeof(Shader);
        }

        public static bool IsSprite(this Type type)
        {
            return type == typeof(Sprite);
        }

        public static bool IsGameObject(this Type type)
        {
            return type == typeof(GameObject);
        }

        public static bool IsComponent(this UnityEngine.Object obj)
        {
            return obj is Component;
        }

        public static bool IsGameObject(this UnityEngine.Object obj)
        {
            return obj is GameObject;
        }
    }
}