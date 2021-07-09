using System;

namespace Voxul
{
	[Serializable]
	public struct TextureIndex
	{
		public int Index;

		public override bool Equals(object obj)
		{
			return obj is TextureIndex index &&
				   Index == index.Index;
		}

		public override int GetHashCode()
		{
			return -2134847229 + Index.GetHashCode();
		}

		public static bool operator ==(TextureIndex left, TextureIndex right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(TextureIndex left, TextureIndex right)
		{
			return !(left == right);
		}
	}
}