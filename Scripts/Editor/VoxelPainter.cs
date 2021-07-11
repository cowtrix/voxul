using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Edit;

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
	}

	internal enum eMirrorMode
	{
		None, X, Y, Z
	}

	[CustomEditor(typeof(VoxelRenderer), false)]
	internal class VoxelPainter : VoxelObjectEditorBase
	{
		Dictionary<EPaintingTool, VoxelPainterTool> m_tools = new Dictionary<EPaintingTool, VoxelPainterTool>
	{
		{ EPaintingTool.Select, new SelectTool() },
		{ EPaintingTool.Add, new AddTool() },
		{ EPaintingTool.Remove, new RemoveTool() },
		{ EPaintingTool.Subdivide, new SubdivideTool() },
		{ EPaintingTool.Paint, new PaintTool() },
		{ EPaintingTool.Clipboard, new ClipboardTool() },
	};

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
		public bool Enabled
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainter_Enabled", true);
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainter_Enabled", value);
			}
		}
		public sbyte CurrentLayer
		{
			get
			{
				return EditorPrefUtility.GetPref("VoxelPainter_CurrentLayer", default(sbyte));
			}
			set
			{
				EditorPrefUtility.SetPref("VoxelPainter_CurrentLayer", value);
			}
		}
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

		public HashSet<Rect> Deadzones = new HashSet<Rect>();
		public IEnumerable<VoxelCoordinate> CurrentSelection => m_selection;
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

		public static int Tab
		{
			get
			{
				return EditorPrefs.GetInt("VoxelPainter_Tab", 0);
			}
			set
			{
				EditorPrefs.SetInt("VoxelPainter_Tab", value);
			}
		}

		public static GUIContent[] Tabs => new[] { new GUIContent("Edit Mesh"), new GUIContent("Settings") };

		public override bool RequiresConstantRepaint() => true;

		public override void OnInspectorGUI()
		{
			if (target.GetType() != typeof(VoxelRenderer))
			{
				base.OnInspectorGUI();
				return;
			}

			Tab = GUILayout.Toolbar(Tab, Tabs);
			if (Tab == 1)
			{
				base.OnInspectorGUI();
				return;
			}

			Enabled = EditorGUILayout.Toggle("Painting Enabled", Enabled);
			if (!Renderer.Mesh)
			{
				EditorGUILayout.HelpBox("Mesh (Voxel Mesh) asset cannot be null", MessageType.Info);
				return;
			}
			EditorGUILayout.LabelField("Painter", EditorStyles.whiteLargeLabel);
			EditorGUILayout.BeginVertical("Box");
			GUI.enabled = Enabled;

			MirrorMode = (eMirrorMode)EditorGUILayout.EnumPopup("Mirror Mode", MirrorMode);
			var oldTool = CurrentTool;
			CurrentLayer = (sbyte)EditorGUILayout.IntSlider("Current Layer", CurrentLayer, -5, 5);
			var newTool = (EPaintingTool)GUILayout.Toolbar((int)CurrentTool, Enum.GetNames(typeof(EPaintingTool)));
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
			switch (Event.current.type)
			{
				case EventType.MouseEnterWindow:
					Renderer.Renderers.ForEach(x => x.hideFlags = x.hideFlags & ~HideFlags.HideInHierarchy);
					break;
				case EventType.MouseLeaveWindow:
					Renderer.Renderers.ForEach(x => x.hideFlags = x.hideFlags | HideFlags.HideInHierarchy);
					break;
			}
		}

		void OnSceneGUI()
		{
			Deadzones.Clear();
			if (!Enabled || !Renderer || !Renderer.Mesh)
			{
				return;
			}

			var tran = Renderer.transform;

			Tools.current = Tool.Custom;
			Handles.color = new Color(1, 1, 1, .1f);
			Handles.DrawLine(tran.position - tran.up * 100, tran.position + tran.up * 100);
			Handles.DrawLine(tran.position - tran.right * 100, tran.position + tran.right * 100);
			Handles.DrawLine(tran.position - tran.forward * 100, tran.position + tran.forward * 100);
			
			DrawToolsIcons();

			var t = m_tools[CurrentTool];
			t.DrawSceneGUI(this, Renderer, Event.current, CurrentLayer);

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

		private void DrawToolsIcons()
		{
			Handles.BeginGUI();
			float buttonSize = 32;
			var toolEnums = Enum.GetValues(typeof(EPaintingTool)).Cast<EPaintingTool>().ToList();
			var names = Enum.GetNames(typeof(EPaintingTool));
			var windowPosition = new Rect(5, 5, 2 * buttonSize + 15, (toolEnums.Count / 2) * buttonSize + 48);
			Deadzones.Add(windowPosition);
			GUI.Label(windowPosition, "Tool", "Window");
			windowPosition = new Rect(windowPosition.position.x + 5, windowPosition.position.y + 24, windowPosition.size.x - 10, windowPosition.size.y - 32);
			var position = windowPosition.position;
			foreach (var toolEnum in toolEnums)
			{
				var tool = m_tools[toolEnum];
				var content = tool.Icon.WithTooltip(names[(int)toolEnum]);
				var rect = new Rect(position.x, position.y, 32, 32);
				if(toolEnum == CurrentTool)
				{
					GUI.enabled = false;
				}
				if(GUI.Button(rect, content))
				{
					CurrentTool = toolEnum;
				}
				GUI.enabled = true;
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

			var settingsRect = new Rect(5, windowPosition.yMax + 15, 100, 100);
			Deadzones.Add(settingsRect);
			GUILayout.BeginArea(settingsRect, "Brush", "Window");
			GUILayout.BeginHorizontal();

			GUILayout.Label(EditorGUIUtility.IconContent("Mirror")
				.WithTooltip("Painting Mirroring Mode"));
			if (GUILayout.Button("X", EditorStyles.miniButtonLeft
				.WithColor(MirrorMode == eMirrorMode.X ? Color.green : Color.white)
				.Bold(MirrorMode == eMirrorMode.X)))
			{
				if(MirrorMode == eMirrorMode.X)
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
			GUILayout.EndArea();
			Handles.EndGUI();
		}

		public void OnDisable()
		{
			m_tools[CurrentTool]?.OnDisable();
			SceneView.duringSceneGui -= DuringSceneGUI;
		}
	}
}