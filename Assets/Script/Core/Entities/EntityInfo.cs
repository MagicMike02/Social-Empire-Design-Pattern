using System.Collections.Generic;

namespace Script.Core.Entities
{
	/// <summary>
	/// A single labeled stat row for the inspector panel.
	/// </summary>
	/// <param name="Label">Localized label (e.g. "Health", "Attack").</param>
	/// <param name="Value">Pre-formatted value string (e.g. "100/100", "+15%").</param>
	public record StatDisplay(string Label, string Value);

	/// <summary>
	/// Immutable snapshot of an entity's display data for the HUD/inspector panel.
	/// Holds no UnityEngine references: <see cref="IconPath"/> is resolved by the UI layer.
	/// </summary>
	/// <param name="EntityName">Display name of the entity.</param>
	/// <param name="Description">Short descriptive text shown under the name.</param>
	/// <param name="IconPath">Resource path or key the UI resolves to an icon asset.</param>
	/// <param name="Stats">Read-only list of stat rows to render (e.g. "Health 100/100").</param>
	/// <param name="HasActions">Whether the entity exposes actions for the action bar.</param>
	public record EntityInfo(
		string EntityName,
		string Description,
		string IconPath,
		IReadOnlyList<StatDisplay> Stats,
		bool HasActions);
}
