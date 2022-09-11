using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxul
{
	[DisallowMultipleComponent]
	public class VoxelColorTint : VoxelRendererPropertyModifier
	{
		[ColorUsage(true, true)]
		public Color Color = Color.white;

		public void SetColor(Color c) => Color = c;

		[ContextMenu("Randomize Color")]
		public void RandomizeColor()
		{
			SetColor(Random.ColorHSV(0, 1, 1, 1, 1, 1));
			Invalidate();
		}

		protected override void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh)
		{
			block.SetColor("AlbedoTint", Color);
		}
	}
}
