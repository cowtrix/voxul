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
		public SurfaceData XPos, XNeg, YPos, YNeg, ZPos, ZNeg;

		public SurfaceData GetSurface(EVoxelDirection dir)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
					return XNeg;
				case EVoxelDirection.XPos:
					return XPos;
				case EVoxelDirection.YNeg:
					return YNeg;
				case EVoxelDirection.YPos:
					return YPos;
				case EVoxelDirection.ZNeg:
					return ZNeg;
				case EVoxelDirection.ZPos:
					return ZPos;
			}
			throw new ArgumentException($"Invalid surface direction: {dir}");
		}

		public VoxelMaterial SetAllSurfaces(SurfaceData data)
		{
			XNeg = data;
			XPos = data;
			YNeg = data;
			YPos = data;
			ZNeg = data;
			ZPos = data;
			return this;
		}

		public VoxelMaterial SetSurface(EVoxelDirection dir, SurfaceData data)
		{
			switch (dir)
			{
				case EVoxelDirection.XNeg:
					XNeg = data;
					break;
				case EVoxelDirection.XPos:
					XPos = data;
					break;
				case EVoxelDirection.YNeg:
					YNeg = data;
					break;
				case EVoxelDirection.YPos:
					YPos = data;
					break;
				case EVoxelDirection.ZNeg:
					ZNeg = data;
					break;
				case EVoxelDirection.ZPos:
					ZPos = data;
					break;
				default:
					throw new ArgumentException($"Invalid surface direction: {dir}");
			}
			return this;
		}
	}

	[CreateAssetMenu]
	public class VoxelMaterialAsset : ScriptableObject
	{
		public VoxelMaterial Data;
	}
}