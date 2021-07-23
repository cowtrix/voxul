using UnityEditor;

namespace Voxul.Utilities
{
    public static class SerializedPropertyExtentions
    {
	    public static T GetValue<T>(this SerializedProperty property)
        {
            return ReflectionUtil.GetNestedObject<T>(property?.serializedObject?.targetObject, property?.propertyPath);
        }
    }
}