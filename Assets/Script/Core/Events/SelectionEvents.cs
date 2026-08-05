using Script.Core.Entities;

namespace Script.Core.Events
{
	/// <summary>
	/// Published via <see cref="GlobalEventBus"/> when an entity becomes the active selection.
	/// Subscribers: HUD/inspector panel, camera follow, audio cues.
	/// </summary>
	public readonly struct EntitySelectedEvent
	{
		/// <summary>The entity that was selected.</summary>
		public readonly ISelectable Entity;

		/// <summary>Pre-fetched display data for the HUD (avoids a second call into the entity).</summary>
		public readonly EntityInfo Info;

		public EntitySelectedEvent(ISelectable entity, EntityInfo info)
		{
			Entity = entity;
			Info = info;
		}
	}

	/// <summary>
	/// Published via <see cref="GlobalEventBus"/> when the active selection is cleared.
	/// Subscribers: HUD/inspector panel (hide), camera (release follow), audio cues.
	/// </summary>
	public readonly struct EntityDeselectedEvent
	{
	}
}
