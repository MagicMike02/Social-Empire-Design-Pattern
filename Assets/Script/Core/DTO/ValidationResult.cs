namespace Script.Core.DTO
{
	/// <summary>
	/// Risultato di una validazione: valido/invalido + motivo opzionale.
	/// Record C# puro — nessuna dipendenza UnityEngine.
	/// </summary>
	public record ValidationResult
	{
		public bool IsValid { get; init; }
		public string Reason { get; init; }

		public ValidationResult(bool isValid, string reason = null)
		{
			IsValid = isValid;
			Reason = reason;
		}

		public static ValidationResult Ok() => new(true);

		public static ValidationResult Fail(string reason) => new(false, reason);

		/// <summary>Composizione AND: la prima validazione fallita vince.</summary>
		public static ValidationResult operator &(ValidationResult a, ValidationResult b)
		{
			if (a is null || !a.IsValid) return a;
			if (b is null || !b.IsValid) return b;
			return Ok();
		}

		/// <summary>Composizione OR: la prima validazione valida vince.</summary>
		public static ValidationResult operator |(ValidationResult a, ValidationResult b)
		{
			if (a is { IsValid: true }) return a;
			if (b is { IsValid: true }) return b;
			return a is null ? b : a;
		}
	}
}
