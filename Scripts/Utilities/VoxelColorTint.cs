using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxul
{
	[ExecuteAlways]
	public class VoxelColorTint : VoxelRendererPropertyModifier
	{
		public Color Color = Color.white;

		protected override void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh)
		{
			block.SetColor("AlbedoTint", Color);
		}
	}
}
