using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	public abstract class VoxelObjectEditorBase<T> : Editor where T:VoxelRenderer
	{
		protected int Tab
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

		protected GUIContent[] Tabs => new[] { new GUIContent(GetType().Name.CamelcaseToSpaces().Replace("Editor", "").Trim()), new GUIContent("Settings") };

		public T Renderer => target as T;

		public override void OnInspectorGUI()
		{
			Tab = GUILayout.Toolbar(Tab, Tabs);
			if (Tab == 1)
			{
				base.OnInspectorGUI();
				return;
			}

			if (!Renderer.Mesh)
			{
				EditorGUILayout.HelpBox("Mesh (Voxel Mesh) asset cannot be null. Set it in the Settings tab.", MessageType.Warning);
				return;
			}

			DrawSpecificGUI();
		}

		protected abstract void DrawSpecificGUI();
	}
}