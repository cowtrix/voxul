using JetBrains.Annotations;
using System;
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

	[CreateAssetMenu]
	public class VoxelMaterialAsset : ScriptableObject
	{
		public VoxelMaterial Data;
	}
}