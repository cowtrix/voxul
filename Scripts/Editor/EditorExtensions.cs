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

		public static object GetTargetObjectOfProperty(this SerializedProperty prop)
		{
			if (prop == null) return null;

			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}
			return obj;
		}

		public static object GetTargetObjectOfProperty(this SerializedProperty prop, object targetObj)
		{
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					targetObj = GetValue_Imp(targetObj, elementName, index);
				}
				else
				{
					targetObj = GetValue_Imp(targetObj, element);
				}
			}
			return targetObj;
		}

		private static object GetValue_Imp(object source, string name, int index)
		{
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			//while (index-- >= 0)
			//    enm.MoveNext();
			//return enm.Current;

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}
			return enm.Current;
		}

		private static object GetValue_Imp(object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}
			return null;
		}

		public static void SetTargetObjectOfProperty(this SerializedProperty prop, object value)
		{
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements.Take(elements.Length - 1))
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}

			if (UnityEngine.Object.ReferenceEquals(obj, null)) return;

			try
			{
				var element = elements.Last();

				if (element.Contains("["))
				{
					var tp = obj.GetType();
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					var field = tp.GetField(elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					var arr = field.GetValue(obj) as System.Collections.IList;
					arr[index] = value;
				}
				else
				{
					var tp = obj.GetType();
					var field = tp.GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (field != null)
					{
					    field.SetValue(obj, value);
					}
				}

			}
			catch
			{
				return;
			}
		}
	}
}