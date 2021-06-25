using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul
{
	public enum EUVMode : byte
	{
		Global, GlobalScaled, Local, LocalScaled,
	}

	public enum ENormalMode : byte
	{
		Hard,
	}

	public enum EMaterialMode
	{
		Opaque,
		Transparent,
	}

	public enum ERenderMode
	{
		Block,
		XPlane,
		YPlane,
		ZPlane,
		XZCross,
		XYCross,
		ZYCross,
		FullCross,
	}

	public enum EVoxelDirection : byte
	{
		YNeg, XNeg,
		ZPos, ZNeg,
		XPos, YPos,
	}

	[Serializable]
	public struct DirectionOverride
	{
		public EVoxelDirection Direction;
		public SurfaceData Data;
	}

	[Serializable]
	public struct TextureIndex
	{
		public int Index;
	}

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

	[Serializable]
	public struct VoxelMaterial
	{
		public EMaterialMode MaterialMode;
		public ERenderMode RenderMode;
		public ENormalMode NormalMode;
		public SurfaceData Default;
		public DirectionOverride[] Overrides;

		public SurfaceData GetSurface(EVoxelDirection dir)
		{
			if (Overrides != null)
			{
				var ov = Overrides.Where(o => o.Direction == dir);
				if (ov.Any())
				{
					return ov.First().Data;
				}
			}

			return Default;
		}

		public IEnumerable<(EVoxelDirection, SurfaceData)> GetSurfaces()
		{
			yield return (EVoxelDirection.XNeg, GetSurface(EVoxelDirection.XNeg));
			yield return (EVoxelDirection.XPos, GetSurface(EVoxelDirection.XPos));
			yield return (EVoxelDirection.YNeg, GetSurface(EVoxelDirection.YNeg));
			yield return (EVoxelDirection.YPos, GetSurface(EVoxelDirection.YPos));
			yield return (EVoxelDirection.ZNeg, GetSurface(EVoxelDirection.ZNeg));
			yield return (EVoxelDirection.ZPos, GetSurface(EVoxelDirection.ZPos));
		}

		public VoxelMaterial Copy()
		{
			return new VoxelMaterial
			{
				Default = Default,
				Overrides = Overrides?.ToArray(),
				RenderMode = RenderMode,
				NormalMode = NormalMode,
				MaterialMode = MaterialMode,
			};
		}
	}

	[CreateAssetMenu]
	public class VoxelMaterialAsset : ScriptableObject
	{
		public VoxelMaterial Data;
	}
}