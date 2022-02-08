using UnityEngine;
using Voxul.Utilities;
using static Voxul.VoxelRenderer;

namespace Voxul
{
	[ExecuteAlways]
	public class VoxelTransform : ExtendedMonoBehaviour
	{
		public eSnapMode SnapMode;
		[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
		public sbyte SnapLayer = 0;
		public bool OverrideChildren;

		protected VoxelRenderer[] Children => GetComponentsInChildren<VoxelRenderer>(true);

		private void Update()
		{
			if (OverrideChildren)
			{
				foreach(var c in Children)
				{
					c.SnapMode = eSnapMode.None;
				}
			}
			if (SnapMode != eSnapMode.None)
			{
				var scale = VoxelCoordinate.LayerToScale(SnapLayer);
				if (SnapMode == eSnapMode.Local)
				{
					transform.localPosition = transform.localPosition.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
				else if (SnapMode == eSnapMode.Global)
				{
					transform.position = transform.position.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
			}
		}
	}
}