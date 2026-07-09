using System.Collections.Generic;
using Script.Core.Entities;
using UnityEngine;

namespace Script.BuildingSystem
{
	/// <summary>
	/// ScriptableObject definition for a single action an entity can expose to the HUD action bar.
	/// Convert to a pure <see cref="IActionable.GameAction"/> via <see cref="ToGameAction"/>.
	/// </summary>
	[CreateAssetMenu(fileName = "GameAction", menuName = "Social Empire/Actions/Game Action")]
	public class GameActionSO : ScriptableObject
	{
		#region Fields

		[SerializeField] private string actionId;
		[SerializeField] private string label;
		[SerializeField] private string iconPath;
		[SerializeField] private float durationSeconds = 1f;
		[SerializeField] private List<BuildingConfigSO.ResourceCost> costs = new();

		#endregion

		#region Properties

		public string ActionId => actionId;
		public string Label => label;
		public string IconPath => iconPath;
		public float DurationSeconds => durationSeconds;
		public IReadOnlyList<BuildingConfigSO.ResourceCost> Costs => costs;

		#endregion

		#region Public Methods

		/// <summary>
		/// Builds the pure data representation for the HUD.
		/// Availability and reason are supplied by the caller (e.g. economy system).
		/// </summary>
		public IActionable.GameAction ToGameAction(bool isAvailable, string unavailableReason = null)
		{
			return new IActionable.GameAction(
				Id: actionId,
				Label: label,
				IconPath: iconPath,
				IsAvailable: isAvailable,
				UnavailableReason: isAvailable ? null : unavailableReason);
		}

		#endregion
	}
}
