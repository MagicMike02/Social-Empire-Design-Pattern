using Script.Core.Entities;

namespace Script.Core.Events
{
	/// <summary>
	/// Lightweight static bridge that lets <see cref="SelectableComponent"/> (a MonoBehaviour
	/// instantiated by the factory) reach the DI-managed <see cref="EntitySelectionManager"/>
	/// without a hard VContainer dependency in the entity layer.
	/// Set by <c>GameLifetimeScope</c> during composition; cleared on scope dispose.
	/// </summary>
	public static class EntitySelectionResolver
	{
		/// <summary>The currently registered selection manager, or null if not yet composed.</summary>
		public static EntitySelectionManager Instance { get; internal set; }
	}
}
