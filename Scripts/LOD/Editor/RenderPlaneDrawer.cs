using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Voxul.LevelOfDetail
{
	[CustomPropertyDrawer(typeof(RenderPlaneVoxelLOD.RenderPlane))]
	public class RenderPlaneDrawer : PropertyDrawer
	{
		public void GetContext(SerializedProperty property, out RenderPlaneVoxelLOD lod, out RenderPlaneVoxelLOD.RenderPlane plane)
		{
			lod = property.serializedObject.targetObject as RenderPlaneVoxelLOD;
			plane = lod.RenderPlanes[int.Parse(Regex.Match(property.propertyPath, @"\[(\d+)\]$").Groups[1].Value)];
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			void DoRow(string fieldName, ref float yPointer, float height = 1)
			{
				var positionProp = property.FindPropertyRelative(fieldName);
				var positionPropRect = new Rect(position.x, yPointer, position.width, EditorGUI.GetPropertyHeight(positionProp) * height);
				EditorGUI.PropertyField(positionPropRect, positionProp, true);
				yPointer += positionPropRect.height + 3;
			}

			var dirProp = property.FindPropertyRelative(nameof(RenderPlaneVoxelLOD.RenderPlane.Direction));
			label.text += $" - {dirProp.enumNames[dirProp.enumValueIndex]}";
			EditorGUI.BeginProperty(position, label, property);

			var yPointer = position.y;
			var foldoutRect = new Rect(position.x, yPointer, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.displayName);
			if (!property.isExpanded)
			{
				EditorGUI.EndProperty();
				return;
			}
			yPointer += foldoutRect.height + 3;

			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Layer), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Min), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Max), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Offset), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Direction), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.CastDepth), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.FlipX), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.FlipY), ref yPointer);
			DoRow(nameof(RenderPlaneVoxelLOD.RenderPlane.Albedo), ref yPointer);

			var buttonRect = new Rect(position.x, yPointer, position.width / 2f, EditorGUIUtility.singleLineHeight);

			if (GUI.Button(buttonRect, "Snap to object"))
			{
				GetContext(property, out var lod, out var plane);
				lod.SnapPlane(plane);
			}

			buttonRect = new Rect(position.x + position.width / 2f, yPointer, position.width / 2f, EditorGUIUtility.singleLineHeight);
			if (GUI.Button(buttonRect, "Rebake"))
			{
				GetContext(property, out var lod, out var plane);
				lod.RebakePlane(plane);
			}
			
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			var positionProp = property.FindPropertyRelative(nameof(RenderPlaneVoxelLOD.RenderPlane.Min));
			var height = EditorGUIUtility.singleLineHeight * 13 + EditorGUI.GetPropertyHeight(positionProp);
			return height;
		}
	}
}