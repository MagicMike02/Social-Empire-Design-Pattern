using System;
using System.Threading;
using System.Threading.Tasks;

namespace Script.Core.AutoSave
{
	/// <summary>
	/// Contratto per il salvataggio automatico periodico e reattivo agli eventi critici.
	/// Implementazione di riferimento: <see cref="AutoSaveManager"/> (C# puro, non MonoBehaviour).
	/// Il timer periodico è gestito da <see cref="AutoSaveLifecycle"/> via <c>ITickable</c> (main thread).
	/// </summary>
	public interface IAutoSaveManager : IDisposable
	{
		/// <summary>True se il salvataggio automatico è attivo.</summary>
		bool IsRunning { get; }

		/// <summary>Timestamp UTC dell'ultimo salvataggio eseguito, o null se mai eseguito.</summary>
		DateTime? LastSaveUtc { get; }

		/// <summary>Numero di salvataggi eseguiti dall'avvio del manager.</summary>
		int SaveCount { get; }

		/// <summary>
		/// Avvia il servizio: sottoscrive gli eventi critici. Idempotente.
		/// Il timer periodico è delegato a <see cref="AutoSaveLifecycle.Tick"/>.
		/// </summary>
		void Start();

		/// <summary>
		/// Ferma il servizio: disiscrive gli eventi critici. Idempotente.
		/// </summary>
		void Stop();

		/// <summary>
		/// Forza un salvataggio immediato (sincrono, main thread).
		/// Usato dal timer <see cref="AutoSaveLifecycle.Tick"/> e dagli eventi critici.
		/// </summary>
		void SaveNow();

		/// <summary>
		/// Forza un salvataggio immediato asincrono (per futuro server autoritativo).
		/// Implementazione attuale: fire-and-forget di <see cref="SaveNow"/>.
		/// Futuro: <c>await _saveManager.SaveToServerAsync()</c> con retry/backoff.
		/// </summary>
		/// <param name="cancellationToken">Token per annullare l'operazione I/O.</param>
		Task SaveNowAsync(CancellationToken cancellationToken = default);
	}
}
