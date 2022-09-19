using UnityEngine;
using Voxul.Utilities;
using static Voxul.VoxelRenderer;

namespace Voxul
{
	[ExecuteInEditMode]
	public class VoxelTransform : ExtendedMonoBehaviour
	{
		public eSnapMode SnapMode;
		[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
		public sbyte SnapLayer = 0;
		public bool OverrideChildren;

		public Vector3 Offset;
		private Vector3 m_lastPosition;

		protected VoxelRenderer[] Children => GetComponentsInChildren<VoxelRenderer>(true);

		private void Update()
		{
			if(transform.position == m_lastPosition)
			{
				return;
			}
			m_lastPosition = transform.position;
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
					transform.localPosition = (transform.localPosition - Offset).RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio) + Offset;
				}
				else if (SnapMode == eSnapMode.Global)
				{
					transform.position = transform.position.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
			}
		}
	}
}