using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	public class MeshVoxelizer : ScriptableWizard
	{
		public sbyte MaxLayer = 3;

		public VoxelMesh TargetVoxelMesh;
		public Collider Collider;

		public SurfaceData Surface;

		[MenuItem("Tools/Voxul/Voxelize Mesh")]
		public static void OpenWindow()
		{
			DisplayWizard<MeshVoxelizer>("Voxelize Mesh", "Create");
		}

		private void OnWizardCreate()
		{
			if (!CheckParams(out var error))
			{
				voxulLogger.Error(error);
				return;
			}
			TargetVoxelMesh.Voxels.Clear();
			var bounds = Collider.bounds;
			var step = VoxelCoordinate.LayerToScale(MaxLayer);
			var worldStep = Collider.transform.lossyScale.x;
			for (var x = bounds.min.x; x < bounds.max.x; x += step)
			{
				for (var y = bounds.min.y; y < bounds.max.y; y += step)
				{
					for (var z = bounds.min.z; z < bounds.max.z; z += step)
					{
						var localCoord = new Vector3(x, y, z);

						var worldcoord = Collider.transform.localToWorldMatrix.MultiplyPoint3x4(localCoord);
						if (Physics.CheckBox(worldcoord, worldStep * Vector3.one * .5f, Collider.transform.rotation, 1 << Collider.gameObject.layer))
						{
							TargetVoxelMesh.Voxels.AddSafe(new Voxel
							{
								Coordinate = VoxelCoordinate.FromVector3(localCoord, MaxLayer),
								Material = new VoxelMaterial
								{
									Default = Surface
								}
							});
						}
					}
				}
			}
			TargetVoxelMesh.Invalidate();
			var go = new GameObject(TargetVoxelMesh.name);
			go.AddComponent<VoxelRenderer>().Mesh = TargetVoxelMesh;
		}

		private bool CheckParams(out string error)
		{
			if (!TargetVoxelMesh)
			{
				error = "Missing TargetVoxelMesh";
				return false;
			}
			if (!Collider)
			{
				error = "Missing Collider";
				return false;
			}
			error = null;
			return true;
		}
	}
}