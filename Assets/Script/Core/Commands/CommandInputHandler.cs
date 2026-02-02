﻿using UnityEngine;
using VContainer;

namespace Script.Core.Commands
{
    /// <summary>
    /// Gestisce input keyboard per Undo/Redo UNIVERSALE.
    /// Shortcuts standard:
    /// - Ctrl+Z = Undo
    /// - Ctrl+Shift+Z = Redo
    /// - Ctrl+Y = Redo (alternativo)
    /// 
    /// NOTE: Questo rimane keyboard-based perché Undo/Redo sono
    /// operazioni "meta" (non parte del gameplay mouse-driven).
    /// Standard in tutti gli editor (Unity, Photoshop, etc.)
    /// </summary>
    public class CommandInputHandler : MonoBehaviour
    {
        private CommandHistory _commandHistory;

        [Inject]
        public void Construct(CommandHistory commandHistory)
        {
            _commandHistory = commandHistory;
        }

        private void Update()
        {
            if (_commandHistory == null)
                return;

            // Ctrl+Shift+Z = Redo (priority su Ctrl+Z)
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
            {
                _commandHistory.Redo();
                return; // Early exit per evitare Undo subito dopo
            }
            
            // Ctrl+Y = Redo (alternativo, standard Windows)
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
            {
                _commandHistory.Redo();
                return;
            }

            // Ctrl+Z = Undo
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            {
                _commandHistory.Undo();
            }
        }

        private void OnGUI()
        {
            if (_commandHistory == null)
            {
                GUILayout.BeginArea(new Rect(10, 250, 350, 60));
                GUILayout.Box("⚠️ CommandHistory NOT INITIALIZED");
                GUILayout.Label("VContainer injection failed");
                GUILayout.EndArea();
                return;
            }

            // UI on-screen per feedback (debug/development)
            var stats = _commandHistory.GetStats();
            
            GUILayout.BeginArea(new Rect(10, 250, 350, 120));
            GUILayout.Box("=== COMMAND HISTORY ===");
            GUILayout.Label($"Undo Stack: {stats.undoCount} commands");
            GUILayout.Label($"Redo Stack: {stats.redoCount} commands");
            GUILayout.Label("");
            GUILayout.Label("Shortcuts:");
            GUILayout.Label("  Ctrl+Z = Undo | Ctrl+Shift+Z / Ctrl+Y = Redo");
            GUILayout.EndArea();
        }
    }
}
