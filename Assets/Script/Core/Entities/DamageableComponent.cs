using System;
using UnityEngine;

namespace Script.Core.Entities
{
	/// <summary>
	/// Reusable health/damage component for any entity prefab.
	/// Implements <see cref="IDamageable"/> with clamped health and an
	/// <see cref="OnHealthChanged"/> event the UI can bind to directly.
	/// </summary>
	public sealed class DamageableComponent : MonoBehaviour, IDamageable
	{
		#region Fields

		[SerializeField] private float maxHealth = 100f;

		private float _currentHealth;
		private bool _isInitialized;

		#endregion

		#region Properties

		/// <inheritdoc/>
		public float MaxHealth => maxHealth;

		/// <inheritdoc/>
		public float CurrentHealth => _currentHealth;

		/// <inheritdoc/>
		public float HealthPercent => MaxHealth > 0f ? _currentHealth / MaxHealth : 0f;

		#endregion

		#region Events

		/// <inheritdoc/>
		public event Action<float> OnHealthChanged;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			_currentHealth = maxHealth;
			_isInitialized = true;
		}

		#endregion

		#region IDamageable

		/// <inheritdoc/>
		public void TakeDamage(float amount)
		{
			if (!_isInitialized || amount <= 0f) return;

			_currentHealth = Mathf.Max(0f, _currentHealth - amount);
			OnHealthChanged?.Invoke(HealthPercent);

			if (_currentHealth <= 0f)
			{
				Destroy(gameObject);
			}
		}

		/// <inheritdoc/>
		public void Heal(float amount)
		{
			if (!_isInitialized || amount <= 0f) return;

			_currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
			OnHealthChanged?.Invoke(HealthPercent);
		}

		#endregion
	}
}
