using System.Collections.Generic;

namespace Script.Core.Entities
{
    /// <summary>
    /// Exposes the list of actions the player can trigger on an entity from the HUD action bar.
    /// </summary>
    public interface IActionable
    {
        /// <summary>
        /// Returns the actions currently available for this entity.
        /// Implementations should return a stable snapshot; availability is reflected per-action
        /// via <see cref="GameAction.IsAvailable"/>/<see cref="GameAction.UnavailableReason"/>.
        /// </summary>
        IReadOnlyList<GameAction> GetAvailableActions();

        /// <summary>
        /// Pure data describing a single actionable command shown in the HUD.
        /// Intentionally holds no UnityEngine references (e.g. no Sprite): the UI resolves
        /// <see cref="IconPath"/> to a visual asset at render time.
        /// </summary>
        /// <param name="Id">Stable identifier used to route the action (e.g. "build.farm").</param>
        /// <param name="Label">Human-readable label for the button.</param>
        /// <param name="IconPath">Resource path or key the UI resolves to an icon asset.</param>
        /// <param name="IsAvailable">Whether the action can be triggered right now.</param>
        /// <param name="UnavailableReason">Reason text when <paramref name="IsAvailable"/> is false; null otherwise.</param>
        public record GameAction(
            string Id,
            string Label,
            string IconPath,
            bool IsAvailable,
            string UnavailableReason);
    }
}
