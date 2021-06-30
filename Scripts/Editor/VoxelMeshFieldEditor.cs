using System;
using System.Collections.Generic;
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

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			Rect GetRow(ref int row)
			{
				var rect = new Rect(new Vector2(position.min.x, position.min.y + RowHeight * row), new Vector2(position.width, RowHeight));
				row++;
				return rect;
			}

			int row = 0;
			var newMesh = EditorGUI.ObjectField(GetRow(ref row), property.objectReferenceValue, typeof(VoxelMesh), false) as VoxelMesh;

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
				var path = EditorUtility.SaveFilePanelInProject("Save Voxel Mesh",
					EditorExtensions.GetActualObjectForSerializedProperty<UnityEngine.Object>(fieldInfo, property)?.name, "asset", "");
				if (!string.IsNullOrEmpty(path))
				{
					AssetDatabase.CreateAsset(newMesh, path);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
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