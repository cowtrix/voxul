using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Voxul.Utilities
{
	public static class UnityMainThreadDispatcher
	{
		private static readonly Queue<Action> m_actionQueue = new Queue<Action>();
		private static SemaphoreSlim _executionQueueLock = new SemaphoreSlim(1, 1);

		static UnityMainThreadDispatcher()
		{
			Application.onBeforeRender += Execute;
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update += Execute;
#endif
		}

		public static void Enqueue(Action a)
		{
			_executionQueueLock.Wait();
			try
			{
				m_actionQueue.Enqueue(a);
			}
			finally
			{
				_executionQueueLock.Release();
			}
		}

		private static void Execute()
		{
			_executionQueueLock.Wait();
			try
			{
				while (m_actionQueue.Count > 0)
				{
					m_actionQueue.Dequeue().Invoke();
				}
			}
			finally
			{
				_executionQueueLock.Release();
			}
		}
	}
}