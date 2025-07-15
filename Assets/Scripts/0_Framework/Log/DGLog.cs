using System.Diagnostics;
using Framework.Extensions;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DGLog
{
    public const string DebugDefine = "DEBUG_BUILD";
    
    [Conditional(DebugDefine)]
    public static void Log(string log, Color color = default)
    {
        if (color != default)
            Debug.Log(log.AddColor(color));
        else
            Debug.Log(log);
    }

    [Conditional(DebugDefine)]
    public static void Log(object log, Color color = default)
    {
        if (color != default)
            Debug.Log(log.ToString().AddColor(color));
        else
            Debug.Log(log);
    }

    [Conditional(DebugDefine)]
    public static void Log<T>(string log, Color color = default) where T : class
    {
        if (color != default)
        {
            string fullLog = $"[{typeof(T)}] : {log}";
            Debug.Log(fullLog.AddColor(color));
        }
        else
            Debug.Log($"[{typeof(T)}] : {log}");
    }
    
    [Conditional(DebugDefine)]
    public static void Log(System.Type type, string log, Color color = default)
    {
        if (color != default)
        {
            string fullLog = $"[{type.Name}] : {log}";
            Debug.Log(fullLog.AddColor(color));
        }
        else
            Debug.Log($"[{type.Name}] : {log}");
    }

    [Conditional(DebugDefine)]
    public static void LogWarning(string log) { Debug.LogWarning(log); }
    
    [Conditional(DebugDefine)]
    public static void LogWarning(object log) { Debug.LogWarning(log); }
    
    [Conditional(DebugDefine)]
    public static void LogWarning<T>(string log) where T : class
    {
        Debug.LogWarning($"[{typeof(T)}] : {log}");
    }

    [Conditional(DebugDefine)]
    public static void LogError(string log) { Debug.LogError(log); }
    
    [Conditional(DebugDefine)]
    public static void LogError(object log) { Debug.LogError(log); }
    
    [Conditional(DebugDefine)]
    public static void LogError<T>(string log) where T : class
    {
        Debug.LogError($"[{typeof(T)}] : {log}");
    }

    [Conditional(DebugDefine)]
    public static void DrawLine(Vector3 start, Vector3 end) { Debug.DrawLine(start, end); }

    [Conditional(DebugDefine)]
    public static void DrawLine(Vector3 start, Vector3 end, Color color) { Debug.DrawLine(start, end, color); }

    [Conditional(DebugDefine)]
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) { Debug.DrawLine(start, end, color, duration); }

    [Conditional(DebugDefine)]
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) { Debug.DrawLine(start, end, color, duration, depthTest); }

    [Conditional(DebugDefine)]
    public static void DrawRay(Vector3 origin, Vector3 direction) { Debug.DrawRay(origin, direction, Color.white, 0, true); }

    [Conditional(DebugDefine)]
    public static void DrawRay(Vector3 origin, Vector3 direction, Color color) { Debug.DrawLine(origin, origin + direction, color, 0, true); }

    [Conditional(DebugDefine)]
    public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration) { Debug.DrawRay(origin, direction, color, duration, true); }

    [Conditional(DebugDefine)]
    public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration, bool depthTest) { Debug.DrawRay(origin, direction, color, duration, depthTest); }
}