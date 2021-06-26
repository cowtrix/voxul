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
		[SerializeField]
		[HideInInspector]
		private double m_lastUpdateTime;

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

		private bool m_isDirty;

		[SerializeField]
		[HideInInspector]
		private string m_lastMeshHash;

		[SerializeField]
		public List<VoxelRendererSubmesh> Renderers = new List<VoxelRendererSubmesh>();

		public Bounds Bounds => Renderers.Select(b => b.Bounds).EncapsulateAll();

		protected virtual void Update()
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
		
		[ContextMenu("Clear")]
		public void ClearMesh()
		{
			Mesh.Invalidate();
			Mesh.Voxels.Clear();
			OnClear();
		}

		protected virtual void OnClear() { }

		public void SetupComponents(bool forceCollider)
		{
			Renderers = new List<VoxelRendererSubmesh>(GetComponentsInChildren<VoxelRendererSubmesh>());
			foreach(var r in Renderers)
			{
				r.SetupComponents(GenerateCollider || forceCollider);
			}
		}

		[ContextMenu("Force Redraw")]
		public void ForceRedraw()
		{
			foreach(Transform t in transform)
			{
				t.gameObject.SafeDestroy();
			}
			SetupComponents(false);
			if (!Mesh)
			{
				Debug.LogWarning($"Tried to force redraw {this}, but it doesn't have a voxel mesh.", this);
				return;
			}
			Mesh.Invalidate();
			Invalidate(false);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
		}

		protected virtual bool ShouldInvalidate()
		{
			return Util.GetDynamicTime() > m_lastUpdateTime + VoxelManager.Instance.MinimumUpdateTime;
		}


		public virtual void Invalidate(bool forceCollider)
		{
			if (!ShouldInvalidate())
			{
				return;
			}
			m_lastUpdateTime = Util.GetDynamicTime();
			m_isDirty = false;
			if (!Mesh)
			{
				return;
			}
			if (MinLayer > MaxLayer)
			{
				MinLayer = MaxLayer;
			}

			Mesh.Meshes.Clear();
			var newMeshes = Mesh.GenerateMeshInstance(MinLayer, MaxLayer).ToList();
			for (int i = 0; i < newMeshes.Count; i++)
			{
				var data = newMeshes[i];
				var mesh = data.Mesh;
				VoxelRendererSubmesh submesh;
				if (Renderers.Count <= newMeshes.Count)
				{
					submesh = new GameObject($"{name}_submesh_hidden")
						.AddComponent<VoxelRendererSubmesh>();
					submesh.transform.SetParent(transform);
					Renderers.Add(submesh);
				}
				else
				{
					submesh = Renderers[i];
				}
				submesh.SetupComponents(forceCollider || GenerateCollider);
				submesh.MeshFilter.sharedMesh = mesh;
				if (GenerateCollider)
				{
					submesh.MeshCollider.sharedMesh = mesh;
				}
				if (!CustomMaterials)
				{
					if (Mesh.Voxels.Any(v => v.Value.Material.MaterialMode == EMaterialMode.Transparent))
					{
						submesh.MeshRenderer.sharedMaterials = new[] { VoxelManager.Instance.DefaultMaterial, VoxelManager.Instance.DefaultMaterialTransparent, };
					}
					else if (submesh.MeshRenderer)
					{
						submesh.MeshRenderer.sharedMaterials = new[] { VoxelManager.Instance.DefaultMaterial, };
					}
				}
			}
			for (int i = 0; i < Mesh.Meshes.Count; i++)
			{
				var m = Mesh.Meshes[i];
				Renderers[i].MeshFilter.sharedMesh = m.Mesh;
			}
			for (var i = Renderers.Count - 1; i >= Mesh.Meshes.Count; --i)
			{
				var r = Renderers[i];
				r.gameObject.SafeDestroy();
				Renderers.RemoveAt(i);
			}
			m_lastMeshHash = Mesh.Hash;
		}

		public Voxel? GetVoxel(Collider collider, int triangleIndex)
		{
			SetupComponents(false);
			var renderer = Renderers.SingleOrDefault(s => s.MeshCollider == collider);
			if (triangleIndex < 0 || !renderer?.MeshRenderer || !renderer.MeshFilter.sharedMesh)
			{
				SetDirty();
				return null;
			}

			var meshVoxelData = Mesh.Meshes.SingleOrDefault(m => m.Mesh == renderer.MeshFilter.sharedMesh);
			if (meshVoxelData == null)
			{
				return null;
			}

			int limit = triangleIndex * 3;
			int submesh;
			for (submesh = 0; submesh < renderer.MeshFilter.sharedMesh.subMeshCount; submesh++)
			{
				int numIndices = renderer.MeshFilter.sharedMesh.GetTriangles(submesh).Length;

				if (numIndices > limit)
					break;
				triangleIndex -= numIndices / 3;
				limit -= numIndices;
			}

			
			if (!meshVoxelData.VoxelMapping.TryGetValue(submesh, out var innermap))
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