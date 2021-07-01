using UnityEngine;

namespace Voxul.Utilities
{
	public static class voxulLogger
	{
		public enum ELogLevel { Error, Warning, Debug }
		public static ELogLevel LogLevel { get; private set; }

		static voxulLogger()
		{
			InvalidateLogLevel();
		}

		public static void InvalidateLogLevel()
		{
			UnityMainThreadDispatcher.Enqueue(() =>
			{
				LogLevel = VoxelManager.Instance.LogLevel;
			});
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