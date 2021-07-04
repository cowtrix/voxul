using UnityEngine;

namespace Voxul.Utilities
{
	/// <summary>
	/// This class will automatically destroy itself unless something calls "Alive" that frame.
	/// </summary>
	[ExecuteAlways]
	public class AutoDestroyer : MonoBehaviour
	{
		public bool Alive;

		public void KeepAlive()
		{
			Alive = true;
		}

		private void LateUpdate()
		{
			if (!Alive)
			{
				gameObject.SafeDestroy();
			}
			Alive = false;
		}
	}
}