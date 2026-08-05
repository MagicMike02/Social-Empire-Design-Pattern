using System;
using Script.Core.Events;

namespace Script.Core.Entities
{
	/// <summary>
	/// Single source of truth for the currently selected entity.
	/// Resolved by VContainer as a singleton. Pure C# (no UnityEngine dependency):
	/// selection visuals are driven by <see cref="ISelectable"/> implementations and
	/// <see cref="EntitySelectedEvent"/>/<see cref="EntityDeselectedEvent"/> subscribers.
	/// </summary>
	public sealed class EntitySelectionManager : IDisposable
	{
		private ISelectable _current;

		/// <summary>The currently selected entity, or null when nothing is selected.</summary>
		public ISelectable CurrentSelection => _current;

		/// <summary>
		/// Selects <paramref name="entity"/>, deselecting the previous selection (if any) first.
		/// A null argument is treated as a deselect request.
		/// </summary>
		public void SelectEntity(ISelectable entity)
		{
			if (entity is null)
			{
				DeselectCurrent();
				return;
			}

			if (ReferenceEquals(_current, entity))
			{
				return;
			}

			DeselectCurrent();

			_current = entity;
			_current.Select();
			GlobalEventBus.Publish(new EntitySelectedEvent(_current, _current.GetEntityInfo()));
		}

		/// <summary>
		/// Deselects the current entity (if any) and publishes <see cref="EntityDeselectedEvent"/>.
		/// Safe to call when nothing is selected.
		/// </summary>
		public void DeselectCurrent()
		{
			if (_current is null)
			{
				return;
			}

			var previous = _current;
			_current = null;
			previous.Deselect();
			GlobalEventBus.Publish(new EntityDeselectedEvent());
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			DeselectCurrent();
		}
	}
}
