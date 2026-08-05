using System;

namespace Script.Core.Entities
{
	/// <summary>
	/// Marks an entity as selectable by the selection system.
	/// Implementations should update <see cref="IsSelected"/> and notify
	/// visual components when <see cref="Select"/>/<see cref="Deselect"/> are called.
	/// </summary>
	public interface ISelectable
	{
		/// <summary>Called by <see cref="EntitySelectionManager"/> when the entity becomes the active selection.</summary>
		void Select();

		/// <summary>Called by <see cref="EntitySelectionManager"/> when the entity is no longer the active selection.</summary>
		void Deselect();

		/// <summary>Whether the entity is currently the active selection.</summary>
		bool IsSelected { get; }

		/// <summary>Static metadata describing this entity for the HUD/inspector panel.</summary>
		EntityInfo GetEntityInfo();
	}
}
