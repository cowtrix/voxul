#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[CustomPropertyDrawer(typeof(TextureIndex))]
	public class TextureIndexInspect : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			float rowHeight = position.height - 17;

			var vm = VoxelManager.Instance;
			if(vm == null)
			{
				EditorGUI.LabelField(
					new Rect(new Vector2(position.min.x, position.max.y - rowHeight), new Vector2(position.width, rowHeight)),
					"ERROR: No VoxelManager found in project.");
				GUIUtility.ExitGUI();
				return;
			}
			var prop = property.FindPropertyRelative("Index");
			var tex = vm.Sprites.ElementAtOrDefault(prop.intValue);
			if (tex)
			{
				EditorGUI.DrawPreviewTexture(
				   new Rect(position.min, new Vector2(32, 32)),
				   tex);
			}
			var newTex = EditorGUI.ObjectField(
				new Rect(new Vector2(position.min.x, position.max.y - rowHeight), new Vector2(position.width, rowHeight)),
				tex, typeof(Texture2D), false) as Texture2D;

			if (tex != newTex && newTex)
			{
				if(vm.Sprites == null)
				{
					vm.Sprites = new List<Texture2D>();
				}
				if (!vm.Sprites.Contains(newTex))
				{
					vm.Sprites.Add(newTex);
					if (!EditorApplication.isCompiling)
					{
						UnityMainThreadDispatcher.Enqueue(() => vm.RegenerateSpritesheet());
					}
				}
				var index = vm.Sprites.IndexOf(newTex);
				prop.intValue = index;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 32;
		}
	}
}
#endif