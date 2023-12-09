using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Utilities
{
	public enum EThreadingMode
	{
		SingleThreaded,
		Task,
		Coroutine,
	}

	public class ThreadHandler
	{
		public delegate IEnumerator ThreadHandle(EThreadingMode mode, CancellationToken token);
		public string Label { get; }
		public ThreadHandle Action { get; set; }
		private EThreadingMode m_lastMode;
		private int m_lock;
		private CancellationTokenSource m_cancellationToken;

		public bool IsRecalculating => m_lock > 0 && (m_cancellationToken == null || !m_cancellationToken.IsCancellationRequested);

		public ThreadHandler(string label)
		{
			Label = label;
		}

		public void Start(bool force, EThreadingMode mode)
		{
			if (m_lastMode != mode)
			{
				force = true;
			}
			if (!force && IsRecalculating)
			{
				return;
			}
			Interlocked.Increment(ref m_lock);
			UnityMainThreadDispatcher.EnsureSubscribed();
			m_lastMode = mode;
#if PLATFORM_WEBGL
			if(Application.isPlaying && mode == EThreadingMode.Task)
			{
				mode = EThreadingMode.Coroutine;
			}
#endif
			switch (mode)
			{
				case EThreadingMode.SingleThreaded:
					var en = RunAction(mode);
					while (en.MoveNext()) { }
					break;
				case EThreadingMode.Task:
					Task.Factory.StartNew(() => {
						var en = RunAction(mode);
						while (en.MoveNext()) { }
					});
					break;
				case EThreadingMode.Coroutine:
					UnityMainThreadDispatcher.Enqueue(RunAction(mode));
					break;
			}
		}

		private IEnumerator RunAction(EThreadingMode mode)
		{
			m_cancellationToken = new CancellationTokenSource();
			var en = Action.Invoke(mode, m_cancellationToken.Token);
			while (en.MoveNext())
			{
				if(mode == EThreadingMode.Coroutine)
				{
					yield return en.Current;
				}
			}
		}

		public void Cancel()
		{
			voxulLogger.Debug($"Cancelled Thread Handler {Label}");
			if (m_cancellationToken != null)
			{
				m_cancellationToken.Cancel();
			}
			m_lock = 0;
		}

		public void Release()
		{
			if (!IsRecalculating)
			{
				return;
			}
			voxulLogger.Debug($"Released Thread Handler {Label}");
			m_lock = 0;
		}
	}
}