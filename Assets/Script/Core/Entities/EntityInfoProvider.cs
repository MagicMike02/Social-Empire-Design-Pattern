using System.Collections.Generic;
using Script.BuildingSystem;
using UnityEngine;

namespace Script.Core.Entities
{
	/// <summary>
	/// Aggregates static display data for an entity into an immutable <see cref="EntityInfo"/>
	/// snapshot consumed by the HUD/inspector panel. Holds no UnityEngine references beyond
	/// the serialized SO array: <see cref="StatModifierSO"/> assets are converted to pure
	/// <see cref="StatDisplay"/> records at request time.
	/// </summary>
	public sealed class EntityInfoProvider : MonoBehaviour
	{
		#region Fields

		[SerializeField] private string entityName;
		[SerializeField] private string description;
		[SerializeField] private string iconPath;
		[SerializeField] private List<StatModifierSO> stats = new();

		#endregion

		#region Public Methods

		/// <summary>
		/// Inizializza i campi display dal <see cref="BuildingConfigSO"/> (o altra fonte dati).
		/// Chiamato dal factory dopo l'istanziazione per garantire coerenza SO → prefab.
		/// </summary>
		public void Init(string name, string description, string iconPath)
		{
			entityName = name;
			this.description = description;
			this.iconPath = iconPath;
		}

		/// <summary>
		/// Builds the immutable <see cref="EntityInfo"/> snapshot for this entity.
		/// Called by <see cref="SelectableComponent.GetEntityInfo"/> on selection.
		/// </summary>
		public EntityInfo GetEntityInfo()
		{
			var statDisplays = new StatDisplay[stats.Count];
			for (int i = 0; i < stats.Count; i++)
			{
				statDisplays[i] = stats[i] != null
					? stats[i].ToStatDisplay()
					: new StatDisplay(string.Empty, string.Empty);
			}

			return new EntityInfo(
				EntityName: entityName,
				Description: description,
				IconPath: iconPath,
				Stats: statDisplays,
				HasActions: GetComponent<IActionable>() != null);
		}

		#endregion
	}
}
