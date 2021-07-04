using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Voxul.Utilities
{
	public class UnityMainThreadDispatcher : MonoBehaviour
	{
		private static readonly Queue<Action> m_actionQueue = new Queue<Action>();
		private static SemaphoreSlim m_executionQueueLock = new SemaphoreSlim(1, 1);
		private static UnityMainThreadDispatcher m_runtimeExecutor;

		public Coroutine Coroutine;

		public static void EnsureSubscribed()
		{
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
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update += Execute;
#endif
		}

		public static void Enqueue(Action a)
		{
			m_executionQueueLock.Wait();
			try
			{
				m_actionQueue.Enqueue(a);
			}
			finally
			{
				m_executionQueueLock.Release();
			}
		}

		public static void Enqueue(IEnumerator a)
		{
			m_executionQueueLock.Wait();
			try
			{
				m_actionQueue.Enqueue(() => m_runtimeExecutor.StartCoroutine(a));
			}
			finally
			{
				m_executionQueueLock.Release();
			}
		}

		static IEnumerator CallbackExecute()
		{
			while (true)
			{
				Execute();
				yield return null;
			}
		}

		private static void Execute()
		{
			m_executionQueueLock.Wait();
			try
			{
				while (m_actionQueue.Count > 0)
				{
					m_actionQueue.Dequeue().Invoke();
				}
			}
			catch(Exception e)
			{
				voxulLogger.Exception(e);
			}
			finally
			{
				m_executionQueueLock.Release();
			}
		}
	}
}