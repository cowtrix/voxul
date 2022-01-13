using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
    [CustomPropertyDrawer(typeof(SerializableGradient))]
    public class SerializableGradientEditor : PropertyDrawer
    {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var obj = (SerializableGradient)property.GetTargetObjectOfProperty();
			var grad = obj.ToGradient();
			grad = EditorGUI.GradientField(position, label, grad);
			obj = new SerializableGradient(grad);
			property.SetTargetObjectOfProperty(obj);

			EditorGUI.EndProperty();
		}
	}
}
