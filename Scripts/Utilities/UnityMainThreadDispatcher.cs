using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Voxul.Utilities
{
	/// <summary>
	/// This is a utility class to assist in Unity multithreading. You can invoke
	/// arbitrary actions or coroutines to be executed on the Unity main thread.
	/// </summary>
	public class UnityMainThreadDispatcher : MonoBehaviour
	{
		private static readonly ConcurrentQueue<Action> m_actionQueue = new ConcurrentQueue<Action>();
		//private static SemaphoreSlim m_executionQueueLock = new SemaphoreSlim(1, 1);
		private static UnityMainThreadDispatcher m_runtimeExecutor;

		public Coroutine Coroutine;

		/// <summary>
		/// You will need to call this from the Unity main thread before calling
		/// `Enqueue` from a different thread, or no event will fire.
		/// </summary>
		public static void EnsureSubscribed()
		{
			// If the app is playing, we setup an invisible gameobject to execute actions
			if (Application.isPlaying && !m_runtimeExecutor)
			{
				m_runtimeExecutor = new GameObject("RuntimeThreadDispatcher_hidden")
					.AddComponent<UnityMainThreadDispatcher>();
				m_runtimeExecutor.gameObject.hideFlags = HideFlags.HideAndDontSave;
			}
			if (m_runtimeExecutor && m_runtimeExecutor.Coroutine == null)
			{
				m_runtimeExecutor.Coroutine = m_runtimeExecutor.StartCoroutine(CallbackExecute());
			}
			// In general, we also latch on to the EditorApplication.update event
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update += Execute;
#endif
		}

		/// <summary>
		/// Enqueue an action to be executed on the main thread.
		/// </summary>
		/// <param name="action">The anonymous function to be executed.</param>
		public static void Enqueue(Action action)
		{
			m_actionQueue.Enqueue(action);
		}

		/// <summary>
		/// Enqueue a coroutine to be executed on the main thread.
		/// NOTE that if this is submitted while the application is not running,
		/// the coroutine will be executed synchronously.
		/// </summary>
		/// <param name="coroutine">The coroutine to be executed.</param>
		public static void Enqueue(IEnumerator coroutine)
		{
			if (!Application.isPlaying)
			{
				Enqueue(() =>
				{
					var en = coroutine;
					while (en.MoveNext()) { }
				});
				return;
			}
			m_actionQueue.Enqueue(() => m_runtimeExecutor.StartCoroutine(coroutine));
		}

		private static IEnumerator CallbackExecute()
		{
			while (true)
			{
				Execute();
				yield return null;
			}
		}

		private static void Execute()
		{
			try
			{
				while (m_actionQueue.Count > 0 && m_actionQueue.TryDequeue(out var action))
				{
					action.Invoke();
				}
			}
			catch(Exception e)
			{
				voxulLogger.Exception(e);
			}
		}
	}
}