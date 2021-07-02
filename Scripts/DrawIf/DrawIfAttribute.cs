using UnityEngine;
using System;

namespace Voxul.Utilities
{
	/// <summary>
	/// The ways you can disable a field when using the DrawIf attribute and the condition isn't met.
	/// </summary>
	public enum DisablingType
	{
		ReadOnly = 2,
		DontDraw = 3
	}

	/// <summary>
	/// Types of comparisons.
	/// </summary>
	public enum ComparisonType
	{
		Equals = 1,
		NotEqual = 2,
		GreaterThan = 3,
		SmallerThan = 4,
		SmallerOrEqual = 5,
		GreaterOrEqual = 6
	}

	/// <summary>
	/// Draws the field/property ONLY if the copared property compared by the comparison type with the value of comparedValue returns true.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class DrawIfAttribute : PropertyAttribute
	{
		public string comparedPropertyName { get; private set; }
		public object comparedValue { get; private set; }
		public ComparisonType comparisonType { get; private set; }
		public DisablingType disablingType { get; private set; }

		/// <summary>
		/// Only draws the field only if a condition is met.
		/// </summary>
		/// <param name="comparedPropertyName">The name of the property that is being compared (case sensitive).</param>
		/// <param name="comparedValue">The value the property is being compared to.</param>
		/// <param name="comparisonType">The type of comparison the values will be compared by.</param>
		/// <param name="disablingType">The type of disabling that should happen if the condition is NOT met. Defaulted to DisablingType.DontDraw.</param>
		public DrawIfAttribute(string comparedPropertyName, object comparedValue, ComparisonType comparisonType, DisablingType disablingType = DisablingType.DontDraw)
		{
			this.comparedPropertyName = comparedPropertyName;
			this.comparedValue = comparedValue;
			this.comparisonType = comparisonType;
			this.disablingType = disablingType;
		}
	}
}