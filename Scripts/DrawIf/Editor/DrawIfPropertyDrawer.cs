using UnityEditor;
using UnityEngine;
using Voxul.Utilities;

namespace Voxul.Edit
{
    [CustomPropertyDrawer(typeof(DrawIfAttribute))]
    public class DrawIfPropertyDrawer : PropertyDrawer
    {
        // Reference to the attribute on the property.
        DrawIfAttribute drawIf;

        // Field that is being compared.
        SerializedProperty comparedField;

        // Height of the property.
        private float propertyHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Set the global variables.
            drawIf = attribute as DrawIfAttribute;
            comparedField = property.serializedObject.FindProperty(drawIf.comparedPropertyName);

            // Get the value of the compared field.
            object comparedFieldValue = comparedField.GetValue<object>();

            // References to the values as numeric types.
            NumericType numericComparedFieldValue = null;
            NumericType numericComparedValue = null;

            try
            {
                // Try to set the numeric types.
                numericComparedFieldValue = new NumericType(comparedFieldValue);
                numericComparedValue = new NumericType(drawIf.comparedValue);
            }
            catch (NumericTypeExpectedException)
            {
                // This place will only be reached if the type is not a numeric one. If the comparison type is not valid for the compared field type, log an error.
                if (drawIf.comparisonType != ComparisonType.Equals && drawIf.comparisonType != ComparisonType.NotEqual)
                {
                    Debug.LogError("The only comparsion types available to type '" + comparedFieldValue.GetType() + "' are Equals and NotEqual. (On object '" + property.serializedObject.targetObject.name + "')");
                    return;
                }
            }

            // Is the condition met? Should the field be drawn?
            bool conditionMet = false;

            // Compare the values to see if the condition is met.
            switch (drawIf.comparisonType)
            {
                case ComparisonType.Equals:
                    if (comparedFieldValue.Equals(drawIf.comparedValue))
                        conditionMet = true;
                    break;

                case ComparisonType.NotEqual:
                    if (!comparedFieldValue.Equals(drawIf.comparedValue))
                        conditionMet = true;
                    break;

                case ComparisonType.GreaterThan:
                    if (numericComparedFieldValue > numericComparedValue)
                        conditionMet = true;
                    break;

                case ComparisonType.SmallerThan:
                    if (numericComparedFieldValue < numericComparedValue)
                        conditionMet = true;
                    break;

                case ComparisonType.SmallerOrEqual:
                    if (numericComparedFieldValue <= numericComparedValue)
                        conditionMet = true;
                    break;

                case ComparisonType.GreaterOrEqual:
                    if (numericComparedFieldValue >= numericComparedValue)
                        conditionMet = true;
                    break;
            }

            // The height of the property should be defaulted to the default height.
            propertyHeight = base.GetPropertyHeight(property, label);

            // If the condition is met, simply draw the field. Else...
            if (conditionMet)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                //...check if the disabling type is read only. If it is, draw it disabled, else, set the height to zero.
                if (drawIf.disablingType == DisablingType.ReadOnly)
                {
                    GUI.enabled = false;
                    EditorGUI.PropertyField(position, property, label);
                    GUI.enabled = true;
                }
                else
                {
                    propertyHeight = 0f;
                }
            }
        }
    }
}