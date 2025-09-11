using System;
using UnityEngine;

namespace Nullframes.Intrigues.Utils
{
    public enum NLogType
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }

    public static class NDebug
    {
        public static void Log(object log, NLogType messageType = NLogType.Log, bool forceDebug = false)
        {
            var debugMode = PlayerPrefs.HasKey("IntriguesDebugMode") && bool.Parse(PlayerPrefs.GetString("IntriguesDebugMode"));

            switch (messageType)
            {
                case NLogType.Log:
                    if (!debugMode && !forceDebug) return;
                    Debug.Log(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                case NLogType.Warning:
                    Debug.LogWarning(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                case NLogType.Error:
                    Debug.LogError(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        public static void Log(object log, bool debugMode, NLogType messageType = NLogType.Log, bool forceDebug = false)
        {
            switch (messageType)
            {
                case NLogType.Log:
                    if (!debugMode && !forceDebug) return;
                    Debug.Log(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                case NLogType.Warning:
                    Debug.LogWarning(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                case NLogType.Error:
                    Debug.LogError(string.Format(STATIC.DEBUG_TITLE, log));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }
    }
}