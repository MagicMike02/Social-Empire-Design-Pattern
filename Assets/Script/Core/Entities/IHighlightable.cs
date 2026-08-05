namespace Script.Core.Entities
{
	/// <summary>
	/// Visual highlight states an entity can be put into by the input/selection systems.
	/// </summary>
	public enum HighlightType
	{
		None = 0,
		Hover = 1,
		Selected = 2,
		Preview = 3,
		Invalid = 4
	}

	/// <summary>
	/// Allows an entity to receive visual highlight feedback (outline, tint, etc.).
	/// Implementations are responsible for swapping the visual representation
	/// based on <see cref="HighlightType"/>.
	/// </summary>
	public interface IHighlightable
	{
		/// <summary>Applies the given highlight state. <see cref="HighlightType.None"/> is equivalent to clearing.</summary>
		void Highlight(HighlightType type);

		/// <summary>Removes any active highlight (equivalent to <see cref="Highlight(HighlightType.None)"/>).</summary>
		void ClearHighlight();
	}
}
