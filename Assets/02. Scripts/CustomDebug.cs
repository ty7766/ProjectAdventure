using UnityEngine;
using System.Diagnostics;

public static class CustomDebug
{
    // "UNITY_EDITOR"가 정의된 경우(에디터)에만 작동
    // 만약 개발 빌드(폰 등)에서도 보고 싶으면 [Conditional("DEVELOPMENT_BUILD")]를 추가하면 됨

    [Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Log(object message, Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message, Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogError(object message, Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }
}