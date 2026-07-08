using UnityEngine;

namespace Script.Common
{
	/// <summary>
	/// Controller per la colorazione (tint) della preview: valid/invalid/neutral.
	/// Pure C#, nessun MonoBehaviour.
	/// Estratto da <see cref="GenericPreviewSystem"/> per rispettare il Single Responsibility Principle.
	/// </summary>
	public sealed class PreviewTintController
	{
		#region Fields

		private Color _validColor = new Color(0.7f, 1f, 0.7f, 0.8f);
		private Color _invalidColor = new Color(1f, 0.7f, 0.7f, 0.8f);
		private Color _neutralColor = new Color(1f, 1f, 1f, 0.8f);
		private bool _lastValidState = true;

		#endregion

		#region Properties

		public bool LastValidState => _lastValidState;

		#endregion

		#region Public API

		/// <summary>
		/// Sovrascrive i colori moltiplicativi per gli stati valid/invalid/neutral.
		/// </summary>
		public void SetValidationColors(Color valid, Color invalid, Color? neutral = null)
		{
			_validColor = valid;
			_invalidColor = invalid;
			if (neutral.HasValue) _neutralColor = neutral.Value;
		}

		/// <summary>
		/// Applica il tint in base allo stato di validità.
		/// Ritorna false se lo stato non è cambiato (no-op).
		/// </summary>
		public bool ApplyValidationState(SpriteRenderer renderer, bool isValid)
		{
			if (_lastValidState == isValid) return false;
			_lastValidState = isValid;
			UpdateColor(renderer, isValid ? _validColor : _invalidColor);
			return true;
		}

		/// <summary>
		/// Forza l'applicazione del tint di validità ignorando il caching dello stato.
		/// </summary>
		public void ForceValidationState(SpriteRenderer renderer, bool isValid)
		{
			_lastValidState = isValid;
			UpdateColor(renderer, isValid ? _validColor : _invalidColor);
		}

		/// <summary>
		/// Ripristina la colorazione neutra.
		/// </summary>
		public void ApplyNeutralState(SpriteRenderer renderer)
		{
			UpdateColor(renderer, _neutralColor);
		}

		#endregion

		#region Private Methods

		private static void UpdateColor(SpriteRenderer renderer, Color targetColor)
		{
			if (renderer == null) return;
			renderer.color = targetColor;
		}

		#endregion
	}
}
