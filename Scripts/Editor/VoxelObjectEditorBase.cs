#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	public abstract class VoxelObjectEditorBase<T> : ExtendedMonobehaviourEditor where T:VoxelRenderer
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

		protected GUIContent[] Tabs => new[] { new GUIContent(GetType().Name.CamelcaseToSpaces().Replace("Editor", "").Trim()), new GUIContent("Settings") };

		public T Renderer => target as T;

		public override void OnInspectorGUI()
		{
			Tab = GUILayout.Toolbar(Tab, Tabs);
			if (Tab == 1 || serializedObject.isEditingMultipleObjects)
			{
				Enabled = false;
				base.OnInspectorGUI();
				return;
			}

			Enabled = EditorGUILayout.Toggle("voxul_PaintingEnabled", Enabled);

			if (!Renderer.Mesh)
			{
				EditorGUILayout.HelpBox("Mesh (Voxel Mesh) asset cannot be null. Set it in the Settings tab.", MessageType.Warning);
				Enabled = false;
				return;
			}

			DrawSpecificGUI();
		}

		static protected T CreateNewInScene()
		{
			var go = new GameObject($"New {typeof(T).Name}");
			var r = go.AddComponent<T>();
			if (Selection.activeGameObject)
			{
				go.transform.SetParent(Selection.activeGameObject.transform);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;
				go.transform.localScale = Vector3.one;
			}
			Selection.activeGameObject = go;
			EditorGUIUtility.PingObject(go);
			return r;
		}

		protected abstract void DrawSpecificGUI();
	}
}
#endif