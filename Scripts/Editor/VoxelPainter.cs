using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Edit;
using Voxul.Meshing;

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

	[CustomEditor(typeof(VoxelRenderer))]
	internal class VoxelPainter : Editor
	{
		[MenuItem("GameObject/3D Object/Voxel Object")]
		public static void CreateNew()
		{
			var go = new GameObject("New Voxel Object");
			var r = go.AddComponent<VoxelRenderer>();
		}

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
		public VoxelRenderer Renderer => target as VoxelRenderer;

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
			Renderer.Mesh = (VoxelMesh)EditorGUILayout.ObjectField("Voxel Mesh", Renderer.Mesh, typeof(VoxelMesh), true);
			if (Renderer.Mesh == null)
			{
				if (GUILayout.Button("Create In-Scene Mesh"))
				{
					Renderer.Mesh = CreateInstance<VoxelMesh>();
					Renderer.Mesh.name = Guid.NewGuid().ToString();
				}
			}
			else if (!AssetDatabase.Contains(Renderer.Mesh) && GUILayout.Button("Save In-Scene Mesh"))
			{
				var path = EditorUtility.SaveFilePanelInProject("Save Voxel Mesh", Renderer.name, "asset", "");
				if (!string.IsNullOrEmpty(path))
				{
					AssetDatabase.CreateAsset(Renderer.Mesh, path);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}
			else if (GUILayout.Button("Clone Mesh"))
			{
				Renderer.Mesh = Instantiate(Renderer.Mesh);
				Renderer.Mesh.name = Guid.NewGuid().ToString();
				Renderer.Mesh.Mesh = null;
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
				Renderer.Invalidate(true);
			}
			EditorGUILayout.EndVertical();
			GUI.enabled = true;
			EditorGUILayout.EndVertical();

			SceneView.RepaintAll();
		}

		void OnSceneGUI()
		{
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

		public void OnDisable()
		{
			m_tools[CurrentTool]?.OnDisable();
		}
	}
}