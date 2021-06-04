using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	[ExecuteAlways]
	public class VoxelRenderer : MonoBehaviour
	{
		[HideInInspector]
		public VoxelMesh Mesh;

		[Header("Settings")]
		public bool CustomMaterials;
		public bool GenerateCollider = true;
		public bool SnapToGrid;
		[Range(sbyte.MinValue, sbyte.MaxValue)]
		public sbyte SnapLayer = 0;

		[Header("Rendering")]
		[Range(sbyte.MinValue, sbyte.MaxValue)]
		public sbyte MinLayer = sbyte.MinValue;
		[Range(sbyte.MinValue, sbyte.MaxValue)]
		public sbyte MaxLayer = sbyte.MaxValue;

		private MeshFilter m_filter;
		public MeshRenderer MeshRenderer;
		public MeshCollider Collider;
		private bool m_isDirty;

		[SerializeField]
		[HideInInspector]
		private string m_lastMeshHash;

		public Bounds Bounds => MeshRenderer.bounds;

		private void Update()
		{
			if (SnapToGrid)
			{
				var scale = VoxelCoordinate.LayerToScale(SnapLayer);
				transform.localPosition = transform.localPosition.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
			}
			if (m_isDirty || Mesh?.Hash != m_lastMeshHash)
			{
				Invalidate(false);
			}
		}

		public void SetDirty() => m_isDirty = true;

		public void SetupComponents(bool forceCollider)
		{
			if (!m_filter)
			{
				m_filter = gameObject.GetOrAddComponent<MeshFilter>();
			}
			//m_filter.hideFlags = HideFlags.HideAndDontSave;
			if (!MeshRenderer)
			{
				MeshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
			}
			//m_renderer.hideFlags = HideFlags.HideAndDontSave;
			if (GenerateCollider || forceCollider)
			{
				if (!Collider)
				{
					Collider = gameObject.GetOrAddComponent<MeshCollider>();
				}
				//m_collider.hideFlags = HideFlags.HideAndDontSave;
				Collider.convex = false;
			}
		}

		[ContextMenu("Clear")]
		public void ClearMesh()
		{
			Mesh.Invalidate();
			Mesh.Voxels.Clear();
		}

		[ContextMenu("Force Redraw")]
		public void ForceRedraw()
		{
			Mesh.Invalidate();
			Invalidate(false);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
		}

		public void Invalidate(bool forceCollider)
		{
			m_isDirty = false;
			SetupComponents(forceCollider);
			if (!Mesh)
			{
				return;
			}
			if (MinLayer > MaxLayer)
			{
				MinLayer = MaxLayer;
			}
			m_filter.sharedMesh = Mesh.GenerateMeshInstance(MinLayer, MaxLayer);
			if (GenerateCollider)
			{
				Collider.sharedMesh = m_filter.sharedMesh;
			}
			if (!CustomMaterials)
			{
				if (Mesh.Voxels.Any(v => v.Value.Material.MaterialMode == EMaterialMode.Transparent))
				{
					MeshRenderer.sharedMaterials = new[] { VoxelManager.DefaultMaterial.Value, VoxelManager.DefaultMaterialTransparent.Value, };
				}
				else if (MeshRenderer)
				{
					MeshRenderer.sharedMaterials = new[] { VoxelManager.DefaultMaterial.Value, };
				}
			}
			m_lastMeshHash = Mesh.Hash;
		}

		public Voxel? GetVoxel(int triangleIndex)
		{
			SetupComponents(false);
			if (triangleIndex < 0 || !m_filter || !m_filter.sharedMesh)
			{
				SetDirty();
				return null;
			}

			int limit = triangleIndex * 3;
			int submesh;
			for (submesh = 0; submesh < m_filter.sharedMesh.subMeshCount; submesh++)
			{
				int numIndices = m_filter.sharedMesh.GetTriangles(submesh).Length;

				if (numIndices > limit)
					break;
				triangleIndex -= numIndices / 3;
				limit -= numIndices;
			}

			if (!Mesh.VoxelMapping.TryGetValue(submesh, out var innermap))
			{
				throw new Exception($"Couldn't find submesh mapping for {submesh}");
			}

			var triMapping = innermap[triangleIndex];
			var vox = Mesh.Voxels.Where(v => v.Key == triMapping.Coordinate);
			if (!vox.Any())
			{
				return null;
			}
			return vox.Single().Value;
		}
	}
}