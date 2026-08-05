using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Script.Core.AutoSave
{
	/// <summary>
	/// Lifecycle bridge tra VContainer e AutoSaveManager.
	/// Implementa <see cref="IStartable"/> (avvio), <see cref="ITickable"/> (timer periodico
	/// sul main thread) e <see cref="IDisposable"/> (cleanup).
	/// Il timer accumula <c>Time.deltaTime</c> e chiama <see cref="IAutoSaveManager.SaveNow"/>
	/// al raggiungimento dell'intervallo configurato.
	/// </summary>
	public sealed class AutoSaveLifecycle : IStartable, ITickable, IDisposable
	{
		#region Dependencies

		private readonly IAutoSaveManager _autoSaveManager;
		private readonly AutoSaveConfigSO _config;

		#endregion

		#region State

		private float _accumulator;
		private float _intervalSeconds = 60f;

		#endregion

		#region Constructor

		[Inject]
		public AutoSaveLifecycle(IAutoSaveManager autoSaveManager, AutoSaveConfigSO config = null)
		{
			_autoSaveManager = autoSaveManager ?? throw new ArgumentNullException(nameof(autoSaveManager));
			_config = config;
		}

		#endregion

		#region IStartable

		/// <summary>
		/// Chiamato da VContainer dopo la costruzione del container.
		/// Avvia il servizio autosave e inizializza l'intervallo dal config.
		/// </summary>
		public void Start()
		{
			try
			{
				_intervalSeconds = _config != null ? Mathf.Max(1f, _config.AutoSaveIntervalSeconds) : 60f;
				_accumulator = 0f;
				_autoSaveManager.Start();

#if UNITY_EDITOR
				Debug.Log($"[AutoSaveLifecycle] Autosave avviato (intervallo {_intervalSeconds}s).");
#endif
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[AutoSaveLifecycle] Errore avvio: {ex.Message}");
#endif
			}
		}

		#endregion

		#region ITickable

		/// <summary>
		/// Chiamato da VContainer ogni frame sul main thread.
		/// Accumula il tempo trascorso e salva al raggiungimento dell'intervallo.
		/// </summary>
		public void Tick()
		{
			_accumulator += Time.deltaTime;
			if (_accumulator < _intervalSeconds) return;

			_accumulator = 0f;
			_autoSaveManager.SaveNow();
		}

		#endregion

		#region IDisposable

		/// <summary>
		/// Chiamato da VContainer quando il LifetimeScope viene distrutto.
		/// Ferma il servizio e rilascia le risorse.
		/// </summary>
		public void Dispose()
		{
			try
			{
				_autoSaveManager?.Dispose();

#if UNITY_EDITOR
				Debug.Log("[AutoSaveLifecycle] Autosave fermato e risorse rilasciate.");
#endif
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[AutoSaveLifecycle] Errore dispose: {ex.Message}");
#endif
			}
		}

		#endregion
	}
}