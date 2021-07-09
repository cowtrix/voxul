using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxul
{
	[Serializable]
	public struct SurfaceData
	{
		[ColorUsage(true, true)]
		public Color Albedo;
		[Range(0, 1)]
		public float Metallic;
		[Range(0, 1)]
		public float Smoothness;
		public TextureIndex Texture;
		public EUVMode UVMode;
		[Range(0, 1)]
		public float TextureFade;

		public override bool Equals(object obj)
		{
			return obj is SurfaceData data &&
				   Albedo.Equals(data.Albedo) &&
				   Metallic == data.Metallic &&
				   Smoothness == data.Smoothness &&
				   EqualityComparer<TextureIndex>.Default.Equals(Texture, data.Texture) &&
				   UVMode == data.UVMode &&
				   TextureFade == data.TextureFade;
		}

		public override int GetHashCode()
		{
			int hashCode = 188875543;
			hashCode = hashCode * -1521134295 + Albedo.GetHashCode();
			hashCode = hashCode * -1521134295 + Metallic.GetHashCode();
			hashCode = hashCode * -1521134295 + Smoothness.GetHashCode();
			hashCode = hashCode * -1521134295 + Texture.GetHashCode();
			hashCode = hashCode * -1521134295 + UVMode.GetHashCode();
			hashCode = hashCode * -1521134295 + TextureFade.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(SurfaceData left, SurfaceData right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SurfaceData left, SurfaceData right)
		{
			return !(left == right);
		}
	}
}