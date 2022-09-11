using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Voxul
{
	public enum EUVMode : byte
	{
		Global, GlobalScaled, Local, LocalScaled,
	}

	public enum ENormalMode : byte
	{
		Hard,
		Spherical,
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
		[FormerlySerializedAs("Data")]
		public SurfaceData Surface;
	}

	[CreateAssetMenu(menuName ="voxul/Voxul Material Brush")]
	public class VoxelBrushAsset : ScriptableObject
	{
		public VoxelBrush Material;
	}
}