using System.Collections.Generic;
using Script.BuildingSystem;
using UnityEngine;
using static Script.Core.Entities.IActionable;

namespace Script.Core.Entities
{
	/// <summary>
	/// Reusable action-bar component for any entity prefab.
	/// Implements <see cref="IActionable"/> by exposing the <see cref="GameActionSO"/>
	/// assets serialized on this component as pure <see cref="IActionable.GameAction"/> records.
	/// Availability is delegated to the caller (economy system) at invoke time.
	/// </summary>
	public sealed class ActionableComponent : MonoBehaviour, IActionable
	{
		#region Fields

		[SerializeField] private List<GameActionSO> actions = new();

		#endregion

		#region IActionable

		/// <inheritdoc/>
		public IReadOnlyList<GameAction> GetAvailableActions()
		{
			var result = new GameAction[actions.Count];
			for (int i = 0; i < actions.Count; i++)
			{
				var so = actions[i];
				result[i] = so != null
					? so.ToGameAction(isAvailable: true)
					: new GameAction(string.Empty, string.Empty, string.Empty, false, "Missing action definition");
			}

			return result;
		}

		#endregion
	}
}
