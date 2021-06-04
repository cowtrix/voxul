using UnityEngine;
namespace Voxul.Utilities
{
	public static class QuaternionExtensions
	{
		public static Quaternion SnapToNearest90Degrees(this Quaternion quat)
		{
			var eulerAngles = quat.eulerAngles;
			eulerAngles.x = Mathf.Round(eulerAngles.x / 90) * 90;
			eulerAngles.y = Mathf.Round(eulerAngles.y / 90) * 90;
			eulerAngles.z = Mathf.Round(eulerAngles.z / 90) * 90;
			return Quaternion.Euler(eulerAngles);
		}
	}
}