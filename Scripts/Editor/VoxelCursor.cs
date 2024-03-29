﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	internal class VoxelCursor
	{
		public Matrix4x4 Matrix { get; private set; }
		private VoxelMesh m_tempVoxelData;
		private Mesh m_cursorMesh;
		private VoxelRenderer m_cursorRenderer;
		private VoxelRenderer m_parentRenderer;
		private AutoDestroyer m_keepAlive;
		private bool m_dirty;

		private Material DebugMaterial => Resources.Load<Material>($"{VoxelManager.RESOURCES_FOLDER}/voxul_Selection");

		public VoxelCursor(VoxelRenderer parent)
		{
			m_parentRenderer = parent;
			m_tempVoxelData = ScriptableObject.CreateInstance<VoxelMesh>();
		}

		public void SetData(Matrix4x4 trs, IEnumerable<Voxel> data = null)
		{
			Matrix = trs;
			if (data != null)
			{
				if (m_cursorMesh == null)
				{
					m_cursorMesh = new Mesh();
				}
				m_tempVoxelData.Voxels = data.Select(v =>
				{
					v.Material.MaterialMode = EMaterialMode.Opaque;
					v.Material.Default.UVMode = EUVMode.Local;
					v.Material.Overrides = null;
					return v;
				}).Finalise();
				var debugMat = DebugMaterial;
				if (!debugMat)
				{
					voxulLogger.Error($"Failed to load debug selection material at {VoxelManager.RESOURCES_FOLDER}/voxul_Selection");
				}
				m_tempVoxelData.Invalidate();
				m_dirty = true;
			}
			Update();
		}

		public void Update()
		{
			if (m_cursorMesh == null)
			{
				Destroy();
				return;
			}
			if (!m_cursorRenderer)
			{
				var go = new GameObject("__cursorRenderer");
				if (m_parentRenderer)
				{
					SceneManager.MoveGameObjectToScene(go, m_parentRenderer.gameObject.scene);
				}
				go.hideFlags = HideFlags.HideAndDontSave;
				m_keepAlive = go.AddComponent<AutoDestroyer>();
				m_cursorRenderer = go.AddComponent<VoxelRenderer>();
				m_cursorRenderer.Mesh = m_tempVoxelData;
				m_cursorRenderer.CustomMaterials = true;
				m_cursorRenderer.OpaqueMaterial = DebugMaterial;
				m_cursorRenderer.TransparentMaterial = DebugMaterial;
			}
			m_cursorRenderer.GenerateCollider = false;
			m_cursorRenderer.Mesh.OverrideOptimisers = true;
			m_keepAlive?.KeepAlive();
			if (m_dirty)
			{
				m_cursorRenderer.Invalidate(true, false);
				m_dirty = false;
			}
			m_cursorRenderer.transform.ApplyTRSMatrix(Matrix);
		}

		public void Destroy()
		{
			if (m_cursorRenderer)
			{
				m_cursorRenderer.gameObject.SafeDestroy();
				m_cursorRenderer = null;
			}
		}
	}
}