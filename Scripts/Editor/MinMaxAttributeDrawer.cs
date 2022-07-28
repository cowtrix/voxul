#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Voxul.Utilities
{
	[CustomPropertyDrawer(typeof(MinMaxAttribute))]
	public class MinMaxDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// cast the attribute to make life easier
			MinMaxAttribute minMax = attribute as MinMaxAttribute;

			// This only works on a vector2! ignore on any other property type (we should probably draw an error message instead!)
			if (property.propertyType == SerializedPropertyType.Vector2 || property.propertyType == SerializedPropertyType.Vector2Int)
			{
				// if we are flagged to draw in a special mode, lets modify the drawing rectangle to draw only one line at a time
				if (minMax.ShowDebugValues || minMax.ShowEditRange)
				{
					position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				}

				var vec2 = property.propertyType == SerializedPropertyType.Vector2 ? property.vector2Value : (Vector2)property.vector2IntValue;

				// pull out a bunch of helpful min/max values....
				float minValue = vec2.x; // the currently set minimum and maximum value
				float maxValue = vec2.y;
				float minLimit = minMax.MinLimit; // the limit for both min and max, min cant go lower than minLimit and maax cant top maxLimit
				float maxLimit = minMax.MaxLimit;

				position = EditorGUI.PrefixLabel(position, label);
				minValue = EditorGUI.FloatField(new Rect(new Vector2(position.x - 30, position.y), new Vector2(70, position.height - 2)), vec2.x);
				EditorGUI.MinMaxSlider(new Rect(new Vector2(position.x + 25, position.y), new Vector2(position.width - 100, position.height)), ref minValue, ref maxValue, minLimit, maxLimit);
				maxValue = EditorGUI.FloatField(new Rect(new Vector2(position.xMax - 80, position.y), new Vector2(70, position.height - 2)), vec2.y);

				var vec = Vector2.zero; // save the results into the property!

				if(minValue > maxValue)
                {
					maxValue = minValue;
                }
				if(maxValue < minValue)
                {
					minValue = maxValue;
                }
				vec.x = minValue;
				vec.y = maxValue;

				if(property.propertyType == SerializedPropertyType.Vector2)
					property.vector2Value = vec;
				else
					property.vector2IntValue = new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
			}
		}

		// this method lets unity know how big to draw the property. We need to override this because it could end up meing more than one line big
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			MinMaxAttribute minMax = attribute as MinMaxAttribute;

			// by default just return the standard line height
			float size = EditorGUIUtility.singleLineHeight;

			// if we have a special mode, add two extra lines!
			if (minMax.ShowEditRange || minMax.ShowDebugValues)
			{
				size += EditorGUIUtility.singleLineHeight * 2;
			}

			return size;
		}
	}
}
#endif