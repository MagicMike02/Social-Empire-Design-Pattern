using System;
using Script.BuildingSystem;
using Script.Core.Events;
using Script.EconomySystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.Core.Entities
{
	/// <summary>
	/// Applies <see cref="StatModifierSO"/> modifiers to the <see cref="GameEconomyManager"/>
	/// when the owning building is placed or destroyed.
	/// Attach to any building prefab that should affect the economy (e.g. houses add Population).
	/// Subscribes to <see cref="BuildingPlacedEvent"/> and <see cref="BuildingDestroyedEvent"/>
	/// via <see cref="GlobalEventBus"/>; unsubscribes in <c>OnDestroy</c>.
	/// </summary>
	[RequireComponent(typeof(Building))]
	public sealed class StatApplier : MonoBehaviour
	{
		#region Fields

		[SerializeField] private StatModifierSO[] modifiers = Array.Empty<StatModifierSO>();

		private Building _building;
		private GameEconomyManager _economyManager;
		private bool _isInitialized;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			_building = GetComponent<Building>();
			_economyManager = FindObjectOfType<GameEconomyManager>();

			if (_economyManager == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[StatApplier] GameEconomyManager not found in scene on {name}. Stat modifiers will not be applied.");
#endif
				return;
			}

			_isInitialized = true;
		}

		private void OnEnable()
		{
			GlobalEventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
			GlobalEventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
		}

		private void OnDisable()
		{
			GlobalEventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
			GlobalEventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// When a building is placed, if it's this GameObject's building, apply all modifiers.
		/// </summary>
		private void OnBuildingPlaced(BuildingPlacedEvent evt)
		{
			if (!_isInitialized || _economyManager == null) return;
			if (!ReferenceEquals(evt.BuildingInstance, _building)) return;

			ApplyModifiers(add: true);
		}

		/// <summary>
		/// When a building is destroyed, if it's this GameObject's building, remove all modifiers.
		/// </summary>
		private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
		{
			if (!_isInitialized || _economyManager == null) return;
			if (!ReferenceEquals(evt.BuildingInstance, _building)) return;

			ApplyModifiers(add: false);
		}

		#endregion

		#region Modifier Application

		/// <summary>
		/// Iterates all <see cref="StatModifierSO"/> entries and applies or removes
		/// their effect on the <see cref="GameEconomyManager"/>.
		/// </summary>
		/// <param name="add">True to add the modifier, false to remove (invert).</param>
		private void ApplyModifiers(bool add)
		{
			foreach (var modifier in modifiers)
			{
				if (modifier == null) continue;
				if (modifier.TargetResource == ResourceType.None) continue;

				var amount = Mathf.RoundToInt(modifier.Value);
				if (amount == 0) continue;

				if (add)
				{
					_economyManager.AddResource(modifier.TargetResource, amount);
				}
				else
				{
					// Remove the modifier effect: spend the same amount.
					// If the resource is below the modifier amount, just set to 0.
					_economyManager.SpendResources(modifier.TargetResource, amount);
				}
			}
		}

		#endregion
	}
}