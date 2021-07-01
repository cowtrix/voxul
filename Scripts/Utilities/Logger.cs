using UnityEngine;

namespace Voxul.Utilities
{
	public static class voxulLogger
	{
		public enum ELogLevel { Error, Warning, Debug }
		public static ELogLevel LogLevel { get; private set; }
		static voxulLogger()
		{
			UnityMainThreadDispatcher.EnsureSubscribed();
			UnityMainThreadDispatcher.Enqueue(() =>
			{
#if !UNITY_EDITOR
				LogLevel = ELogLevel.Error;
#else
				LogLevel = (ELogLevel)UnityEditor.EditorPrefs.GetInt($"voxul_LogLevel", (int)ELogLevel.Debug);
#endif
			});
		}

		public static void SetLogLevel(ELogLevel level)
		{
			LogLevel = level;
#if UNITY_EDITOR
			UnityEditor.EditorPrefs.SetInt($"voxul_LogLevel", (int)level);
#endif
		}

		public static void Debug(string message, Object context = null)
		{
			if(LogLevel < ELogLevel.Debug)
			{
				return;
			}
			UnityEngine.Debug.Log(message, context);
		}

		public static void Warning(string message, Object context = null)
		{
			if (LogLevel < ELogLevel.Warning)
			{
				return;
			}
			UnityEngine.Debug.LogWarning(message, context);
		}

		public static void Error(string message, Object context = null)
		{
			UnityEngine.Debug.LogError(message, context);
		}

		public static void Exception(System.Exception exc, Object context = null)
		{
			UnityEngine.Debug.LogException(exc, context);
		}
	}
}