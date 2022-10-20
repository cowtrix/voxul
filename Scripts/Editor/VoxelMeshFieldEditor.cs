#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Voxul.Meshing;

namespace Voxul.Edit
{

	[CustomPropertyDrawer(typeof(VoxelMesh))]
	public class VoxelMeshFieldEditor : PropertyDrawer
	{
		const float RowHeight = 20;

		private static int GetRowCount(SerializedProperty property)
		{
			var mesh = property.objectReferenceValue as VoxelMesh;
			if (!mesh)
			{
				return 2;
			}
			if (!AssetDatabase.Contains(mesh))
			{
				return 3;
			}
			return 2;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return RowHeight * GetRowCount(property);
		}

		public override void OnGUI(Rect rootPos, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(rootPos, label, property);
			// Draw label
			var position = EditorGUI.PrefixLabel(rootPos, GUIUtility.GetControlID(FocusType.Passive), label);

			Rect GetRow(ref int row)
			{
				var rect = new Rect(new Vector2(position.min.x, position.min.y + RowHeight * row), new Vector2(position.width, RowHeight));
				row++;
				return rect;
			}

			int row = 0;
			var newMesh = EditorGUI.ObjectField(GetRow(ref row), property.objectReferenceValue, typeof(VoxelMesh), false) as VoxelMesh;
			if(newMesh != property.objectReferenceValue)
			{
				property.objectReferenceValue = newMesh;
			}

			var helpContent = EditorGUIUtility.IconContent("_Help");
			helpContent.text = "Mesh Info";
			GUI.enabled = property.objectReferenceValue != null;
			if (GUI.Button(new Rect(new Vector2(rootPos.xMin, rootPos.yMin + RowHeight), new Vector2(100, RowHeight)), helpContent))
			{
				EditorWindow.GetWindow<VoxelMeshInfoWindow>()
					.SetData(property.objectReferenceValue as VoxelMesh);
			}
			GUI.enabled = true;

			if (newMesh == null)
			{
				if (GUI.Button(GetRow(ref row), "Create In-Scene Mesh"))
				{
					newMesh = ScriptableObject.CreateInstance<VoxelMesh>();
					newMesh.name = Guid.NewGuid().ToString();
					property.objectReferenceValue = newMesh;
				}
			}
			else if (!AssetDatabase.Contains(newMesh) &&
				GUI.Button(GetRow(ref row), "Save In-Scene Mesh"))
			{
				var savedPath = EditorPrefs.GetString($"{nameof(VoxelMeshFieldEditor)}_Path", "");
				var path = EditorUtility.SaveFilePanelInProject("Save Voxel Mesh",
					EditorExtensions.GetActualObjectForSerializedProperty<UnityEngine.Object>(fieldInfo, property)?.name, "asset", "",
					savedPath);
				if (!string.IsNullOrEmpty(path))
				{
					var folder = Path.GetDirectoryName(path);
					EditorPrefs.SetString($"{nameof(VoxelMeshFieldEditor)}_Path", folder);
				}
				if (!string.IsNullOrEmpty(path))
				{
					AssetDatabase.CreateAsset(newMesh, path);
					AssetDatabase.SaveAssets();
				}
			}
			else if (GUI.Button(GetRow(ref row), "Clone Mesh"))
			{
				property.objectReferenceValue = UnityEngine.Object.Instantiate(newMesh);
				property.objectReferenceValue.name = Guid.NewGuid().ToString();
				(property.objectReferenceValue as VoxelMesh).UnityMeshInstances = null;
			}

			EditorGUI.EndProperty();
		}
	}
}
#endif