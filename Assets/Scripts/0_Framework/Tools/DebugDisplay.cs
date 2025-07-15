using System;
using Framework.Extensions;
using UnityEditor;
using UnityEngine;

namespace Framework
{
    public class DebugDisplay : MonoBehaviour
    {
        private GUIStyle style;
        private Rect rect;

        private float ratio = 0.03f;
        
        private const float divGB = 1024f * 1024f * 1024f;

        private const string memTextFormat = "Using : {0}GB | Total : {1}GB";
        private const string drawCallTextFormat = "DrawCalls : {0}";

        private float totalRamGB = -1;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            int width = Screen.width, height = Screen.height;
            float startX = 0;
            float startY = 0;
#if UNITY_ANDROID || UNITY_IOS
            bool isPortrait = Screen.width < Screen.height;
            startX = isPortrait ? 0 : Screen.width * 0.05f;
            startY = isPortrait ? Screen.height * 0.05f : 0;
            ratio = 0.02f;
#endif
            rect = new Rect(startX, startY, width, height * ratio);
            style = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = Mathf.RoundToInt(height * ratio),
                normal = { textColor = Color.white }
            };

            totalRamGB = (SystemInfo.systemMemorySize / 1024f).Round(1);
        }

        private Rect NextRect(Rect current)
        {
            return new Rect(current.x, current.y + current.height, current.width, current.height);
        }

        private void OnGUI()
        {
            long memoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            float memoryGB = (memoryBytes / divGB).Round(1);

            string text = string.Format(memTextFormat, memoryGB, totalRamGB);
            GUI.Label(rect, text, style);

#if UNITY_EDITOR
            // UnityStats 는 에디터 전용
            Rect drawCallRect = NextRect(rect);
            GUI.Label(drawCallRect, string.Format(drawCallTextFormat, UnityStats.drawCalls), style);
            
#endif
        }
    }
}