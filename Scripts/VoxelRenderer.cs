using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	[SelectionBase]
	[ExecuteAlways]
	public class VoxelRenderer : MonoBehaviour
	{
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
		public EThreadingMode ThreadingMode = EThreadingMode.Task;

		protected bool m_isDirty;

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
				Invalidate(false, false);
			}
		}

		protected virtual void Awake()
		{
			VoxelManager.Instance.OnValidate();
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
				voxulLogger.Warning($"Tried to force redraw {this}, but it doesn't have a voxel mesh.", this);
				return;
			}
			Mesh.Invalidate();
			Invalidate(true, false);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
		}

		public virtual void Invalidate(bool force, bool forceCollider)
		{
			m_isDirty = false;
			if (!Mesh)
			{
				return;
			}
			if (MinLayer > MaxLayer)
			{
				MinLayer = MaxLayer;
			}

			SetupComponents(forceCollider || GenerateCollider);

			if(Mesh.CurrentWorker == null)
			{
				Mesh.CurrentWorker = new VoxelMeshWorker();
				Mesh.CurrentWorker.OnCompleted += OnMeshRebuilt;
			}
			Mesh.CurrentWorker.GenerateMesh(this, ThreadingMode, force, MinLayer, MaxLayer);

			m_lastMeshHash = Mesh.Hash;
		}

		protected virtual void OnMeshRebuilt(VoxelMeshWorker worker, VoxelMesh voxelMesh)
		{
			voxulLogger.Debug($"VoxelRenderer.OnMeshRebuilt: {voxelMesh}, {this}", this);
			if(Mesh.Hash != voxelMesh.Hash)
			{
				voxulLogger.Error("Unexpected hash!");
				return;
			}
			for (int i = 0; i < voxelMesh.UnityMeshInstances.Count; i++)
			{
				var data = voxelMesh.UnityMeshInstances[i];
				var unityMesh = data.UnityMesh;
				VoxelRendererSubmesh submesh;
				if (Renderers.Count <= voxelMesh.UnityMeshInstances.Count)
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
				submesh.SetupComponents(GenerateCollider);
				submesh.MeshFilter.sharedMesh = unityMesh;
				if (GenerateCollider)
				{
					submesh.MeshCollider.sharedMesh = unityMesh;
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
			for (int i = 0; i < Mesh.UnityMeshInstances.Count; i++)
			{
				var m = Mesh.UnityMeshInstances[i];
				Renderers[i].SetupComponents(false);
				Renderers[i].MeshFilter.sharedMesh = m.UnityMesh;
			}
			for (var i = Renderers.Count - 1; i >= Mesh.UnityMeshInstances.Count; --i)
			{
				var r = Renderers[i];
				if(r && r.gameObject)
				{
					continue;
				}
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

			var meshVoxelData = Mesh.UnityMeshInstances.SingleOrDefault(m => m.UnityMesh == renderer.MeshFilter.sharedMesh);
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