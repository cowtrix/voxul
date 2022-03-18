using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Batching
{
	[Serializable]
	public class VoxelBatchRendererChunk
	{
		[Serializable]
		public class LodLevel
		{
			public VoxelMesh Mesh;
			public VoxelRenderer Renderer;

			public void CleanUp()
			{
				Mesh?.SafeDestroy();
				if (Renderer)
				{
					Renderer?.gameObject?.SafeDestroy();
				}
			}
		}

		public VoxelCoordinate Center;
		public List<VoxelRenderer> Renderers = new List<VoxelRenderer>();

		public LodLevel LOD0;
		public LodLevel LOD1;
		public LodLevel LOD2;

		public MeshCollider MeshCollider;
		public Mesh ColliderMesh;
	}

	public class VoxelBatchRendererManager : ExtendedMonoBehaviour
	{
		[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
		public sbyte ChunkLayerSize = -2;
		public List<VoxelBatchRendererChunk> Chunks = new List<VoxelBatchRendererChunk>();

		[ContextMenu("Regenerate")]
		public void Regenerate()
		{
			// Clean up existing chunks
			CleanUp();

			var allRenderers = FindObjectsOfType<VoxelRenderer>(true)
				.Where(r => ShouldBatch(r))
				.ToList();

			// Split all renderers into chunks
			var chunks = new Dictionary<VoxelCoordinate, List<VoxelRenderer>>();
			foreach (var renderer in allRenderers)
			{
				var layerScale = VoxelCoordinate.LayerToScale(ChunkLayerSize);
				var bounds = renderer.Bounds;
				bounds.Expand(layerScale * Vector3.one);
				for (var x = bounds.min.x; x <= bounds.max.x; x += layerScale)
					for (var y = bounds.min.y; y <= bounds.max.y; y += layerScale)
						for (var z = bounds.min.z; z <= bounds.max.z; z += layerScale)
						{
							var coord = VoxelCoordinate.FromVector3(x, y, z, ChunkLayerSize);
							if (!chunks.TryGetValue(coord, out var chunkList))
							{
								chunkList = new List<VoxelRenderer>();
								chunks[coord] = chunkList;
							}
							chunkList.Add(renderer);
						}
			}

			// Iterate through chunk list and generate
			var voxels = new Dictionary<VoxelCoordinate, Voxel>();
			foreach (var chunk in chunks)
			{
				var chunkEntry = new VoxelBatchRendererChunk() { Center = chunk.Key, Renderers = chunk.Value };
				Chunks.Add(chunkEntry);

				var chunkTransform = new GameObject($"Chunk_{chunkEntry.Center}").transform;
				chunkTransform.SetParent(transform);
				chunkTransform.position = chunk.Key.ToVector3();

				voxels.Clear();

				foreach (var renderer in chunk.Value)
				{
					var chunkBounds = new Bounds(renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(chunkEntry.Center.ToVector3()), VoxelCoordinate.LayerToScale(ChunkLayerSize) * Vector3.one);
					foreach (var v in renderer.Mesh.GetVoxels(chunkBounds))
					{
						var localCoord = v.Coordinate;
						var localVec3 = localCoord.ToVector3();
						var chunkVec3 = chunkTransform.worldToLocalMatrix.MultiplyPoint3x4(renderer.transform.localToWorldMatrix.MultiplyPoint3x4(localVec3));
						var chunkCoord = VoxelCoordinate.FromVector3(chunkVec3, localCoord.Layer);
						voxels[chunkCoord] = new Voxel(chunkCoord, v.Material.Copy());
					}
				}

				{
					var lodLevel = chunkEntry.LOD0;

					lodLevel = new VoxelBatchRendererChunk.LodLevel
					{
						Mesh = ScriptableObject.CreateInstance<VoxelMesh>(),
						Renderer = chunkTransform.gameObject.AddComponent<VoxelRenderer>(),
					};
					lodLevel.Renderer.BatchingEnabled = false;
					lodLevel.Renderer.Mesh = lodLevel.Mesh;
					lodLevel.Renderer.GenerateCollider = false;
					lodLevel.Renderer.ThreadingMode = EThreadingMode.SingleThreaded;
					lodLevel.Mesh.Voxels = new VoxelMapping(voxels);
					lodLevel.Mesh.Invalidate();
					lodLevel.Renderer.Invalidate(true, false);

					chunkEntry.LOD0 = lodLevel;
				}
			}
		}

		[ContextMenu("Clean Up")]
		public void CleanUp()
		{
			foreach (var chunk in Chunks)
			{
				chunk.LOD0.CleanUp();
			}
			Chunks.Clear();
		}

		private bool ShouldBatch(VoxelRenderer r)
		{
			if (!r.BatchingEnabled)
			{
				return false;
			}
			if (r.CustomMaterials)
			{
				return false;
			}
			if (!r.transform.forward.IsOnAxis())
			{
				// TODO allow 90 snapping
				return false;
			}
			if (r.transform.lossyScale != Vector3.one)
			{
				return false;
			}
			if (!r.gameObject.isStatic || !r.gameObject.activeInHierarchy)
			{
				return false;
			}
			var lodGroup = r.GetComponentInParent<LODGroup>();
			if (lodGroup)
			{
				var groups = lodGroup.GetLODs();
				foreach (var group in groups.Skip(1))
				{
					foreach (var submesh in r.Submeshes)
					{
						if (group.renderers.Contains(submesh.MeshRenderer))
						{
							return false;
						}
					}
				}
			}
			if (!r.transform.position.PointIsOnVoxelGrid())
			{
				return false;
			}
			return true;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.magenta;
			foreach (var c in Chunks)
			{
				Gizmos.DrawWireCube(c.Center.ToVector3(), Vector3.one * VoxelCoordinate.LayerToScale(ChunkLayerSize));
			}
		}
	}
}