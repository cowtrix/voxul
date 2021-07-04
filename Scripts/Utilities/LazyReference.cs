using System;

namespace Voxul.Utilities
{
	/// <summary>
	/// This is a value that is lazy-intialized the first time the value is accessed.
	/// </summary>
	/// <typeparam name="T">The type of the object.</typeparam>
	public class LazyReference<T> where T : UnityEngine.Object
	{
		public T Value
		{
			get
			{
				if (!m_initialized)
				{
					m_initialized = true;
					m_value = m_getter.Invoke();
				}
				return m_value;
			}
		}
		private readonly Func<T> m_getter;
		private T m_value;
		private bool m_initialized = false;
		public LazyReference(Func<T> getter)
		{
			m_getter = getter;
		}
	}
}