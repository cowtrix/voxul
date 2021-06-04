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
					return ov.Single().Data;
				}
			}

			return Default;
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