using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Script.Core.Commands
{
	/// <summary>
	/// Gestisce stack Undo/Redo per TUTTI i comandi del gioco.
	/// Pattern: Memento (traccia history comandi).
	/// 
	/// UNIVERSALE: Gestisce BuildingCommands, ResourceCommands, UnitCommands (future).
	/// 
	/// DESIGN: Usa LinkedList per trim O(1) sul limite history (vs Stack O(N)).
	/// Lo State del Command è read-only all'esterno: CommandHistory chiama
	/// Confirm()/Undo(), non setta mai command.State direttamente.
	/// </summary>
	public class CommandHistory
	{
		// LinkedList: AddLast/RemoveFirst = O(1) per il trim del limite history.
		// Last = comando più recente, First = comando più vecchio.
		private readonly LinkedList<ICommand> _undoList = new();
		private readonly LinkedList<ICommand> _redoList = new();
		private const int MaxHistorySize = 50; // Limit memory (configurabile)

		/// <summary>
		/// Esegue comando e lo aggiunge allo stack Undo.
		/// Invalida stack Redo (nuova branch di azioni).
		/// </summary>
		/// <param name="command">Comando da eseguire (Building, Resource, Unit...)</param>
		/// <returns>True se eseguito con successo</returns>
		public bool ExecuteCommand(ICommand command)
		{
			if (command == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[CommandHistory] Tentativo di eseguire comando null!");
#endif
				return false;
			}

			bool success = command.Execute();

			if (success)
			{
				// Conferma tramite il metodo del Command (non setta State direttamente).
				command.Confirm();

				// Aggiungi a undo list (coda = più recente)
				_undoList.AddLast(command);

				// Invalida redo list (nuova branch)
				_redoList.Clear();

				// Limit history size O(1): rimuovi il più vecchio in testa.
				TrimUndoHistory();

#if UNITY_EDITOR
				Debug.Log($"[CommandHistory] ✓ Executed: {command.Description} (Undo stack: {_undoList.Count})");
#endif
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[CommandHistory] ✗ Failed to execute: {command.Description}");
#endif
			}

			return success;
		}

		/// <summary>
		/// Esegue comando in modalità asincrona con optimistic update + conferma.
		/// Per ora esegue l'optimistic update e conferma immediatamente.
		/// In futuro: attenderà la conferma da IBackendService.
		/// </summary>
		/// <param name="command">Comando da eseguire (Building, Resource, Unit...)</param>
		/// <returns>True se eseguito e confermato con successo</returns>
		public async Task<bool> ExecuteCommandAsync(ICommand command)
		{
			if (command == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[CommandHistory] Tentativo di eseguire comando null!");
#endif
				return false;
			}

			bool success = await command.ExecuteAsync();

			if (success)
			{
				// Conferma tramite il metodo del Command (non setta State direttamente).
				command.Confirm();

				// Aggiungi a undo list (coda = più recente)
				_undoList.AddLast(command);

				// Invalida redo list (nuova branch)
				_redoList.Clear();

				// Limit history size O(1): rimuovi il più vecchio in testa.
				TrimUndoHistory();

#if UNITY_EDITOR
				Debug.Log($"[CommandHistory] ✓ Executed (async): {command.Description} (Undo stack: {_undoList.Count})");
#endif
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[CommandHistory] ✗ Failed to execute (async): {command.Description}");
#endif
			}

			return success;
		}

		/// <summary>
		/// Annulla ultimo comando (Ctrl+Z).
		/// Sposta comando da Undo stack a Redo stack.
		/// </summary>
		/// <returns>True se annullato con successo</returns>
		public bool Undo()
		{
			if (_undoList.Count == 0)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[CommandHistory] Nothing to undo (stack vuoto)");
#endif
				return false;
			}

			var command = _undoList.Last.Value;
			_undoList.RemoveLast();
			bool success = command.Undo();

			if (success)
			{
				// Lo State.RolledBack viene impostato dal Command stesso dentro Undo().
				_redoList.AddLast(command);

#if UNITY_EDITOR
				Debug.Log($"[CommandHistory] ✓ Undone: {command.Description} (Redo stack: {_redoList.Count})");
#endif
			}
			else
			{
				// Undo failed, ripristina list (mantieni coerenza)
				_undoList.AddLast(command);
#if UNITY_EDITOR
				Debug.LogError($"[CommandHistory] ✗ Failed to undo: {command.Description}");
#endif
			}

			return success;
		}

		/// <summary>
		/// Riesegue ultimo comando annullato (Ctrl+Shift+Z o Ctrl+Y).
		/// Sposta comando da Redo stack a Undo stack.
		/// </summary>
		/// <returns>True se rieseguito con successo</returns>
		public bool Redo()
		{
			if (_redoList.Count == 0)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[CommandHistory] Nothing to redo (stack vuoto)");
#endif
				return false;
			}

			var command = _redoList.Last.Value;
			_redoList.RemoveLast();
			bool success = command.Execute();

			if (success)
			{
				// Conferma tramite il metodo del Command (non setta State direttamente).
				command.Confirm();
				_undoList.AddLast(command);

				// Limit history size O(1) anche sul redo→undo.
				TrimUndoHistory();

#if UNITY_EDITOR
				Debug.Log($"[CommandHistory] ✓ Redone: {command.Description} (Undo stack: {_undoList.Count})");
#endif
			}
			else
			{
				// Redo failed, ripristina list
				_redoList.AddLast(command);
#if UNITY_EDITOR
				Debug.LogError($"[CommandHistory] ✗ Failed to redo: {command.Description}");
#endif
			}

			return success;
		}

		/// <summary>
		/// Pulisce tutto l'history (es. cambio scena, new game, load game).
		/// </summary>
		public void Clear()
		{
			int undoCount = _undoList.Count;
			int redoCount = _redoList.Count;

			_undoList.Clear();
			_redoList.Clear();

#if UNITY_EDITOR
			Debug.Log($"[CommandHistory] History cleared ({undoCount} undo + {redoCount} redo commands)");
#endif
		}

		/// <summary>
		/// Ottiene statistiche per UI/debugging.
		/// </summary>
		/// <returns>(undoCount, redoCount)</returns>
		public (int undoCount, int redoCount) GetStats()
		{
			return (_undoList.Count, _redoList.Count);
		}

		/// <summary>
		/// Verifica se è possibile annullare.
		/// Utile per abilitare/disabilitare pulsante Undo in UI.
		/// </summary>
		public bool CanUndo() => _undoList.Count > 0;

		/// <summary>
		/// Verifica se è possibile rieseguire.
		/// Utile per abilitare/disabilitare pulsante Redo in UI.
		/// </summary>
		public bool CanRedo() => _redoList.Count > 0;

		/// <summary>
		/// Mantiene l'undo list entro MaxHistorySize rimuovendo i comandi più vecchi.
		/// O(1) per ogni elemento in eccesso (RemoveFirst su LinkedList).
		/// </summary>
		private void TrimUndoHistory()
		{
			while (_undoList.Count > MaxHistorySize)
			{
				_undoList.RemoveFirst();

#if UNITY_EDITOR
				Debug.LogWarning($"[CommandHistory] History limit reached ({MaxHistorySize}), oldest command discarded");
#endif
			}
		}
	}
}
