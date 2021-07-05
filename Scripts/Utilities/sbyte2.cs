using System;
using UnityEngine;

namespace Voxul.Utilities
{
	[Serializable]
	public struct sbyte2
	{
		public sbyte x, y;

		public override bool Equals(object obj)
		{
			return obj is sbyte2 @sbyte &&
				   x == @sbyte.x &&
				   y == @sbyte.y;
		}

		public override int GetHashCode()
		{
			int hashCode = 1502939027;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(sbyte2 left, sbyte2 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(sbyte2 left, sbyte2 right)
		{
			return !(left == right);
		}
	}
}