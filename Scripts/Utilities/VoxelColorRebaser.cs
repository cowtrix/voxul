using UnityEngine;

namespace Voxul
{
	public class VoxelColorRebaser : VoxelRendererPropertyModifier
	{
		[ColorUsage(true, true)]
		public Color Red = Color.red;
		[ColorUsage(true, true)]
		public Color Green = Color.green;
		[ColorUsage(true, true)]
		public Color Blue = Color.blue;

		protected override void SetPropertyBlock(MaterialPropertyBlock block, VoxelRendererSubmesh submesh)
		{
			block.SetColor("TargetRed", Red);
			block.SetColor("TargetGreen", Green);
			block.SetColor("TargetBlue", Blue);
		}
	}
}
