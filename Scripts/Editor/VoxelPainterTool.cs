using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	internal abstract class VoxelPainterTool
	{
		public abstract GUIContent Icon { get; }

		private Editor m_cachedBrushEditor;
		private bool m_cachedEditorNeedsRefresh = true;
		private List<string> m_brushes = new List<string>();
		protected static VoxelMaterial DefaultMaterial => new VoxelMaterial { Default = new SurfaceData { Albedo = Color.white } };

		protected static VoxelMaterialAsset m_asset;

		protected static VoxelMaterial CurrentBrush
		{
			get
			{
				if (!m_asset)
				{
					m_asset = ScriptableObject.CreateInstance<VoxelMaterialAsset>();
					m_asset.Data = EditorPrefUtility.GetPref("VoxelPainter_Brush", DefaultMaterial);
				}
				return m_asset.Data;
			}
			set
			{
				if (!m_asset)
				{
					m_asset = ScriptableObject.CreateInstance<VoxelMaterialAsset>();
				}
				m_asset.Data = value;
				EditorUtility.SetDirty(m_asset);
			}
		}

		protected abstract EPaintingTool ToolID { get; }

		protected virtual bool GetVoxelDataFromPoint(
			VoxelPainter voxelPainterTool,
			VoxelRenderer renderer,
			MeshCollider collider,
			Vector3 hitPoint,
			Vector3 hitNorm,
			int triIndex,
			sbyte layer,
			out List<VoxelCoordinate> selection,
			out EVoxelDirection hitDir)
		{
			hitNorm = renderer.transform.worldToLocalMatrix.MultiplyVector(hitNorm);
			VoxelCoordinate.VectorToDirection(hitNorm, out hitDir);
			var voxelN = renderer.GetVoxel(hitPoint, hitNorm);
			if (voxelN.HasValue)
			{
				selection = new List<VoxelCoordinate> { voxelN.Value.Coordinate };
				return true;
			}

			selection = null;
			return false;
		}

		public void DrawSceneGUI(VoxelPainter voxelPainter, VoxelRenderer renderer, Event currentEvent, sbyte painterLayer)
		{
			DrawToolsGUI(currentEvent, voxelPainter);
			if (!renderer.Mesh)
			{
				renderer.SetupComponents(true);
				return;
			}

			if(voxelPainter.Deadzones.Any(d => d.Contains(Event.current.mousePosition)))
			{
				return;
			}

			Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			var hitPoint = Vector3.zero;
			var hitNorm = Vector3.up;
			var triIndex = -1;
			MeshCollider collider = null;
			foreach (var r in renderer.Renderers)
			{
				if (!r.MeshCollider)
				{
					continue;
				}
				if (r.MeshCollider.Raycast(worldRay, out var hitInfo, 10000))
				{
					hitPoint = hitInfo.point;
					hitNorm = hitInfo.normal;
					triIndex = hitInfo.triangleIndex;
					collider = r.MeshCollider;
				}
			}
			if (!collider)
			{
				var p = new Plane(renderer.transform.up, -renderer.transform.position.y); ;
				if (p.Raycast(worldRay, out var planePoint))
				{
					hitPoint = worldRay.origin + worldRay.direction * planePoint;
					hitNorm = renderer.transform.up; ;
				}
			}
			Handles.DrawWireCube(hitPoint, Vector3.one * .02f);
			Handles.DrawLine(hitPoint, hitPoint + hitNorm * .2f);
			if (!GetVoxelDataFromPoint(voxelPainter, renderer, collider, hitPoint, hitNorm, triIndex, painterLayer,
					out var selection, out var hitDir) && ToolID != EPaintingTool.Clipboard)
			{
				return;
			}

			if (selection != null)
			{
				foreach (var brushCoord in selection)
				{
					var layerScale = VoxelCoordinate.LayerToScale(brushCoord.Layer);
					var voxelWorldPos = brushCoord.ToVector3();
					var voxelScale = layerScale * Vector3.one * .51f;
					Handles.matrix = renderer.transform.localToWorldMatrix;
					HandleExtensions.DrawWireCube(voxelWorldPos, voxelScale, Quaternion.identity, Color.cyan);
					Handles.Label(voxelWorldPos, brushCoord.ToString(), EditorStyles.textField);
				}
			}

			if (currentEvent.alt)
			{
				if(currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
				{
					var vox = selection.First();
					CurrentBrush = voxelPainter.Renderer.Mesh.Voxels[vox].Material.Copy();
					if (ToolID == EPaintingTool.Paint)
					{
						var cb = CurrentBrush;
						cb.Overrides = null;
						var surface = CurrentBrush.GetSurface(hitDir);
						cb.Default = surface;
						CurrentBrush = cb;
					}
					currentEvent.Use();
				}
				return;
			}
			
			if (DrawSceneGUIInternal(voxelPainter, renderer, currentEvent, selection, hitDir))
			{
				renderer.Mesh.Hash = System.Guid.NewGuid().ToString();
				EditorUtility.SetDirty(renderer.Mesh);
				Event.current.Use();
			}

		}

		public virtual void DrawToolsGUI(Event currentEvent, VoxelPainter voxelPainter)
		{
			Handles.BeginGUI();
			var screenViewRect = SceneView.currentDrawingSceneView.position;
			var statusWidth = 160f;
			var rect = new Rect(screenViewRect.width / 2 - statusWidth / 2, 5, statusWidth, 25);
			if (currentEvent.alt)
			{
				GUI.Label(rect,
					"PICKING\nRelease ALT to stop"
					, "Box");				
				return;
			}
			else
			{
				GUI.Label(rect,
				"ALT to change to picker", "Box");
			}
		}

		public virtual bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			bool dirty = false;
			GUILayout.BeginVertical("Box");
			GUILayout.Label("Presets");
			var selIndex = GUILayout.SelectionGrid(-1,
				m_brushes.Select(b => new GUIContent(Path.GetFileNameWithoutExtension(b))).ToArray(), 3);
			if (selIndex >= 0)
			{
				dirty = true;
				CurrentBrush = AssetDatabase.LoadAssetAtPath<VoxelMaterialAsset>(m_brushes.ElementAt(selIndex)).Data;
				voxulLogger.Debug($"Loaded brush from {m_brushes.ElementAt(selIndex)}");
			}
			GUILayout.EndVertical();
			if (dirty || m_cachedBrushEditor == null || !m_cachedBrushEditor || m_cachedEditorNeedsRefresh || Event.current.alt)
			{
				if (m_cachedBrushEditor)
				{
					UnityEngine.Object.DestroyImmediate(m_cachedBrushEditor);
				}
				m_cachedBrushEditor = Editor.CreateEditor(m_asset);
				m_cachedEditorNeedsRefresh = false;
			}
			m_cachedBrushEditor?.DrawDefaultInspector();
			EditorPrefUtility.SetPref("VoxelPainter_Brush", CurrentBrush);
			return false;
		}

		protected abstract bool DrawSceneGUIInternal(VoxelPainter painter, VoxelRenderer Renderer, Event currentEvent,
			List<VoxelCoordinate> selection, EVoxelDirection hitDir);

		public virtual void OnEnable()
		{
			m_brushes = AssetDatabase.FindAssets($"t: {nameof(VoxelMaterialAsset)}")
				.Select(b => AssetDatabase.GUIDToAssetPath(b))
				.ToList();
		}

		public virtual void OnDisable()
		{
		}
	}
}