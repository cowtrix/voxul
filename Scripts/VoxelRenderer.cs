using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul
{
	[SelectionBase]
	public class VoxelRenderer : MonoBehaviour, ISerializationCallbackReceiver
	{
		public enum eSnapMode
		{
			None, Local, Global
		}

		public VoxelMesh Mesh;

		[Header("Settings")]
		public bool CustomMaterials;
		public Material OpaqueMaterial;
		public Material TransparentMaterial;

		public bool GenerateCollider = true;
		[HideInInspector]
		[Obsolete("Replaced by SnapMode")]
		public bool SnapToGrid;
		public eSnapMode SnapMode;
		[Range(VoxelCoordinate.MIN_LAYER, VoxelCoordinate.MAX_LAYER)]
		public sbyte SnapLayer = 0;

		[Header("Rendering")]
		public EThreadingMode ThreadingMode;

		public float MaxCoroutineUpdateTime = 0.5f;

		protected bool m_isDirty;

		[SerializeField]
		[HideInInspector]
		protected VoxelMeshWorker m_voxWorker;

		protected virtual VoxelMeshWorker GetVoxelMeshWorker()
		{
			if (m_voxWorker == null)
			{
				m_voxWorker = new VoxelMeshWorker(Mesh);
			}
			return m_voxWorker;
		}

		[SerializeField]
		[HideInInspector]
		private string m_lastMeshHash;

		[FormerlySerializedAs("Renderers")]
		[SerializeField]
		public List<VoxelRendererSubmesh> Submeshes = new List<VoxelRendererSubmesh>();

		public Bounds Bounds => Submeshes.Select(b => b.Bounds).EncapsulateAll();

		/*protected virtual void Update()
		{
			if (SnapMode != eSnapMode.None)
			{
				var scale = VoxelCoordinate.LayerToScale(SnapLayer);
				if (SnapMode == eSnapMode.Local)
				{
					transform.localPosition = transform.localPosition.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
				else if (SnapMode == eSnapMode.Global)
				{
					transform.position = transform.position.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
			}
			if (m_isDirty || Mesh?.Hash != m_lastMeshHash)
			{
				Invalidate(false, false);
			}
		}*/

		private void Reset()
		{
			ThreadingMode = VoxelManager.Instance.DefaultThreadingMode;
			MaxCoroutineUpdateTime = VoxelManager.Instance.DefaultMaxCoroutineUpdateTime;
		}

		protected virtual void Awake()
		{
			VoxelManager.Instance.OnValidate();
		}

		public void SetDirty() => m_isDirty = true;

		[ContextMenu("Clear")]
		public void ClearMesh()
		{
			if (!Util.PromptEditor($"Clear Mesh {this.Mesh}?", "Are you sure you want to clear this mesh and delete all of its data permanently?", "Yes, I'm sure"))
			{
				return;
			}
			Mesh.Voxels.Clear();
			Mesh.Invalidate();
			OnClear();
		}

		[ContextMenu("Clean Submeshes")]
		public void CleanSubmeshes()
		{
			foreach (var submesh in Submeshes)
			{
				if (!submesh)
				{
					continue;
				}
				if (submesh.gameObject != gameObject)
				{
					submesh.gameObject.SafeDestroy();
				}
				else
				{
					submesh.SafeDestroy();
				}
			}
			Submeshes.Clear();
		}

		protected virtual void OnClear() { }

		public void SetupComponents(bool forceCollider)
		{
			Submeshes = new List<VoxelRendererSubmesh>(GetComponentsInChildren<VoxelRendererSubmesh>()
				.Where(r => r.Parent == this));
			foreach (var r in Submeshes)
			{
				r.SetupComponents(this, GenerateCollider || forceCollider);
			}
		}

		[ContextMenu("Force Redraw")]
		public void ForceRedraw()
		{
			Mesh?.Invalidate();
			Invalidate(true, false);
		}

		public virtual void Invalidate(bool force, bool forceCollider)
		{
			m_isDirty = false;
			if (!Mesh)
			{
				foreach (var submesh in Submeshes)
				{
					submesh.MeshFilter.sharedMesh = null;
					submesh.MeshRenderer.sharedMaterial = null;
					if (submesh.MeshCollider)
					{
						submesh.MeshCollider.sharedMesh = null;
					}
				}
				return;
			}

			Profiler.BeginSample("Invalidate");
			SetupComponents(forceCollider || GenerateCollider);

			Mesh.CurrentWorker = GetVoxelMeshWorker();
			Mesh.CurrentWorker.OnCompleted -= OnMeshRebuilt;
			Mesh.CurrentWorker.OnCompleted += OnMeshRebuilt;
			Mesh.CurrentWorker.VoxelMesh = Mesh;
			Mesh.CurrentWorker.GenerateMesh(ThreadingMode, force);

			m_lastMeshHash = Mesh.Hash;
			this.TrySetDirty();
			Profiler.EndSample();
		}

		protected virtual void OnMeshRebuilt(VoxelMeshWorker worker, VoxelMesh voxelMesh)
		{
			if (!this)
			{
				// Object has been destroyed
				return;
			}
			voxulLogger.Debug($"VoxelRenderer.OnMeshRebuilt: {voxelMesh}, {this}", this);
			if (Mesh.Hash != voxelMesh.Hash)
			{
				voxulLogger.Error("Unexpected hash!");
				return;
			}
			for (int i = 0; i < voxelMesh.UnityMeshInstances.Count; i++)
			{
				var data = voxelMesh.UnityMeshInstances[i];
				var unityMesh = data.UnityMesh;

				VoxelRendererSubmesh submesh;
				if (Submeshes.Count < voxelMesh.UnityMeshInstances.Count)
				{
					if (i == 0)
					{
						submesh = gameObject.GetOrAddComponent<VoxelRendererSubmesh>();
					}
					else
					{
						submesh = new GameObject($"{name}_submesh_hidden_{i}")
							.AddComponent<VoxelRendererSubmesh>();
						submesh.transform.SetParent(transform);
					}
					Submeshes.Add(submesh);
				}
				else
				{
					submesh = Submeshes[i];
				}
				submesh.SetupComponents(this, GenerateCollider);
				submesh.MeshFilter.sharedMesh = unityMesh;
				if (GenerateCollider)
				{
					voxulLogger.Debug($"Set MeshCollider mesh");
					unityMesh.MarkDynamic();
					unityMesh.MarkModified();
					submesh.MeshCollider.sharedMesh = unityMesh;
				}
				if (!CustomMaterials)
				{
					var vm = VoxelManager.Instance;
					if (!vm.DefaultMaterial || !vm.DefaultMaterialTransparent)
					{
						vm.OnValidate();
					}
					SetMaterials(submesh, VoxelManager.Instance.DefaultMaterial, VoxelManager.Instance.DefaultMaterialTransparent);
				}
				else
				{
					SetMaterials(submesh, OpaqueMaterial, TransparentMaterial);
				}
			}
			for (var i = Submeshes.Count - 1; i >= Mesh.UnityMeshInstances.Count; --i)
			{
				var r = Submeshes[i];
				if (r || (r != null && r.gameObject))
				{
					voxulLogger.Debug($"Destroying submesh renderer {r}", this);
					if (i == 0)
					{
						r.SafeDestroy();
					}
					else
					{
						r.gameObject.SafeDestroy();
					}
				}
				Submeshes.RemoveAt(i);
			}
			m_lastMeshHash = Mesh.Hash;
#if UNITY_EDITOR
			foreach (var r in Submeshes)
			{
				r.SetDirty();
			}
			UnityEditor.EditorUtility.SetDirty(gameObject);
			UnityEditor.EditorUtility.SetDirty(Mesh);
#endif
		}

		private void SetMaterials(VoxelRendererSubmesh submesh, Material opaque, Material transparent)
		{
			lock (Mesh)
			{
				if (Mesh.Voxels.Any(v => v.Value.Material.MaterialMode == EMaterialMode.Transparent))
				{
					submesh.MeshRenderer.sharedMaterials = new[] { opaque, transparent, };
				}
				else if (submesh.MeshRenderer)
				{
					submesh.MeshRenderer.sharedMaterials = new[] { opaque, };
				}
			}
		}

		public Voxel? GetVoxel(Vector3 worldPos, Vector3 worldNormal)
		{
			var localCoord = transform.worldToLocalMatrix.MultiplyPoint3x4(worldPos);
			var localNormal = transform.worldToLocalMatrix.MultiplyVector(worldNormal)
				.ClosestAxisNormal();
			localCoord -= localNormal * .001f;
			foreach (var v in Mesh.Voxels)
			{
				if (v.Key.ToBounds().Contains(localCoord))
				{
					return v.Value;
				}
			}
			return null;
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			if (SnapToGrid)
			{
				SnapToGrid = false;
				SnapMode = eSnapMode.Local;
			}
		}
	}
}