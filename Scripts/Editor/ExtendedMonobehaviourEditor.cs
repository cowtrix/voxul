#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace Voxul
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(ExtendedMonoBehaviour), true)]
	public class ExtendedMonobehaviourEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var methods = target.GetType().GetMethods()
				.Select(m => (m.GetCustomAttributes<ContextMenu>().FirstOrDefault(), m))
				.Where(m => m.Item1 != null);
			foreach (var m in methods)
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
				object firstVal;
				bool manyVal = false;
				try
				{
					firstVal = prop.GetValue(target);

					manyVal = targets.Length > 1;
					if (firstVal != null)
					{
						manyVal = targets.Any(t => !firstVal.Equals(prop.GetValue(t)));
					}
				}
				catch (Exception e)
				{
					while (e.InnerException != null)
						e = e.InnerException;
					firstVal = $"{e.GetType()}: {e.Message}";
				}

				if (firstVal is IList list)
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
#endif