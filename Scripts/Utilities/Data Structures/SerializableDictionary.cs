using System.Collections.Generic;
using UnityEngine;

namespace Voxul.Utilities
{
	abstract public class SerializableDictionary<K, V>
		: Dictionary<K, V>, ISerializationCallbackReceiver
	{
		[SerializeField]
		private K[] keys;
		[SerializeField]
		private V[] values;

		public void OnAfterDeserialize()
		{
			if (keys == null || values == null)
			{
				voxulLogger.Error("Failed to deserialize SerializableDictionary<>");
				return;
			}
			var c = keys.Length;
			for (int i = 0; i < c; i++)
			{
				this[keys[i]] = values[i];
			}
			keys = null;
			values = null;
		}

		public void OnBeforeSerialize()
		{
			var c = this.Count;
			keys = new K[c];
			values = new V[c];
			int i = 0;
			using (var e = this.GetEnumerator())
				while (e.MoveNext())
				{
					var kvp = e.Current;
					keys[i] = kvp.Key;
					values[i] = kvp.Value;
					i++;
				}
		}
	}

}
