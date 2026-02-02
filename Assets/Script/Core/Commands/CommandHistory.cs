using System.Collections.Generic;
using UnityEngine;

namespace Script.Core.Commands
{
    /// <summary>
    /// Gestisce stack Undo/Redo per TUTTI i comandi del gioco.
    /// Pattern: Memento (traccia history comandi).
    /// 
    /// UNIVERSALE: Gestisce BuildingCommands, ResourceCommands, UnitCommands (future).
    /// </summary>
    public class CommandHistory
    {
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();
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
                Debug.LogWarning("[CommandHistory] Tentativo di eseguire comando null!");
                return false;
            }

            bool success = command.Execute();

            if (success)
            {
                // Aggiungi a undo stack
                _undoStack.Push(command);

                // Invalida redo stack (nuova branch)
                _redoStack.Clear();

                // Limit history size (gestione memoria)
                if (_undoStack.Count > MaxHistorySize)
                {
                    // Rimuovi comando più vecchio (FIFO on overflow)
                    var tempStack = new Stack<ICommand>();
                    
                    // Sposta tutti tranne il più vecchio
                    for (int i = 0; i < MaxHistorySize; i++)
                    {
                        tempStack.Push(_undoStack.Pop());
                    }
                    
                    // Scarta il più vecchio
                    _undoStack.Clear();
                    
                    // Ripristina stack
                    while (tempStack.Count > 0)
                    {
                        _undoStack.Push(tempStack.Pop());
                    }

                    Debug.LogWarning($"[CommandHistory] History limit reached ({MaxHistorySize}), oldest command discarded");
                }

                #if UNITY_EDITOR
                Debug.Log($"[CommandHistory] ✓ Executed: {command.Description} (Undo stack: {_undoStack.Count})");
                #endif
            }
            else
            {
                Debug.LogWarning($"[CommandHistory] ✗ Failed to execute: {command.Description}");
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
            if (_undoStack.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("[CommandHistory] Nothing to undo (stack vuoto)");
                #endif
                return false;
            }

            var command = _undoStack.Pop();
            bool success = command.Undo();

            if (success)
            {
                _redoStack.Push(command);
                
                #if UNITY_EDITOR
                Debug.Log($"[CommandHistory] ✓ Undone: {command.Description} (Redo stack: {_redoStack.Count})");
                #endif
            }
            else
            {
                // Undo failed, ripristina stack (mantieni coerenza)
                _undoStack.Push(command);
                Debug.LogError($"[CommandHistory] ✗ Failed to undo: {command.Description}");
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
            if (_redoStack.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("[CommandHistory] Nothing to redo (stack vuoto)");
                #endif
                return false;
            }

            var command = _redoStack.Pop();
            bool success = command.Execute();

            if (success)
            {
                _undoStack.Push(command);
                
                #if UNITY_EDITOR
                Debug.Log($"[CommandHistory] ✓ Redone: {command.Description} (Undo stack: {_undoStack.Count})");
                #endif
            }
            else
            {
                // Redo failed, ripristina stack
                _redoStack.Push(command);
                Debug.LogError($"[CommandHistory] ✗ Failed to redo: {command.Description}");
            }

            return success;
        }

        /// <summary>
        /// Pulisce tutto l'history (es. cambio scena, new game, load game).
        /// </summary>
        public void Clear()
        {
            int undoCount = _undoStack.Count;
            int redoCount = _redoStack.Count;
            
            _undoStack.Clear();
            _redoStack.Clear();
            
            Debug.Log($"[CommandHistory] History cleared ({undoCount} undo + {redoCount} redo commands)");
        }

        /// <summary>
        /// Ottiene statistiche per UI/debugging.
        /// </summary>
        /// <returns>(undoCount, redoCount)</returns>
        public (int undoCount, int redoCount) GetStats() 
        {
            return (_undoStack.Count, _redoStack.Count);
        }

        /// <summary>
        /// Verifica se è possibile annullare.
        /// Utile per abilitare/disabilitare pulsante Undo in UI.
        /// </summary>
        public bool CanUndo() => _undoStack.Count > 0;

        /// <summary>
        /// Verifica se è possibile rieseguire.
        /// Utile per abilitare/disabilitare pulsante Redo in UI.
        /// </summary>
        public bool CanRedo() => _redoStack.Count > 0;
    }
}
