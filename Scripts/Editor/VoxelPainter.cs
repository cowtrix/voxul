#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Edit;
using Voxul.Meshing;
using Voxul.Utilities;

namespace Voxul.Edit
{
	internal enum EPaintingTool
	{
		Select,
		Add,
		Remove,
		Subdivide,
		Paint,
		Clipboard,
        Erase,
        Warp,
		Primitive,
    }

	internal enum eMirrorMode
	{
		None, X, Y, Z
	}

	[CustomEditor(typeof(VoxelRenderer), false)]
	[CanEditMultipleObjects]
	internal class VoxelPainter : VoxelObjectEditorBase<VoxelRenderer>
	{
		[MenuItem("GameObject/3D Object/voxul/Voxel Object")]
		public static void CreateNew()
		{
			CreateNewInScene();
		}

		[MenuItem("Tools/Voxul/Rebake All Renderers in Scene")]
		public static void RebakeAll()
		{
			var history = new HashSet<VoxelMesh>();
			foreach(var r in FindObjectsOfType<VoxelRenderer>())
			{
				if (history.Contains(r.Mesh))
				{
                    r.Invalidate(false, false);
                }
				else
				{
                    r.Invalidate(true, false);
                }
				history.Add(r.Mesh);
			}
		}

		[MenuItem("Tools/Voxul/Disable All Renderers in Scene")]
		public static void DisableAll()
		{
			foreach (var r in FindObjectsOfType<VoxelRenderer>())
			{
				r.enabled = false;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(r);
#endif
			}
		}

		[MenuItem("Tools/Voxul/Enable Lightmap Generation In Scene")]
		public static void EnableLightmaps()
		{
			foreach (var r in FindObjectsOfType<VoxelRenderer>())
			{
                if (!r.gameObject.isStatic || !r.Mesh)
                {
					continue;
                }
				r.Mesh.GenerateLightmaps = true;
				r.Mesh.Invalidate();
				r.Invalidate(true, false);
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(r);
#endif
			}
		}

		Dictionary<EPaintingTool, VoxelPainterTool> m_tools = new Dictionary<EPaintingTool, VoxelPainterTool>
		{
			{ EPaintingTool.Select, new SelectTool() },
			{ EPaintingTool.Add, new AddTool() },
			{ EPaintingTool.Remove, new RemoveTool() },
			{ EPaintingTool.Subdivide, new SubdivideTool() },
			{ EPaintingTool.Paint, new PaintTool() },
			{ EPaintingTool.Clipboard, new ClipboardTool() },
			{ EPaintingTool.Erase, new EraseTool() },
			//{ EPaintingTool.Warp, new WarpTool() },
			//{ EPaintingTool.Primitive, new PrimitiveTool() },
		};

