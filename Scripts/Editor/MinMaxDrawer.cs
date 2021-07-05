using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
	[CustomPropertyDrawer(typeof(sbyte2))]
	public class MinMaxDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var controlRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			var xProp = property.FindPropertyRelative("x");
			var yProp = property.FindPropertyRelative("y");
			float x = xProp.intValue;
			float y = yProp.intValue;
			float minLimit = sbyte.MinValue;
			float maxLimit = sbyte.MaxValue;

			var lh = EditorGUIUtility.singleLineHeight;
			var w = 50;
			EditorGUI.MinMaxSlider(new Rect(position.x, position.y, position.width, lh),
				label, ref x, ref y, minLimit, maxLimit);

			var midX = controlRect.x + (controlRect.width / 2f);
			x = EditorGUI.IntField(new Rect(controlRect.xMin, controlRect.y + lh, w, EditorGUIUtility.singleLineHeight), (int)x);
			y = EditorGUI.IntField(new Rect(controlRect.xMax - w, controlRect.y + lh, w, lh), (int)y);

			xProp.intValue = (int)x;
			yProp.intValue = (int)y;

			EditorGUI.EndProperty();
		}

		// this method lets unity know how big to draw the property. We need to override this because it could end up meing more than one line big
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2.1f;
		}

	}
}