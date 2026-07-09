using Script.Core.Entities;
using UnityEngine;

namespace Script.BuildingSystem
{
	/// <summary>
	/// ScriptableObject definition for a single stat row displayed in the entity inspector panel.
	/// Convert to a pure <see cref="StatDisplay"/> via <see cref="ToStatDisplay"/>.
	/// </summary>
	[CreateAssetMenu(fileName = "StatModifier", menuName = "Social Empire/Stats/Stat Modifier")]
	public class StatModifierSO : ScriptableObject
	{
		#region Fields

		[SerializeField] private string statId;
		[SerializeField] private float value;
		[SerializeField] private bool isPercentage;
		[SerializeField] private string displayLabel;

		#endregion

		#region Properties

		public string StatId => statId;
		public float Value => value;
		public bool IsPercentage => isPercentage;
		public string DisplayLabel => displayLabel;

		#endregion

		#region Public Methods

		/// <summary>
		/// Builds the pure data representation for the HUD, formatting the value
		/// as a percentage (e.g. "+15%") when <see cref="IsPercentage"/> is true,
		/// otherwise as a plain number (e.g. "100").
		/// </summary>
		public StatDisplay ToStatDisplay()
		{
			var formattedValue = isPercentage
				? $"{value:+0.##;-0.##;0}%"
				: value.ToString("0.##");

			return new StatDisplay(
				Label: string.IsNullOrEmpty(displayLabel) ? statId : displayLabel,
				Value: formattedValue);
		}

		#endregion
	}
}
