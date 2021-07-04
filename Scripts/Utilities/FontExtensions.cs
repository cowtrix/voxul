using System;
using UnityEngine;
namespace Voxul.Utilities
{
	public static class FontExtensions
	{
		public static char GetCharacter(this CharacterInfo info) => Convert.ToChar(info.index);
	}
}