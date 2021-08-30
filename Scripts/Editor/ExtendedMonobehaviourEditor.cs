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
			var methods = target.GetType().GetMethods()
				.Select(m => (m.GetCustomAttribute<ContextMenu>(), m))
				.Where(m => m.Item1 != null);
			foreach(var m in methods)
			{
				if (GUILayout.Button(m.Item1.menuItem))
				{
					foreach (var t in targets)
						m.Item2.Invoke(t, null);
				}
			}

			var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
				.Where(p => !p.DeclaringType.Assembly.FullName.Contains("UnityEngine.CoreModule"));
			EditorGUILayout.BeginVertical("Box");
			foreach (var prop in properties)
			{
				var firstVal = prop.GetValue(target);
				bool manyVal = targets.Length > 1;
				if(firstVal != null)
				{
					manyVal = targets.Any(t => !firstVal.Equals(prop.GetValue(t)));
				}

				if(firstVal is IList list)
				{
					EditorGUILayout.LabelField(prop.Name, $"Count: {list.Count}");
				}
				else if (firstVal is UnityEngine.Object unityObj && unityObj)
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
