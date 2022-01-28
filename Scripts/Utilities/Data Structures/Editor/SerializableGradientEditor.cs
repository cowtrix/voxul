using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
			if (property == null)
			{
				return;
			}
			EditorGUI.BeginProperty(position, label, property);

			var isGradientKey = $"SerializableGradientEditor_{property.propertyPath}";
			var isGradient = EditorPrefs.HasKey(isGradientKey);
			var obj = (SerializableGradient)property.GetTargetObjectOfProperty();

			var toggleGradientModeRect = new Rect(position.x + position.width - 20, position.y, 20, 20);
			GUI.color = isGradient ? Color.green : Color.white;
			var content = EditorGUIUtility.IconContent("ColorPicker.CycleColor");
			content.tooltip = "Enable Gradient Mode";
			if(GUI.Button(toggleGradientModeRect, content))
			{
				isGradient = !isGradient;
				if (isGradient)
				{
					EditorPrefs.SetString(isGradientKey, "");
				}
				else
				{
					EditorPrefs.DeleteKey(isGradientKey);
				}
			}
			GUI.color = Color.white;
			position.width -= 22;

			if (isGradient)
			{
				var grad = obj.ToGradient();
				grad = EditorGUI.GradientField(position, label, grad, true);
				obj = new SerializableGradient(grad);
			}
			else
			{
				var c = obj.colorKeys.FirstOrDefault().Color;
				var a = obj.alphaKeys.FirstOrDefault().Alpha;
				c = c.WithAlpha(a);
				c = EditorGUI.ColorField(position, label, c, true, true, true);
				obj = new SerializableGradient(c);
			}
			
			property.SetTargetObjectOfProperty(obj);

			EditorGUI.EndProperty();
		}
	}
}
