using UnityEditor;
using UnityEngine;
using Voxul.Utilities;
using UnityEditorInternal;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Voxul.Edit
{
	[CustomPropertyDrawer(typeof(IPolyList), true)]
	public class PolyListDrawer : PropertyDrawer
	{
		ReorderableList list;

		private void OnAddItemFromDropdown(object obj)
		{
			int last = list.serializedProperty.arraySize;
			list.serializedProperty.InsertArrayElementAtIndex(last);

			SerializedProperty lastProp = list.serializedProperty.GetArrayElementAtIndex(last);
			lastProp.managedReferenceValue = obj;

			list.serializedProperty.serializedObject.ApplyModifiedProperties();
		}

		private List<Type> GetNonAbstractTypesSubclassOf(Type parentType, bool sorted = true)
		{
			Assembly assembly = Assembly.GetAssembly(parentType);

			List<Type> types = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(parentType)).ToList();

			int CompareTypesNames(Type a, Type b)
			{
				return a.Name.CompareTo(b.Name);
			}

			if (sorted)
				types.Sort(CompareTypesNames);

			return types;
		}

		private void SetupList(SerializedProperty property)
		{
			property = property.FindPropertyRelative("Data");

			list = new ReorderableList(property.serializedObject, property);
			list.drawElementCallback += (rect, index, isActive, isFocused) =>
			{
				const int margin = 20;
				rect = new Rect(rect.x + margin, rect.y, rect.width - margin, rect.height);
				var element = list.serializedProperty.GetArrayElementAtIndex(index); //The element in the list
				var label = new GUIContent(element.managedReferenceFullTypename);
				element.isExpanded = EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element.isExpanded, label);
				if (!element.isExpanded)
				{
					return;
				}
				var type = Type.GetType(element.managedReferenceFullTypename);
				EditorGUI.PropertyField(rect, element, label, true);
			};
			list.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "");
			};
			list.elementHeightCallback += index =>
			{
				var element = list.serializedProperty.GetArrayElementAtIndex(index); //The element in the list
				return element != null && element.isExpanded ? EditorGUI.GetPropertyHeight(element) : EditorGUIUtility.singleLineHeight;
			};
			list.onAddCallback = (ReorderableList l) => {
				var index = l.serializedProperty.arraySize;
				l.serializedProperty.arraySize++;
				l.index = index;
				var element = l.serializedProperty.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("Type").enumValueIndex = 0;
				element.FindPropertyRelative("Count").intValue = 20;
				element.FindPropertyRelative("Prefab").objectReferenceValue =
						AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Mobs/Cube.prefab",
						typeof(GameObject)) as GameObject;
			};
			list.onAddDropdownCallback += (Rect rect, ReorderableList list) =>
			{
				var menu = new GenericMenu();
				var typeName = list.serializedProperty.type;
				var type = Type.GetType($"{nameof(Voxul)}.{nameof(Voxul.Meshing)}.{typeName}", true);
				var childTypes = GetNonAbstractTypesSubclassOf(type, true);
				foreach(var childType in childTypes)
				{
					menu.AddItem(new GUIContent(childType.Name),
					false, OnAddItemFromDropdown,
					Activator.CreateInstance(childType));
				}
				menu.ShowAsContext();
			};
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if(list == null)
			{
				SetupList(property);
			}
			list.DoList(position);
			EditorGUI.EndProperty();
		}

		// this method lets unity know how big to draw the property. We need to override this because it could end up meing more than one line big
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (list == null)
			{
				SetupList(property);
			}
			
			return list.GetHeight();
		}

	}
}