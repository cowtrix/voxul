using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Testing
{
	public class BoxBenchmark : MonoBehaviour
	{
		public EThreadingMode ThreadingMode;
		public Vector3 Size = Vector3.one;
		public sbyte Layer = 2;

		private VoxelRenderer Renderer;
		void Start()
		{
			Renderer = new GameObject("Voxel Renderer")
				.AddComponent<VoxelRenderer>();
			Renderer.GenerateCollider = false;
			Renderer.ThreadingMode = ThreadingMode;
			Renderer.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
			Renderer.transform.position = transform.position;
		}

		// Update is called once per frame
		void Update()
		{
			Renderer.transform.localRotation *= Quaternion.Euler(1, .1f, .1f);
			if (Renderer.Mesh.CurrentWorker != null && Renderer.Mesh.CurrentWorker.IsRecalculating)
			{
				return;
			}

			var step = VoxelCoordinate.LayerToScale(Layer) * 2;
			for (var x = -Size.x; x < Size.x; x += step)
			{
				for (var y = -Size.y; y < Size.y; y += step)
				{
					for (var z = -Size.z; z < Size.z; z += step)
					{
						var coord = VoxelCoordinate.FromVector3(new Vector3(x, y, z), Layer);
						var neighbor = coord + new VoxelCoordinate(0, 2, 0, Layer);

						VoxelMaterial mat;
						/*if (Renderer.Mesh.Voxels.TryGetValue(neighbor, out var neighborVox))
						{
							mat = neighborVox.Material;
						}
						else
						{
							if(y < 0)
							{
								voxulLogger.Error("Something has gone very wrong...");
							}
							mat = new VoxelMaterial
							{
								Default = new SurfaceData
								{
									Albedo = Random.ColorHSV(),
								}
							};
						}*/

						mat = new VoxelMaterial
						{
							Default = new SurfaceData
							{
								Albedo = Random.ColorHSV(),
							}
						};
						Renderer.Mesh.Voxels[coord] = new Voxel(coord, mat);
					}
				}
			}
			Renderer.Mesh.Invalidate();
			Renderer.Invalidate(false, false);
		}
	}

}
