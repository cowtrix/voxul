using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public static class EditorExtensions
	{
		public static GUIStyle Bold(this GUIStyle style, bool enabled)
		{
			if (!enabled)
			{
				return style;
			}
			style = new GUIStyle(style);
			style.fontStyle = FontStyle.Bold;
			return style;
		}

		public static GUIStyle WithColor(this GUIStyle style, Color color)
		{
			style = new GUIStyle(style);
			style.normal.textColor = color;
			style.focused.textColor = color;
			style.active.textColor = color;
			style.hover.textColor = color;
			return style;
		}

		public static GUIContent WithTooltip(this GUIContent content, string tooltip)
		{
			content.tooltip = tooltip;
			return content;
		}

		public static T GetActualObjectForSerializedProperty<T>(FieldInfo fieldInfo, SerializedProperty property) where T : class
		{
			var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
			if (obj == null) { return null; }

			T actualObject = null;
			if (obj.GetType().IsArray)
			{
				var index = Convert.ToInt32(new string(property.propertyPath.Where(c => char.IsDigit(c)).ToArray()));
				actualObject = ((T[])obj)[index];
			}
			else
			{
				actualObject = obj as T;
			}
			return actualObject;
		}
	}
}