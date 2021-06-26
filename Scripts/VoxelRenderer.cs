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
		public VoxelMesh Mesh;

		[Serializable]
		public class UnityRendererInstance
		{
			public GameObject GameObject;
			public MeshFilter MeshFilter;
			public MeshRenderer MeshRenderer;
			public MeshCollider MeshCollider;

			public void SetupComponents(GameObject gameObject, bool collider)
			{
				if (!GameObject)
				{
					GameObject = new GameObject("RendererChunk");
					GameObject.transform.SetParent(gameObject.transform);
				}
				GameObject.hideFlags = HideFlags.HideInHierarchy;
				if (!MeshFilter)
				{
					MeshFilter = GameObject.GetOrAddComponent<MeshFilter>();
				}
				if (!MeshRenderer)
				{
					MeshRenderer = GameObject.GetOrAddComponent<MeshRenderer>();
				}
				if (collider)
				{
					if (!MeshCollider)
					{
						MeshCollider = GameObject.GetOrAddComponent<MeshCollider>();
					}

					MeshCollider.convex = false;
				}
			}

			public Bounds Bounds => MeshRenderer.bounds;
		}

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


		public List<UnityRendererInstance> Renderers = new List<UnityRendererInstance>();

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
		}

		public void SetupComponents(bool forceCollider)
		{
			foreach(var r in Renderers)
			{
				r.SetupComponents(gameObject, GenerateCollider || forceCollider);
			}
		}

		[ContextMenu("Force Redraw")]
		public void ForceRedraw()
		{
			SetupComponents(false);
			Mesh.Invalidate();
			Invalidate(false);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
		}

		public virtual void Invalidate(bool forceCollider)
		{
			//Debug.Log($"Invalidated {this}", this);
			m_isDirty = false;
			if (!Mesh)
			{
				return;
			}
			if (MinLayer > MaxLayer)
			{
				MinLayer = MaxLayer;
			}

			var newMeshes = Mesh.GenerateMeshInstance(MinLayer, MaxLayer).ToList();
			for (int i = 0; i < newMeshes.Count; i++)
			{
				Mesh mesh = newMeshes[i];
				UnityRendererInstance r;
				if (Renderers.Count <= newMeshes.Count)
				{
					r = new UnityRendererInstance();
					Renderers.Add(r);
				}
				else
				{
					r = Renderers[i];
				}
				r.SetupComponents(gameObject, forceCollider || GenerateCollider);
				r.MeshFilter.sharedMesh = mesh;
				if (GenerateCollider)
				{
					r.MeshCollider.sharedMesh = mesh;
				}
				if (!CustomMaterials)
				{
					if (Mesh.Voxels.Any(v => v.Value.Material.MaterialMode == EMaterialMode.Transparent))
					{
						r.MeshRenderer.sharedMaterials = new[] { VoxelManager.Instance.DefaultMaterial, VoxelManager.Instance.DefaultMaterialTransparent, };
					}
					else if (r.MeshRenderer)
					{
						r.MeshRenderer.sharedMaterials = new[] { VoxelManager.Instance.DefaultMaterial, };
					}
				}
			}
			
			for(var i = Renderers.Count - 1; i >= newMeshes.Count; --i)
			{
				var r = Renderers[i];
				r.GameObject.SafeDestroy();
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