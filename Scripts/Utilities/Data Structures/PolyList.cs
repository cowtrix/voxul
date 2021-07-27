using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxul.Utilities
{
	public interface IPolyList 
	{
	}

	[Serializable]
	public abstract class PolyList<T> : IPolyList
	{
		[Serializable]
		private struct SerializedPackage
		{
			public string AssemblyQualifiedName;
			public string JSONData;
		}

		[SerializeReference]
		public List<T> Data = new List<T>();

		/*public void OnAfterDeserialize()
		{
			Data?.Clear();
			if(m_data == null)
			{
				return;
			}
			foreach(var item in m_data)
			{
				var type = Type.GetType(item.AssemblyQualifiedName, true);
				var obj = (T)JsonUtility.FromJson(item.JSONData, type);
				Data.Add(obj);
			}
		}

		public void OnBeforeSerialize()
		{
			if(Data == null || Data.Count == 0)
			{
				m_data = null;
				return;
			}
			m_data = m_data ?? new List<SerializedPackage>();
			m_data.Clear();
			foreach (var item in Data)
			{
				m_data.Add(new SerializedPackage
				{
					AssemblyQualifiedName = item.GetType().AssemblyQualifiedName,
					JSONData = JsonUtility.ToJson(item),
				});
			}
		}*/
	}
}
