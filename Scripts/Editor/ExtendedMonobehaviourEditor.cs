using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Reflection;

namespace Voxul
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(ExtendedMonoBehaviour), true)]
	public class ExtendedMonobehaviourEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
				.Where(p => !p.DeclaringType.Assembly.FullName.Contains("UnityEngine.CoreModule"));
			EditorGUILayout.BeginVertical("Box");
			foreach (var prop in properties)
			{
				var firstVal = prop.GetValue(target);
				bool manyVal = targets.Any(t => !firstVal.Equals(prop.GetValue(t)));

				if (firstVal is UnityEngine.Object unityObj && unityObj)
				{
					EditorGUILayout.ObjectField(prop.Name, unityObj, prop.PropertyType, true);
				}
				else
				{
					EditorGUILayout.LabelField(prop.Name, manyVal ? "-" : firstVal == null ? "NULL" : firstVal.ToString());
				}
			}
			EditorGUILayout.EndVertical();
			base.OnInspectorGUI();
		}
	}
}
