using UnityEngine;

namespace Script.Core.Events
{
	/// <summary>
	/// Pubblicato quando il caricamento del salvataggio è completato (tutti gli edifici ripristinati).
	/// Publisher: SaveManager (fine Load)
	/// Subscribers: PathfindingManager (clear cache una sola volta), altri sistemi post-load
	/// </summary>
	public readonly struct SaveLoadCompletedEvent
	{
		// Marker event - no payload needed
	}
}