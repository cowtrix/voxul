using System;
using UnityEngine;

[CreateAssetMenu]
public class TextureArrayCollection : ScriptableObject
{
	[Serializable]
	public class TextureArrayMapping : SerializableDictionary<string, Texture2DArray> { }
	[HideInInspector]
	public TextureArrayMapping Data = new TextureArrayMapping();
}
