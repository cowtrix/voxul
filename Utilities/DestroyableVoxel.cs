using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;
using Voxul;
using Voxul.Meshing;

namespace Voxul
{
	[RequireComponent(typeof(VoxelRenderer))]
	public class DestroyableVoxel : MonoBehaviour
	{
		public bool ReadyToDestroy => m_gibSpawnCount < 0;
		private int m_gibSpawnCount;

		const int GibLayer = 11;

		[Tooltip("Voxels above this layer will subdivide themselves")]
		public sbyte SegmentationLayer;
		[Range(1, 10)]
		public int MaxChunkSize = 5;
		public GameObject HealthEffect;
		public float ExplosionForce = 400;
		public float ExplosionDistance = 10;
		public bool WholeObject
		{
			get
			{
				var mc = GetComponent<MeshCollider>();
				return !mc || mc.convex;
			}
		}
		public int Health { get; set; }

		public VoxelRenderer Renderer => GetComponent<VoxelRenderer>();
		public VoxelMesh Mesh => Renderer.Mesh;

		private void Start()
		{
			Renderer.Mesh = Instantiate(Renderer.Mesh);
			Mesh.Mesh = null; // To make sure we make our own
			Health = Math.Max(1, Mesh.Voxels.Values.Count(IsHealthBlock));
		}

		public static bool IsHealthBlock(Voxel v)
		{
			if (v.Coordinate.Layer > 1)
			{
				return false;
			}
			var total = (v.Material.Default.Albedo.Luminosity() + v.Material.Overrides.Sum(z => z.Data.Albedo.Luminosity()))
					/ (1 + v.Material.Overrides.Length);
			return total > 1;
		}

		public bool DestroyVoxel(Voxel v, float force, Vector3 hitPoint)
		{
			//Debug.Log($"Destroying a voxel: {v.Coordinate}");
			if (!Mesh.Voxels.Remove(v.Coordinate))
			{
				return false;
			}
			// Hit
			if (IsHealthBlock(v))
			{
				if (Health > 0)
				{
					Health--;
					if (HealthEffect)
					{
						Instantiate(HealthEffect).transform.position =
							transform.localToWorldMatrix.MultiplyPoint3x4(v.Coordinate.ToVector3());
					}
				}
				return false;
			}
			return true;
		}

		public void Gib(float force, Vector3 normal, IEnumerable<Voxel> voxels)
		{
			m_gibSpawnCount++;
			var gib = new GameObject("gib");
			gib.transform.position = transform.position;
			gib.layer = GibLayer;
			if (voxels.Count() == 1)
			{
				var v = voxels.Single();
				var r = gib.AddComponent<SingleVoxelRenderer>();
				r.Voxel = v;
				r.Invalidate();
				var bc = gib.AddComponent<BoxCollider>();
				bc.size *= .95f;
			}
			else
			{
				var r = gib.AddComponent<VoxelRenderer>();
				r.GenerateCollider = true;
				r.Mesh = ScriptableObject.CreateInstance<VoxelMesh>();
				foreach (var v in voxels)
				{
					r.Mesh.Voxels.Add(v.Coordinate, v);
				}
				r.Mesh.Invalidate();
				r.Invalidate(false);
				gib.GetComponent<MeshCollider>().convex = true;
			}
			gib.AddComponent<DestroyInTime>();
			var rb = gib.AddComponent<Rigidbody>();
			rb.gameObject.AddComponent<GravityRigidbody>();
			rb.drag = 1;
			rb.angularDrag = .8f;
			rb.AddForce(ExplosionForce * force * normal, ForceMode.Force);
			rb.AddTorque(Vector3.one * UnityEngine.Random.value * force);
			m_gibSpawnCount--;
		}

		private void OnDisable()
		{
			if (!HealthEffect || !!Application.isPlaying
#if UNITY_EDITOR
			|| !UnityEditor.EditorApplication.isPlaying
#endif
			)
			{
				return;
			}
			Instantiate(HealthEffect).transform.position = transform.position;
		}
	}

}