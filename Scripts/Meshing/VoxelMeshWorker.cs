using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	public class VoxelMeshWorker
	{
		public static readonly EVoxelDirection[] Directions = Enum.GetValues(typeof(EVoxelDirection)).Cast<EVoxelDirection>().ToArray();

		public VoxelMesh VoxelMesh { get; }
		public event VoxelRebuildMeshEvent OnCompleted;

		private Task m_currentTask;
		private CancellationTokenSource m_cancellationToken;
		private SemaphoreSlim m_voxelMeshSemaphore = new SemaphoreSlim(1, 1);

		public List<IntermediateVoxelMeshData> IntermediateData = new List<IntermediateVoxelMeshData>();

		public VoxelMeshWorker(VoxelMesh mesh)
		{
			VoxelMesh = mesh;
		}

		public void GenerateMesh(bool force = false, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
		{
			if (m_currentTask != null && m_currentTask.Status == TaskStatus.Running)
			{
				if (!force)
				{
					Debug.LogWarning($"ignoring rebake as one is already in progress");
					return;
				}
				Debug.LogWarning($"Forcing restart of recalculation job for {VoxelMesh}", VoxelMesh);
				while (m_currentTask != null)
				{
					m_cancellationToken.Cancel();
				}
			}

			IntermediateData.Clear();
			m_cancellationToken = new CancellationTokenSource();
			var token = m_cancellationToken.Token;
			Task newTask = null;
			
			newTask = new Task(() =>
			{
				int voxelCount = 0;
				List<KeyValuePair<VoxelCoordinate, Voxel>> allVoxels;
				m_voxelMeshSemaphore.Wait();
				try
				{
					allVoxels = VoxelMesh.Voxels
						.Where(v => v.Key.Layer >= minLayer && v.Key.Layer <= maxLayer)
						.OrderBy(v => v.Value.Material.MaterialMode)
						.ToList();
				}
				finally
				{
					m_voxelMeshSemaphore.Release();
				}
				while (voxelCount < allVoxels.Count)
				{
					var data = new IntermediateVoxelMeshData(allVoxels);
					IntermediateData.Add(data);
					int startVoxCount = voxelCount;
					foreach (var vox in allVoxels.Skip(startVoxCount))
					{
						if (m_cancellationToken.IsCancellationRequested)
						{
							if (m_currentTask == newTask)
							{
								m_currentTask = null;
							}
							return;
						}

						if (vox.Key != vox.Value.Coordinate)
						{
							throw new Exception($"Voxel {vox.Key} had incorrect key in data");
						}
						switch (vox.Value.Material.RenderMode)
						{
							case ERenderMode.Block:
								Cube(vox.Value, data);
								break;
							case ERenderMode.XPlane:
								Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, });
								break;
							case ERenderMode.YPlane:
								Plane(vox.Value, data, new[] { EVoxelDirection.YPos, EVoxelDirection.YNeg, });
								break;
							case ERenderMode.ZPlane:
								Plane(vox.Value, data, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
								break;
							case ERenderMode.XYCross:
								Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
								break;
							case ERenderMode.XZCross:
								Plane(vox.Value, data, new[] { EVoxelDirection.XPos, EVoxelDirection.XNeg, EVoxelDirection.ZPos, EVoxelDirection.ZNeg, });
								break;
							case ERenderMode.ZYCross:
								Plane(vox.Value, data, new[] { EVoxelDirection.ZPos, EVoxelDirection.ZNeg, EVoxelDirection.YPos, EVoxelDirection.YNeg, });
								break;
							case ERenderMode.FullCross:
								Plane(vox.Value, data, Directions.ToArray());
								break;
						}
						voxelCount++;
						if (data.Vertices.Count > 65535 - 100)
						{
							break;
						}
					}

				}
				UnityMainThreadDispatcher.Enqueue(() => Complete());
				if (m_currentTask == newTask)
				{
					m_currentTask = null;
				}
			}, token);
			m_currentTask = newTask;
			m_currentTask.Start();
		}

		void Complete()
		{
			if (!VoxelMesh)
			{
				return;
			}
			m_voxelMeshSemaphore.Wait();
			try
			{
				for (int i = 0; i < IntermediateData.Count; i++)
				{
					if (VoxelMesh.UnityMeshInstances.Count <= i)
					{
						VoxelMesh.UnityMeshInstances.Add(new MeshVoxelData());
					}
					var voxData = IntermediateData[i];
					var meshData = VoxelMesh.UnityMeshInstances[i];
					if (!meshData.UnityMesh
#if UNITY_EDITOR
				|| (meshData.UnityMesh && UnityEditor.AssetDatabase.Contains(VoxelMesh) && !UnityEditor.AssetDatabase.Contains(meshData.UnityMesh))
#endif
			)
					{

#if UNITY_EDITOR
						if (UnityEditor.AssetDatabase.Contains(VoxelMesh))
						{
							var assetPath = UnityEditor.AssetDatabase.GetAssetPath(VoxelMesh);
							UnityEditor.AssetDatabase.AddObjectToAsset(meshData.UnityMesh, assetPath);
						}
						else
#endif
							meshData.UnityMesh = new Mesh();
					}
					meshData.UnityMesh.name = $"{VoxelMesh.name}_mesh_{VoxelMesh.Hash}_0";
					IntermediateVoxelMeshData result = IntermediateData[i];
					meshData.UnityMesh = voxData.SetMesh(meshData.UnityMesh);
					meshData.VoxelMapping = voxData.TriangleVoxelMapping;
				}

				for (var i = VoxelMesh.UnityMeshInstances.Count - 1; i >= IntermediateData.Count; --i)
				{
					var m = VoxelMesh.UnityMeshInstances[i];
					m.UnityMesh.SafeDestroy();
					VoxelMesh.UnityMeshInstances.RemoveAt(i);
				}
			}
			finally
			{
				m_voxelMeshSemaphore.Release();
			}
			OnCompleted.Invoke(this, VoxelMesh);
		}

		public static void Plane(Voxel vox, IntermediateVoxelMeshData data, IEnumerable<EVoxelDirection> dirs)
		{
			var origin = vox.Coordinate.ToVector3();
			var size = vox.Coordinate.GetScale() * Vector3.one;
			DoPlanes(origin, 0, size.xz(), dirs, vox, data);
		}

		public static void Cube(Voxel vox, IntermediateVoxelMeshData data)
		{
			var origin = vox.Coordinate.ToVector3();
			var size = vox.Coordinate.GetScale() * Vector3.one;
			var dirs = Directions.ToList();

			var higherLayer = (sbyte)(vox.Coordinate.Layer - 1);
			var higherCoord = vox.Coordinate.ChangeLayer(higherLayer);

			for (int i = dirs.Count - 1; i >= 0; i--)
			{
				EVoxelDirection dir = dirs[i];
				var neighborCoord = vox.Coordinate + VoxelCoordinate.DirectionToCoordinate(dir, vox.Coordinate.Layer);
				if (data.Voxels != null
					&& data.Voxels.TryGetValue(neighborCoord, out var n)
					&& n.Material.RenderMode == ERenderMode.Block
					&& n.Material.MaterialMode == vox.Material.MaterialMode)
				{
					dirs.RemoveAt(i);
					continue;
				}

				// If the neighbour in a higher layer blocks then the whole side is guaranteed to be occluded
				/*neighborCoord = higherCoord + VoxelCoordinate.DirectionToCoordinate(dir, higherLayer);
				if (Voxels.TryGetValue(neighborCoord, out n)
					&& n.Material.RenderMode == ERenderMode.Block
					&& n.Material.MaterialMode == vox.Material.MaterialMode)
				{
					dirs.RemoveAt(i);
				}*/
			}
			DoPlanes(origin, size.y, size.xz(), dirs, vox, data);
		}

		private static void DoPlanes(Vector3 origin, float offset, Vector2 size,
			IEnumerable<EVoxelDirection> dirs, Voxel vox, IntermediateVoxelMeshData data)
		{
			var submeshIndex = (int)vox.Material.MaterialMode;
			if (!data.Triangles.TryGetValue(submeshIndex, out var tris))
			{
				tris = new List<int>();
				data.Triangles[submeshIndex] = tris;
				if (data.TriangleVoxelMapping != null)
				{
					data.TriangleVoxelMapping[submeshIndex] = new TriangleVoxelMapping.InnerMapping();
				}
			}
			var startTri = tris.Count / 3;
			foreach (var dir in dirs)
			{
				var surface = vox.Material.GetSurface(dir);
				// Get the basic mesh stuff
				VoxelMeshWorker.GetPlane(origin, offset, size, dir, vox.Material, data);

				// Do the colors
				data.Color1.AddRange(Enumerable.Repeat(surface.Albedo, 4));

				// UV2 extra data
				var uv2 = new Vector4(surface.Smoothness, surface.Texture.Index, surface.Metallic, 1 - surface.TextureFade)
					.RemoveNans();
				data.UV2.AddRange(Enumerable.Repeat(uv2, 4));

				var endTri = tris.Count / 3;
				for (var j = startTri; j < endTri; ++j)
				{
					if (data.TriangleVoxelMapping != null)
					{
						data.TriangleVoxelMapping[submeshIndex][j] =
							new VoxelCoordinateTriangleMapping { Coordinate = vox.Coordinate, Direction = dir };
					}
				}
			}
		}

		public static void GetPlane(Vector3 origin, float offset, Vector2 size, EVoxelDirection dir,
			VoxelMaterial material, IntermediateVoxelMeshData data)
		{
			var surface = material.GetSurface(dir);
			var submeshIndex = (int)material.MaterialMode;

			var cubeLength = size.x;
			var cubeWidth = offset;
			var cubeHeight = size.y;

			Quaternion rot = VoxelCoordinate.DirectionToQuaternion(dir);

			// Vertices
			Vector3 v1 = origin + rot * new Vector3(-cubeLength * .5f, cubeWidth * .5f, -cubeHeight * .5f);
			Vector3 v2 = origin + rot * new Vector3(cubeLength * .5f, cubeWidth * .5f, -cubeHeight * .5f);
			Vector3 v3 = origin + rot * new Vector3(cubeLength * .5f, cubeWidth * .5f, cubeHeight * .5f);
			Vector3 v4 = origin + rot * new Vector3(-cubeLength * .5f, cubeWidth * .5f, cubeHeight * .5f);
			var vOffset = data.Vertices.Count;
			data.Vertices.AddRange(new[]
			{
			v1, v2, v3, v4
		});

			// Triangles
			if (!data.Triangles.TryGetValue(submeshIndex, out var submeshList))
			{
				submeshList = new System.Collections.Generic.List<int>();
				data.Triangles[submeshIndex] = submeshList;
			}
			submeshList.AddRange(new[]
			{
			// Cube Left Side Triangles
			3 + vOffset, 1 + vOffset, 0 + vOffset,
			3 + vOffset, 2 + vOffset, 1 + vOffset,
		});

			Vector2 _00_CORDINATES = new Vector2(1f, 1f);
			Vector2 _10_CORDINATES = new Vector2(0f, 1f);
			Vector2 _01_CORDINATES = new Vector2(1f, 0f);
			Vector2 _11_CORDINATES = new Vector2(0f, 0f);
			var uvMode = surface.UVMode;
			switch (uvMode)
			{
				case EUVMode.Local:
					data.UV1.AddRange(new[]
					{
					_11_CORDINATES, _01_CORDINATES, _00_CORDINATES, _10_CORDINATES,
				});
					break;
				case EUVMode.LocalScaled:
					data.UV1.AddRange(new[]
					{
					_11_CORDINATES * size.x, _01_CORDINATES * size.x, _00_CORDINATES * size.x, _10_CORDINATES * size.x,
				});
					break;
				case EUVMode.Global:
					switch (dir)
					{
						case EVoxelDirection.ZNeg:
						case EVoxelDirection.ZPos:
							data.UV1.AddRange(new[] { v1.xy(), v2.xy(), v3.xy(), v4.xy(), });
							break;
						case EVoxelDirection.YNeg:
						case EVoxelDirection.YPos:
							data.UV1.AddRange(new[] { v1.xz(), v2.xz(), v3.xz(), v4.xz(), });
							break;
						case EVoxelDirection.XNeg:
						case EVoxelDirection.XPos:
							data.UV1.AddRange(new[] { v1.yz(), v2.yz(), v3.yz(), v4.yz(), });
							break;
					}
					break;
				case EUVMode.GlobalScaled:
					switch (dir)
					{
						case EVoxelDirection.ZNeg:
						case EVoxelDirection.ZPos:
							data.UV1.AddRange(new[] { v1.xy() / size.x, v2.xy() / size.x, v3.xy() / size.x, v4.xy() / size.x, });
							break;
						case EVoxelDirection.YNeg:
						case EVoxelDirection.YPos:
							data.UV1.AddRange(new[] { v1.xz() / size.x, v2.xz() / size.x, v3.xz() / size.x, v4.xz() / size.x, });
							break;
						case EVoxelDirection.XNeg:
						case EVoxelDirection.XPos:
							data.UV1.AddRange(new[] { v1.yz() / size.x, v2.yz() / size.x, v3.yz() / size.x, v4.yz() / size.x, });
							break;
					}
					break;
			}

		}
	}
}