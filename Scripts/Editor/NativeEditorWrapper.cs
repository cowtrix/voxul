#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Voxul.Edit
{
	public static class NativeEditorUtility
	{
		private static Dictionary<Type, object> m_editorCache = new Dictionary<Type, object>();
		public static NativeEditorWrapper<T> GetWrapper<T>() where T : UnityEngine.Object
		{
			if (!m_editorCache.TryGetValue(typeof(T), out var editorObj)
				|| !(editorObj is NativeEditorWrapper<T> wrapper))
			{
				wrapper = new NativeEditorWrapper<T>();
				m_editorCache[typeof(T)] = wrapper;
			}
			return wrapper;
		}

	}

	public class NativeEditorWrapper<T> where T:UnityEngine.Object
	{
		private Editor m_cachedBrushEditor;
		private bool m_cachedEditorNeedsRefresh = true;
		public T LastValue;
		
		public void DrawGUI(UnityEngine.Object context, T obj)
		{
			if(LastValue != obj || m_cachedBrushEditor == null || !m_cachedBrushEditor || m_cachedEditorNeedsRefresh)
			{
				if (m_cachedBrushEditor)
				{
					UnityEngine.Object.DestroyImmediate(m_cachedBrushEditor);
				}
				m_cachedBrushEditor = Editor.CreateEditorWithContext(new[] { obj }, context);
				m_cachedEditorNeedsRefresh = false;
			}
			m_cachedBrushEditor?.DrawDefaultInspector();
			LastValue = obj;
		}
	}
}
#endif