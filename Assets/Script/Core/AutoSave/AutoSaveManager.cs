using System;
using System.Threading;
using System.Threading.Tasks;
using Script.Core.Events;
using Script.Core.SaveSystem;
using UnityEngine;
using VContainer;

namespace Script.Core.AutoSave
{
	/// <summary>
	/// Salvataggio automatico reattivo agli eventi critici + trigger periodico.
	/// Classe C# pura (non MonoBehaviour): il timer periodico è gestito da
	/// <see cref="AutoSaveLifecycle"/> via <c>ITickable</c> (main thread, ogni frame);
	/// </summary>
	public sealed class AutoSaveManager : IAutoSaveManager
	{
		#region Dependencies

		private readonly SaveManager _saveManager;
		private readonly AutoSaveConfigSO _config;

		#endregion

		#region State

		private readonly object _lock = new();
		private bool _running;
		private DateTime? _lastSaveUtc;
		private int _saveCount;
		private DateTime _lastEventSaveUtc = DateTime.MinValue;

		#endregion

		#region Properties

		public bool IsRunning => _running;
		public DateTime? LastSaveUtc => _lastSaveUtc;
		public int SaveCount => _saveCount;

		#endregion

		#region Constructor

		/// <summary>
		/// Costruttore VContainer. <paramref name="config"/> può essere null:
		/// in tal caso si usano i valori di default (intervallo 60s, debounce 2s).
		/// </summary>
		[Inject]
		public AutoSaveManager(SaveManager saveManager, AutoSaveConfigSO config = null)
		{
			_saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
			_config = config;
		}

		#endregion

		#region IAutoSaveManager

		/// <summary>
		/// Avvia il servizio: sottoscrive gli eventi critici. Idempotente.
		/// Il timer periodico è delegato a <see cref="AutoSaveLifecycle.Tick"/>.
		/// </summary>
		public void Start()
		{
			lock (_lock)
			{
				if (_running) return;
				_running = true;
			}

			SubscribeCriticalEvents();

#if UNITY_EDITOR
			Debug.Log("[AutoSaveManager] Avviato (eventi critici + timer via ITickable).");
#endif
		}

		/// <summary>
		/// Ferma il servizio: disiscrive gli eventi critici. Idempotente.
		/// </summary>
		public void Stop()
		{
			lock (_lock)
			{
				if (!_running) return;
				_running = false;
			}

			UnsubscribeCriticalEvents();

#if UNITY_EDITOR
			Debug.Log("[AutoSaveManager] Fermato.");
#endif
		}

		/// <summary>
		/// Forza un salvataggio immediato (sincrono, main thread).
		/// Chiamato da <see cref="AutoSaveLifecycle.Tick"/> al scadere dell'intervallo
		/// e da <see cref="OnCriticalEvent"/> in risposta a eventi critici.
		/// </summary>
		public void SaveNow()
		{
			try
			{
				_saveManager.Save();
				lock (_lock)
				{
					_lastSaveUtc = DateTime.UtcNow;
					_saveCount++;
				}
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[AutoSaveManager] Errore durante il salvataggio: {ex.Message}");
#endif
			}
		}

		/// <summary>
		/// Forza un salvataggio immediato asincrono (per futuro server autoritativo).
		/// Implementazione attuale: fire-and-forget di <see cref="SaveNow"/> sul main thread.
		/// Futuro: <c>await _saveManager.SaveToServerAsync(token)</c> con retry/backoff.
		/// </summary>
		/// <param name="cancellationToken">Token per annullare l'operazione I/O.</param>
		public async Task SaveNowAsync(CancellationToken cancellationToken = default)
		{
			// TODO [Server]: sostituire con await _saveManager.SaveToServerAsync(cancellationToken);
			// Implementazione attuale: fire-and-forget sincrono (SaveManager.Save() usa API Unity).
			await Task.Run(() => SaveNow(), cancellationToken);
		}

		#endregion

		#region Critical Events Subscription

		private void SubscribeCriticalEvents()
		{
			if (_config != null && !_config.SaveOnCriticalEvents) return;

			GlobalEventBus.Subscribe<BuildingPlacedEvent>(OnCriticalEvent);
			GlobalEventBus.Subscribe<ZoneUnlockedEvent>(OnCriticalEvent);
			GlobalEventBus.Subscribe<ResourceAmountChangedEvent>(OnCriticalEvent);
		}

		private void UnsubscribeCriticalEvents()
		{
			GlobalEventBus.Unsubscribe<BuildingPlacedEvent>(OnCriticalEvent);
			GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(OnCriticalEvent);
			GlobalEventBus.Unsubscribe<ResourceAmountChangedEvent>(OnCriticalEvent);
		}

		/// <summary>
		/// Handler unificato per i tre eventi critici. Applica il debounce configurato
		/// per evitare burst di salvataggi ravvicinati, poi invoca <see cref="SaveNow"/>.
		/// Gli eventi di <see cref="GlobalEventBus"/> sono pubblicati sul main thread.
		/// </summary>
		private void OnCriticalEvent<T>(T evt) where T : struct
		{
			float debounce = _config != null ? _config.EventDebounceSeconds : 2f;

			DateTime nowUtc = DateTime.UtcNow;
			lock (_lock)
			{
				if ((nowUtc - _lastEventSaveUtc).TotalSeconds < debounce) return;
				_lastEventSaveUtc = nowUtc;
			}

			SaveNow();
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			Stop();
		}

		#endregion
	}
}