		public EPaintingTool CurrentTool
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainter_CurrentTool", EPaintingTool.Add);
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainter_CurrentTool", value);
			}
		}

		public float ToolsPanelHeight { get; private set; }
		public HashSet<Rect> Deadzones = new HashSet<Rect>();
		public IReadOnlyList<VoxelCoordinate> CurrentSelection => m_selection.ToList().AsReadOnly();
		private HashSet<VoxelCoordinate> m_selection = new HashSet<VoxelCoordinate>();
		private bool m_selectionDirty = true;

		public void SetSelection(IEnumerable<VoxelCoordinate> coords)
		{
			m_selection.Clear();
			m_selectionDirty = true;
			if (coords == null)
			{
				return;
			}
			foreach (var c in coords)
			{
				m_selection.Add(c);
			}
		}

		public void AddSelection(VoxelCoordinate c)
		{
			m_selection.Add(c);
			m_selectionDirty = true;
		}

		public VoxelCursor SelectionCursor
		{
			get
			{
				if (__selectionCursor == null)
				{
					__selectionCursor = new VoxelCursor(Renderer);
				}
				return __selectionCursor;
			}
		}
		private VoxelCursor __selectionCursor;

		public override bool RequiresConstantRepaint() => true;

		protected override void DrawSpecificGUI()
		{
			if (Enabled)
			{
				Tools.current = Tool.Custom;
			}
			EditorGUILayout.LabelField("Painter", EditorStyles.whiteLargeLabel);
			EditorGUILayout.BeginVertical("Box");
			GUI.enabled = Enabled;
			var oldTool = CurrentTool;
			//CurrentLayer = (sbyte)EditorGUILayout.IntSlider("Current Layer", CurrentLayer, -5, 5);
			var newTool = (EPaintingTool)GUILayout.Toolbar((int)CurrentTool, m_tools.Select(s => s.Key.ToString()).ToArray());
			bool dirty = newTool != CurrentTool;
			CurrentTool = newTool;
			var t = m_tools[CurrentTool];
			if (dirty)
			{
				m_tools[oldTool].OnDisable();
				t.OnEnable();
			}
			EditorGUILayout.BeginVertical("Box");
			if (t.DrawInspectorGUI(this))
			{
				EditorUtility.SetDirty(Renderer.Mesh);
				Renderer.Invalidate(true, true);
			}
			EditorGUILayout.EndVertical();
			GUI.enabled = true;
			EditorGUILayout.EndVertical();

			SceneView.RepaintAll();
		}

		private void OnEnable()
		{
			SceneView.duringSceneGui += DuringSceneGUI;
		}

		private void DuringSceneGUI(SceneView sceneView)
		{
			void HideSubmesh(VoxelRendererSubmesh submesh)
			{
				if (!submesh)
				{
					return;
				}
				submesh.hideFlags |= HideFlags.HideInHierarchy;
			}
			switch (Event.current.type)
			{
				case EventType.MouseEnterWindow:
					Renderer.Submeshes.ForEach(HideSubmesh);
					break;
				case EventType.MouseLeaveWindow:
					Renderer.Submeshes.ForEach(HideSubmesh);
					break;
			}
		}

		void OnSceneGUI()
		{
			Deadzones.Clear();
			if (!Renderer || !Renderer.Mesh)
			{
				return;
			}

			if (Renderer.SnapMode != VoxelRenderer.eSnapMode.None && Tools.current != Tool.Rotate)
			{
				var scale = VoxelCoordinate.LayerToScale(Renderer.SnapLayer);
				if (Renderer.SnapMode == VoxelRenderer.eSnapMode.Local)
				{
					Renderer.transform.localPosition = Renderer.transform.localPosition.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
				else if (Renderer.SnapMode == VoxelRenderer.eSnapMode.Global)
				{
					Renderer.transform.position = Renderer.transform.position.RoundToIncrement(scale / (float)VoxelCoordinate.LayerRatio);
				}
			}
			if (!Enabled)
			{
				return;
			}

			var tran = Renderer.transform;
			Handles.color = new Color(1, 1, 1, .1f);
			Handles.DrawLine(tran.position - tran.up * 100, tran.position + tran.up * 100);
			Handles.DrawLine(tran.position - tran.right * 100, tran.position + tran.right * 100);
			Handles.DrawLine(tran.position - tran.forward * 100, tran.position + tran.forward * 100);

			DrawSceneGUIToolsIcons();

			if (!m_tools.ContainsKey(CurrentTool))
			{
				CurrentTool = default;
			}
			var t = m_tools[CurrentTool];
			if(t.DrawSceneGUI(this, Renderer, Event.current))
			{
				EditorUtility.SetDirty(Renderer.Mesh);
				Renderer.Invalidate(true, true);
			}

			if (m_selectionDirty)
			{
				SelectionCursor.SetData(Renderer.transform.localToWorldMatrix, m_selection.Select(x => Renderer.Mesh.Voxels[x]));
			}
			else
			{
				SelectionCursor.SetData(Renderer.transform.localToWorldMatrix);
			}
			m_selectionDirty = false;
			SelectionCursor.Update();
		}

		private void DrawSceneGUIToolsIcons()
		{
			Handles.BeginGUI();
			float buttonSize = 32;
			var toolEnums = Enum.GetValues(typeof(EPaintingTool)).Cast<EPaintingTool>()
				.Where(l => m_tools.ContainsKey(l))
				.ToList();
			var names = Enum.GetNames(typeof(EPaintingTool));
			var windowPosition = new Rect(5, 5, 2 * buttonSize + 15, Mathf.Ceil(toolEnums.Count / 2f) * buttonSize + 48);
			Deadzones.Add(windowPosition);
			GUI.Label(windowPosition, "Tool", "Window");
			windowPosition = new Rect(windowPosition.position.x + 5, windowPosition.position.y + 24, windowPosition.size.x - 10, windowPosition.size.y - 32);
			ToolsPanelHeight = windowPosition.yMax;
			var position = windowPosition.position;
			foreach (var toolEnum in toolEnums)
			{
				if(!m_tools.TryGetValue(toolEnum, out var tool))
				{
					continue;
				}
				var content = tool.Icon.WithTooltip(names[(int)toolEnum]);
				var rect = new Rect(position.x, position.y, 32, 32);
				if (toolEnum == CurrentTool)
				{
					GUI.color = Color.green;
					GUI.Label(rect, content, "Button");
					GUI.color = Color.white;
				}
				else
				{
					if (GUI.Button(rect, content))
					{
						CurrentTool = toolEnum;
					}
				}
				if (position.x > windowPosition.position.x)
				{
					position.x = windowPosition.x;
					position.y += buttonSize + 5;
				}
				else
				{
					position.x += buttonSize + 5;
				}
			}

			Handles.EndGUI();
		}

		public void OnDisable()
		{
			if (!m_tools.ContainsKey(CurrentTool))
			{
				CurrentTool = default;
			}
            m_tools[CurrentTool]?.OnDisable();
			SceneView.duringSceneGui -= DuringSceneGUI;
		}
	}
}
#endif