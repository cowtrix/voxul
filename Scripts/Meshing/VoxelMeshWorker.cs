using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Voxul;
using Voxul.Utilities;

namespace Voxul.Meshing
{
	/// <summary>
	/// The VoxelMeshWorker is responsible for taking a VoxelMesh object
	/// and transforming it into one or more Unity Meshes.
	/// </summary>
	[Serializable]
	public class VoxelMeshWorker
	{
		/// <summary>
		/// A list of the intermediate data objects which are used while the job is running.
		/// </summary>
		public List<IntermediateVoxelMeshData> IntermediateData = new List<IntermediateVoxelMeshData>();

		public VoxelMeshWorker(VoxelMesh mesh)
		{
			VoxelMesh = mesh;
		}

		/// <summary>
		/// The voxel mesh which contains this worker.
		/// </summary>
		public VoxelMesh VoxelMesh;
		public event VoxelRebuildMeshEvent OnCompleted;

		protected float m_maxCoroutineUpdateTime;
		protected ThreadHandler m_handler;

		private Guid m_lastGenID;
		private object m_threadObjectLock = new object();

		public bool IsRecalculating => m_handler != null ? m_handler.IsRecalculating : false;

		private void CancelCurrentJob() => m_handler?.Cancel();

		public void GenerateMesh(EThreadingMode mode, bool force = false, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
		{
			if (!VoxelMesh)
			{
				throw new ArgumentNullException(nameof(VoxelMesh));
			}
			voxulLogger.Debug($"VoxelMeshWorker: GenerateMesh for {VoxelMesh}", VoxelMesh);
			if (IsRecalculating)
			{
				if (!force)
				{
					voxulLogger.Warning($"ignoring rebake for {VoxelMesh} as one is already in progress", VoxelMesh);
					return;
				}
				CancelCurrentJob();
			}
			IntermediateData = IntermediateData ?? new List<IntermediateVoxelMeshData>();
			IntermediateData.Clear();
			if (m_handler == null)
			{
				m_handler = new ThreadHandler(VoxelMesh.name);
			}
			m_handler.Action = (m, t) => GenerateMesh(m, t, minLayer, maxLayer);
			m_handler.Start(force, mode);
		}

		protected virtual IEnumerator GenerateMesh(EThreadingMode mode, CancellationToken token, sbyte minLayer = sbyte.MinValue, sbyte maxLayer = sbyte.MaxValue)
		{
			var timeLim = m_maxCoroutineUpdateTime;
			var sw = Stopwatch.StartNew();
			var thisJobGuid = Guid.NewGuid();
			voxulLogger.Debug($"Started rebake job {thisJobGuid}");
			m_lastGenID = thisJobGuid;
			int voxelCount = 0;
			List<KeyValuePair<VoxelCoordinate, Voxel>> allVoxels;
			VoxelPointMapping pointMapping;
			m_threadObjectLock = m_threadObjectLock ?? new object();
			lock (m_threadObjectLock)
			{
				allVoxels = VoxelMesh.Voxels
					.Where(v => v.Key.Layer >= minLayer && v.Key.Layer <= maxLayer)
					.OrderBy(v => v.Value.Material.MaterialMode)
					.ToList();
				pointMapping = new VoxelPointMapping(VoxelMesh.PointMapping);
			}
			// Iterate through all voxels and transform into face data
			int vertexCounter = 0;
			while (voxelCount < allVoxels.Count)
			{
				var data = new IntermediateVoxelMeshData();
				data.Initialise(allVoxels, pointMapping);
				IntermediateData.Add(data);
				int startVoxCount = voxelCount;
				foreach (var vox in allVoxels.Skip(startVoxCount))
				{
					if (token.IsCancellationRequested)
					{
						voxulLogger.Debug($"Cancelled rebake job {thisJobGuid}");
						yield break;
					}
					if (vox.Key != vox.Value.Coordinate)
					{
						throw new Exception($"Voxel {vox.Key} had incorrect key in data");
					}
					switch (vox.Value.Material.RenderMode)
					{
						case ERenderMode.Block:
							GenerateFaces_Cube(vox.Value, data);
							break;
						case ERenderMode.XPlane:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg);
							break;
						case ERenderMode.YPlane:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.YPos, EVoxelDirection.YNeg);
							break;
						case ERenderMode.ZPlane:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
							break;
						case ERenderMode.XYCross:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
																		 EVoxelDirection.YPos, EVoxelDirection.YNeg);
							break;
						case ERenderMode.XZCross:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
																		 EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
							break;
						case ERenderMode.ZYCross:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.ZPos, EVoxelDirection.ZNeg,
																		 EVoxelDirection.YPos, EVoxelDirection.YNeg);
							break;
						case ERenderMode.FullCross:
							GenerateFaces_CenteredPlane(vox.Value, data, EVoxelDirection.XPos, EVoxelDirection.XNeg,
																		 EVoxelDirection.YPos, EVoxelDirection.YNeg,
																		 EVoxelDirection.ZPos, EVoxelDirection.ZNeg);
							break;
					}
					if (mode == EThreadingMode.Coroutine && sw.Elapsed.TotalSeconds > timeLim)
					{
						// If we've spent the maximum amount of time in this frame, yield
						sw.Restart();
						yield return null;
					}
					voxelCount++;
					vertexCounter += vox.Value.Material.RenderMode.EstimateVertexCount();
					if (vertexCounter >= 65535)
					{
						// We've reached the max vertex count more or less, so make a new renderer
						vertexCounter = 0;
						break;
					}
				}
				foreach (var opt in VoxelMesh.OptimiserOverrides?.Data?
					.Where(o => o != null && o.Enabled))
				{
					opt.OnPreFaceStep(data);
				}
				ConvertFacesToMesh(data);
			}
			foreach (var data in IntermediateData)
			{
				foreach (var opt in VoxelMesh.OptimiserOverrides.Data
					.Where(o => o != null && o.Enabled))
				{
					opt.OnBeforeCompleteOffThread(data);
				}
			}
			if (mode == EThreadingMode.Task)
			{
				UnityMainThreadDispatcher.Enqueue(() => Complete(thisJobGuid));
			}
			else
			{
				Complete(thisJobGuid);
			}
		}

		protected virtual void Complete(Guid jobID)
		{
			if (!VoxelMesh || m_lastGenID != jobID)
			{
				voxulLogger.Debug($"Ignored the complete for job {jobID} because ID's were different (latest is {m_lastGenID})");
				return;
			}
			voxulLogger.Debug($"Completing render job for {this}");
			lock (m_threadObjectLock)
			{
				if (VoxelMesh.UnityMeshInstances == null)
				{
					VoxelMesh.UnityMeshInstances = new List<MeshVoxelData>();
				}
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
						voxulLogger.Debug($"Created new mesh for {VoxelMesh}", VoxelMesh);
						meshData.UnityMesh = new Mesh();
						meshData.UnityMesh.MarkDynamic();
#if UNITY_EDITOR
						if (UnityEditor.AssetDatabase.Contains(VoxelMesh))
						{
							var assetPath = UnityEditor.AssetDatabase.GetAssetPath(VoxelMesh);
							if (meshData.UnityMesh)
								UnityEditor.AssetDatabase.AddObjectToAsset(meshData.UnityMesh, assetPath);
							UnityEditor.EditorUtility.SetDirty(VoxelMesh);
						}
#endif
					}
					meshData.UnityMesh.name = $"{VoxelMesh.name}_mesh_{VoxelMesh.Hash}_0";
					IntermediateVoxelMeshData result = IntermediateData[i];
					meshData.UnityMesh = voxData.SetMesh(meshData.UnityMesh);
				}

				for (var i = VoxelMesh.UnityMeshInstances.Count - 1; i >= IntermediateData.Count; --i)
				{
					var m = VoxelMesh.UnityMeshInstances[i];
					voxulLogger.Debug($"Destroying mesh {m}");
					m.UnityMesh.SafeDestroy();
					VoxelMesh.UnityMeshInstances.RemoveAt(i);
				}

				IntermediateData.Clear();
			}
			OnCompleted.Invoke(this, VoxelMesh);
			m_handler.Release();
		}

		public virtual void Clear()
		{
			CancelCurrentJob();
			IntermediateData?.Clear();
		}

		public static void GenerateFaces_CenteredPlane(Voxel vox, IntermediateVoxelMeshData data, params EVoxelDirection[] dirs)
		{
			for (int i = 0; i < dirs.Length; i++)
			{
				var dir = dirs[i];
				var vec3int = new Vector3Int(vox.Coordinate.X, vox.Coordinate.Y, vox.Coordinate.Z);
				var swizzle = vec3int.SwizzleForDir(dir, out var depthFloat).RoundToVector2Int();
				var faceCoord = new VoxelFaceCoordinate
				{
					Min = swizzle,
					Max = swizzle,
					Depth = (int)depthFloat,
					Direction = dir,
					Layer = vox.Coordinate.Layer,
					Offset = 0,
				};
				var face = new VoxelFace
				{
					Surface = vox.Material.GetSurface(dir),
					MaterialMode = vox.Material.MaterialMode,
					RenderMode = vox.Material.RenderMode,
				};
				data.Faces.Add(faceCoord, face);
			}
		}

		public static void GenerateFaces_Cube(Voxel vox, IntermediateVoxelMeshData data)
		{
			for (int i = 0; i < VoxelExtensions.Directions.Length; i++)
			{
				EVoxelDirection dir = VoxelExtensions.Directions[i];
				var vec3int = new Vector3Int(vox.Coordinate.X, vox.Coordinate.Y, vox.Coordinate.Z);
				var swizzle = vec3int.SwizzleForDir(dir, out var depthFloat).RoundToVector2Int();
				var faceCoord = new VoxelFaceCoordinate
				{
					Min = swizzle,
					Max = swizzle,
					Depth = (int)depthFloat,
					Direction = dir,
					Layer = vox.Coordinate.Layer,
					Offset = .5f,
				};
				var face = new VoxelFace
				{
					Surface = vox.Material.GetSurface(dir),
					MaterialMode = vox.Material.MaterialMode,
					RenderMode = vox.Material.RenderMode,
				};
				data.Faces.Add(faceCoord, face);
			}
		}

		public static void ConvertFacesToMesh(IntermediateVoxelMeshData data)
		{
			Vector3 GetOffset(Vector3 point)
			{
				if (data.PointOffsets != null && data.PointOffsets.TryGetValue(point, out var offset))
				{
					return offset;
				}
				return Vector3.zero;
			}

			foreach (var voxelFace in data.Faces)
			{
				var submeshIndex = (int)voxelFace.Value.MaterialMode;
				if (!data.Triangles.TryGetValue(submeshIndex, out var tris))
				{
					tris = new List<int>(data.Voxels.Count * 16 * 3);
					data.Triangles[submeshIndex] = tris;
				}

				var surface = voxelFace.Value.Surface;

				var layerScale = VoxelCoordinate.LayerToScale(voxelFace.Key.Layer);

				var height = voxelFace.Key.Offset * layerScale;
				var min = new VoxelCoordinate(voxelFace.Key.Min.ReverseSwizzleForDir(voxelFace.Key.Depth, voxelFace.Key.Direction), voxelFace.Key.Layer);
				var max = new VoxelCoordinate(voxelFace.Key.Max.ReverseSwizzleForDir(voxelFace.Key.Depth, voxelFace.Key.Direction), voxelFace.Key.Layer);
				var planeSize = ((voxelFace.Key.Max + Vector2.one * .5f) - (voxelFace.Key.Min - Vector2.one * .5f)) * layerScale;
				if (voxelFace.Key.Direction != EVoxelDirection.ZNeg && voxelFace.Key.Direction != EVoxelDirection.ZPos)
				{
					planeSize = new Vector2(planeSize.y, planeSize.x);
				}

				var bounds = min.ToBounds();
				bounds.Encapsulate(max.ToBounds());

				var rot = VoxelCoordinate.DirectionToQuaternion(voxelFace.Key.Direction);

				// Vertices
				Vector3 v1 = bounds.center + rot * new Vector3(-planeSize.x * .5f, height, -planeSize.y * .5f);
				v1 += GetOffset(v1);
				Vector3 v2 = bounds.center + rot * new Vector3(planeSize.x * .5f, height, -planeSize.y * .5f);
				v2 += GetOffset(v2);
				Vector3 v3 = bounds.center + rot * new Vector3(planeSize.x * .5f, height, planeSize.y * .5f);
				v3 += GetOffset(v3);
				Vector3 v4 = bounds.center + rot * new Vector3(-planeSize.x * .5f, height, planeSize.y * .5f);
				v4 += GetOffset(v4);
				var vOffset = data.Vertices.Count;
				data.Vertices.AddRange(new[] { v1, v2, v3, v4 });

				// Triangles
				if (!data.Triangles.TryGetValue(submeshIndex, out var submeshList))
				{
					submeshList = new System.Collections.Generic.List<int>();
					data.Triangles[submeshIndex] = submeshList;
				}
				submeshList.AddRange(new[]  {
					3 + vOffset, 1 + vOffset, 0 + vOffset,
					3 + vOffset, 2 + vOffset, 1 + vOffset,
				});

				// Do the colors
				data.Color1.AddRange(Enumerable.Repeat(surface.Albedo, 4));

				// UV2 extra data
				var uv2 = new Vector4(surface.Smoothness, surface.Texture.Index, surface.Metallic, 1 - surface.TextureFade)
					.RemoveNans();
				data.UV2.AddRange(Enumerable.Repeat(uv2, 4));

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
					_11_CORDINATES * planeSize.x, _01_CORDINATES * planeSize.x, _00_CORDINATES * planeSize.x, _10_CORDINATES * planeSize.x,
				});
						break;
					case EUVMode.Global:
						switch (voxelFace.Key.Direction)
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
						switch (voxelFace.Key.Direction)
						{
							case EVoxelDirection.ZNeg:
							case EVoxelDirection.ZPos:
								data.UV1.AddRange(new[] { v1.xy() / planeSize.x, v2.xy() / planeSize.x, v3.xy() / planeSize.x, v4.xy() / planeSize.x, });
								break;
							case EVoxelDirection.YNeg:
							case EVoxelDirection.YPos:
								data.UV1.AddRange(new[] { v1.xz() / planeSize.x, v2.xz() / planeSize.x, v3.xz() / planeSize.x, v4.xz() / planeSize.x, });
								break;
							case EVoxelDirection.XNeg:
							case EVoxelDirection.XPos:
								data.UV1.AddRange(new[] { v1.yz() / planeSize.x, v2.yz() / planeSize.x, v3.yz() / planeSize.x, v4.yz() / planeSize.x, });
								break;
						}
						break;
				}

			}
		}

	}
}