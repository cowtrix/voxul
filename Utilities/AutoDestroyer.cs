using UnityEngine;

namespace Voxul.Utilities
{
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