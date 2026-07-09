using System;

namespace Script.Core.Entities
{
	/// <summary>
	/// Exposes health state and mutation for combat/damage systems.
	/// </summary>
	public interface IDamageable
	{
		/// <summary>Maximum health the entity can hold.</summary>
		float MaxHealth { get; }

		/// <summary>Current health value, clamped between 0 and <see cref="MaxHealth"/>.</summary>
		float CurrentHealth { get; }

		/// <summary>Current health as a normalized ratio in the range [0,1].</summary>
		float HealthPercent { get; }

		/// <summary>
		/// Reduces <see cref="CurrentHealth"/> by <paramref name="amount"/> (clamped at 0).
		/// </summary>
		void TakeDamage(float amount);

		/// <summary>
		/// Increases <see cref="CurrentHealth"/> by <paramref name="amount"/> (clamped at <see cref="MaxHealth"/>).
		/// </summary>
		void Heal(float amount);

		/// <summary>
		/// Raised whenever <see cref="CurrentHealth"/> changes.
		/// The parameter is the new <see cref="HealthPercent"/> (0..1) for direct UI binding.
		/// </summary>
		event Action<float> OnHealthChanged;
	}
}
