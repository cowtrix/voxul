using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Voxul.Edit
{
	public static class EditorExtensions
	{
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