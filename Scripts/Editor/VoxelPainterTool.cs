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

		public eMirrorMode MirrorMode
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainter_MirrorMode", eMirrorMode.None);
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainter_MirrorMode", value);
			}
		}

		private List<string> m_brushes = new List<string>();
		protected static VoxelMaterial DefaultMaterial => new VoxelMaterial { Default = new SurfaceData { Albedo = Color.white } };

		protected virtual bool DrawSceneGUIWithNoSelection => false;

		protected static VoxelMaterialAsset m_asset;

		protected static VoxelMaterial CurrentBrush
		{
			get
			{
				if (!m_asset)
				{
					m_asset = ScriptableObject.CreateInstance<VoxelMaterialAsset>();
					m_asset.Material = EditorPrefUtility.GetPref("VoxelPainter_Brush", DefaultMaterial);
				}
				return m_asset.Material;
			}
			set
			{
				if (!m_asset)
				{
					m_asset = ScriptableObject.CreateInstance<VoxelMaterialAsset>();
				}
				m_asset.Material = value;
				EditorUtility.SetDirty(m_asset);
			}
		}

		protected abstract EPaintingTool ToolID { get; }

		protected virtual bool GetVoxelDataFromPoint(
			VoxelPainter voxelPainter,
			VoxelRenderer renderer,
			MeshCollider collider,
			Vector3 hitPoint,
			Vector3 hitNorm,
			int triIndex,
			out HashSet<VoxelCoordinate> selection,
			out EVoxelDirection hitDir)
		{
			VoxelCoordinate.VectorToDirection(renderer.transform.worldToLocalMatrix.MultiplyVector(hitNorm).ClosestAxisNormal(), out hitDir);
			var voxelN = renderer.GetVoxel(hitPoint, hitNorm);
			if (voxelN.HasValue)
			{
				selection = new HashSet<VoxelCoordinate> { voxelN.Value.Coordinate };
				return true;
			}

			selection = null;
			return false;
		}

		public void DrawSceneGUI(VoxelPainter voxelPainter, VoxelRenderer renderer, Event currentEvent)
		{
			// Block all clicks from propagating to the scene view.
			HandleUtility.AddDefaultControl(-1);

			Event e = Event.current;
			if (e.type == EventType.MouseUp && e.isMouse && !voxelPainter.Deadzones.Any(d => d.Contains(currentEvent.mousePosition)))
			{
				// This returns a picked object as it normally does, but does not select it yet.
				GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);

				if (picked == renderer.gameObject || !picked)
				{
					// We select it, if valid for our needs.
					Selection.activeObject = renderer.gameObject;
					//e.Use();
				}
			}

			DrawToolsGUI(currentEvent, voxelPainter);
			if (!renderer.Mesh)
			{
				renderer.SetupComponents(true);
				return;
			}

			if (voxelPainter.Deadzones.Any(d => d.Contains(Event.current.mousePosition)))
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
					hitNorm = renderer.transform.up;
				}
			}
			Handles.DrawWireCube(hitPoint, Vector3.one * .02f);
			Handles.DrawLine(hitPoint, hitPoint + hitNorm * .2f);
			if (!GetVoxelDataFromPoint(voxelPainter, renderer, collider, hitPoint, hitNorm, triIndex,
					out var selection, out var hitDir) && !DrawSceneGUIWithNoSelection)
			{
				return;
			}

			if (selection != null)
			{
				for (int i = 0; i < selection.Count; i++)
				{
					VoxelCoordinate coord = selection.ElementAt(i);

					switch (MirrorMode)
					{
						case eMirrorMode.X:
							selection.Add(new VoxelCoordinate(-coord.X, coord.Y, coord.Z, coord.Layer));
							break;
						case eMirrorMode.Y:
							selection.Add(new VoxelCoordinate(coord.X, -coord.Y, coord.Z, coord.Layer));
							break;
						case eMirrorMode.Z:
							selection.Add(new VoxelCoordinate(coord.X, coord.Y, -coord.Z, coord.Layer));
							break;
					}

					var layerScale = VoxelCoordinate.LayerToScale(coord.Layer);
					var voxelWorldPos = coord.ToVector3();
					var voxelScale = layerScale * Vector3.one * .51f;
					Handles.matrix = renderer.transform.localToWorldMatrix;
					HandleExtensions.DrawWireCube(voxelWorldPos, voxelScale, Quaternion.identity, Color.cyan);
					Handles.Label(voxelWorldPos, coord.ToString(), EditorStyles.textField);
				}
			}

			if (currentEvent.alt)
			{
				if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
				{
					var vox = selection.FirstOrDefault();
					CurrentBrush = voxelPainter.Renderer.Mesh.Voxels[vox].Material.Copy();
					if (ToolID == EPaintingTool.Paint)
					{
						var cb = CurrentBrush;
						cb.Overrides = new DirectionOverride[0];
						var surface = CurrentBrush.GetSurface(hitDir);
						cb.Default = surface;
						CurrentBrush = cb;
					}
					UseEvent(currentEvent);
					UnityMainThreadDispatcher.EnsureSubscribed();
					UnityMainThreadDispatcher.Enqueue(() => Selection.activeObject = voxelPainter.Renderer.gameObject);
				}
				return;
			}

			if (DrawSceneGUIInternal(voxelPainter, renderer, currentEvent, selection, hitDir, hitPoint))
			{
				renderer.Mesh.Hash = System.Guid.NewGuid().ToString();
				EditorUtility.SetDirty(renderer.Mesh);
			}
		}

		protected void UseEvent(Event ev)
		{
			ev.Use();
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

			var settingsRect = new Rect(5, voxelPainter.ToolsPanelHeight + 15, 100, GetToolWindowHeight());
			voxelPainter.Deadzones.Add(settingsRect);
			GUILayout.BeginArea(settingsRect, "Brush", "Window");
			DrawToolLayoutGUI(settingsRect, currentEvent, voxelPainter);
			GUILayout.EndArea();
			Handles.EndGUI();
		}

		protected virtual int GetToolWindowHeight() => 45;

		protected virtual void DrawToolLayoutGUI(Rect rect, Event currentEvent, VoxelPainter voxelPainter)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label(EditorGUIUtility.IconContent("Mirror")
				.WithTooltip("Painting Mirroring Mode"));
			if (GUILayout.Button("X", EditorStyles.miniButtonLeft
				.WithColor(MirrorMode == eMirrorMode.X ? Color.green : Color.white)
				.Bold(MirrorMode == eMirrorMode.X)))
			{
				if (MirrorMode == eMirrorMode.X)
				{
					MirrorMode = eMirrorMode.None;
				}
				else
				{
					MirrorMode = eMirrorMode.X;
				}
			}
			if (GUILayout.Button("Y", EditorStyles.miniButtonMid
				.WithColor(MirrorMode == eMirrorMode.Y ? Color.green : Color.white)
				.Bold(MirrorMode == eMirrorMode.Y)))
			{
				if (MirrorMode == eMirrorMode.Y)
				{
					MirrorMode = eMirrorMode.None;
				}
				else
				{
					MirrorMode = eMirrorMode.Y;
				}
			}
			if (GUILayout.Button("Z", EditorStyles.miniButtonRight
				.WithColor(MirrorMode == eMirrorMode.Z ? Color.green : Color.white)
				.Bold(MirrorMode == eMirrorMode.Z)))
			{
				if (MirrorMode == eMirrorMode.Z)
				{
					MirrorMode = eMirrorMode.None;
				}
				else
				{
					MirrorMode = eMirrorMode.Z;
				}
			}
			GUILayout.EndHorizontal();

		}

		public virtual bool DrawInspectorGUI(VoxelPainter voxelPainter)
		{
			if (!m_asset)
			{
				return false;
			}
			GUILayout.BeginVertical("Box");
			GUILayout.Label("Presets");
			var selIndex = GUILayout.SelectionGrid(-1,
				m_brushes.Select(b => new GUIContent(Path.GetFileNameWithoutExtension(b))).ToArray(), 3);
			if (selIndex >= 0)
			{
				CurrentBrush = AssetDatabase.LoadAssetAtPath<VoxelMaterialAsset>(m_brushes.ElementAt(selIndex)).Material;
				voxulLogger.Debug($"Loaded brush from {m_brushes.ElementAt(selIndex)}");
			}
			GUILayout.EndVertical();
			var wrapper = NativeEditorUtility.GetWrapper<VoxelMaterialAsset>();
			wrapper.DrawGUI(m_asset);
			EditorPrefUtility.SetPref("VoxelPainter_Brush", CurrentBrush);
			return false;
		}

		protected abstract bool DrawSceneGUIInternal(VoxelPainter painter, VoxelRenderer Renderer, Event currentEvent,
			HashSet<VoxelCoordinate> selection, EVoxelDirection hitDir, Vector3 hitPos);

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