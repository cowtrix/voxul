using Common;
using UnityEngine;

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
		if(!Alive)
		{
			gameObject.SafeDestroy();
		}
		Alive = false;
	}
}
