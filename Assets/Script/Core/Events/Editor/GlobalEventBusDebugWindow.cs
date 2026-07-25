#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Script.Core.Events.Editor
{
	/// <summary>
	/// EditorWindow per monitorare in tempo reale tutti gli eventi pubblicati sul
	/// <see cref="GlobalEventBus"/>. Intercetta gli eventi tramite l'hook editor-only
	/// <c>OnEventPublished</c> — nessuna sottoscrizione manuale necessaria.
	///
	/// USAGE: Tools > Social Empire > Event Bus Debug
	/// 
	/// Funzionalita:
	/// - Log live con timestamp, nome evento, dominio, subscriber count
	/// - Filtri per dominio (Building, Grid, Resource, Economy, Input, Selection, Pathfinding, GameState)
	/// - Ricerca testuale libera
	/// - Pausa/Play per freeze il flusso
	/// - Conteggio sessione per tipo evento
	/// - Export CSV del log
	/// </summary>
	public class GlobalEventBusDebugWindow : EditorWindow
	{
		#region Constants

		private const int MaxEntries = 500;
		private const float RepaintInterval = 0.1f;

		private static readonly Color ColorBuilding = new(0.4f, 0.8f, 1f);    // Azzurro
		private static readonly Color ColorGrid = new(0.5f, 1f, 0.5f);        // Verde
		private static readonly Color ColorResource = new(1f, 0.85f, 0.3f);   // Giallo
		private static readonly Color ColorEconomy = new(1f, 0.6f, 0.3f);     // Arancione
		private static readonly Color ColorInput = new(0.8f, 0.6f, 1f);       // Viola
		private static readonly Color ColorSelection = new(1f, 0.5f, 0.7f);   // Rosa
		private static readonly Color ColorPathfinding = new(0.3f, 1f, 1f);   // Cyan
		private static readonly Color ColorGameState = new(0.7f, 0.7f, 0.7f); // Grigio
		private static readonly Color ColorDefault = Color.white;

		#endregion

		#region Types

		private readonly struct EventEntry
		{
			public readonly double Timestamp;
			public readonly string EventTypeName;
			public readonly string Domain;
			public readonly int SubscriberCount;

			public EventEntry(double timestamp, string eventTypeName, string domain, int subscriberCount)
			{
				Timestamp = timestamp;
				EventTypeName = eventTypeName;
				Domain = domain;
				SubscriberCount = subscriberCount;
			}
		}

		#endregion

		#region Menu

		[MenuItem("Tools/Social Empire/Event Bus Debug")]
		public static void ShowWindow()
		{
			var window = GetWindow<GlobalEventBusDebugWindow>("Event Bus Debug");
			window.minSize = new Vector2(420, 300);
			window.Show();
		}

		#endregion

		#region Fields

		private readonly List<EventEntry> _entries = new();
		private readonly Dictionary<string, int> _sessionCounts = new();
		private readonly HashSet<string> _activeFilters = new();

		private bool _isPaused;
		private string _searchQuery = "";
		private Vector2 _scrollPos;
		private float _lastRepaintTime;
		private int _totalEventsSession;

		// Domini noti (popolati all'avvio)
		private static readonly string[] KnownDomains =
		{
			"Building", "Grid", "Resource", "Economy",
			"Input", "Selection", "Pathfinding", "GameState"
		};

		#endregion

		#region Lifecycle

		private void OnEnable()
		{
			GlobalEventBus._debugLoggingEnabled = true;
			GlobalEventBus.OnEventPublished += HandleEventPublished;

			// Inizializza tutti i filtri come attivi
			foreach (var domain in KnownDomains)
			{
				_activeFilters.Add(domain);
			}
		}

		private void OnDisable()
		{
			GlobalEventBus._debugLoggingEnabled = false;
			GlobalEventBus.OnEventPublished -= HandleEventPublished;
		}

		private void Update()
		{
			// Repaint rate-limited per evitare overhead
			if (!_isPaused && Time.realtimeSinceStartup - _lastRepaintTime > RepaintInterval)
			{
				Repaint();
				_lastRepaintTime = Time.realtimeSinceStartup;
			}
		}

		private void OnGUI()
		{
			DrawToolbar();
			DrawFilters();
			DrawLog();
			DrawStats();
		}

		#endregion

		#region Event Handling

		private void HandleEventPublished(Type eventType, double timestamp)
		{
			if (_isPaused) return;

			var eventName = eventType.Name;
			var domain = DeriveDomain(eventName);
			var subscriberCount = GetSubscriberCountSafe(eventType);

			var entry = new EventEntry(timestamp, eventName, domain, subscriberCount);
			_entries.Add(entry);

			// Trim buffer circolare
			if (_entries.Count > MaxEntries)
			{
				_entries.RemoveAt(0);
			}

			// Aggiorna contatori sessione
			_totalEventsSession++;
			if (_sessionCounts.ContainsKey(eventName))
				_sessionCounts[eventName]++;
			else
				_sessionCounts[eventName] = 1;
		}

		private static int GetSubscriberCountSafe(Type eventType)
		{
			// Use reflection to call the generic GetSubscriberCount<T> — limited to known events.
			// Fallback: return registered event count from GetRegisteredEvents.
			try
			{
				var registered = GlobalEventBus.GetRegisteredEvents();
				return registered.TryGetValue(eventType.Name, out var count) ? count : 0;
			}
			catch
			{
				return 0;
			}
		}

		#endregion

		#region UI Drawing

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			// Play/Pause
			var prevColor = GUI.backgroundColor;
			GUI.backgroundColor = _isPaused ? Color.red : Color.green;
			if (GUILayout.Button(_isPaused ? "\u25b6 Play" : "\u23f8 Pause",
				EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				_isPaused = !_isPaused;
			}
			GUI.backgroundColor = prevColor;

			// Clear
			if (GUILayout.Button("\U0001f5d1 Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
			{
				_entries.Clear();
				_sessionCounts.Clear();
				_totalEventsSession = 0;
			}

			GUILayout.FlexibleSpace();

			// Stats inline
			EditorGUILayout.LabelField(
				$"\U0001f4ca Session: {_totalEventsSession} events | {_sessionCounts.Count} types | Buffer: {_entries.Count}/{MaxEntries}",
				EditorStyles.miniLabel);

			// Export
			if (GUILayout.Button("\U0001f4e5 Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				ExportToCSV();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawFilters()
		{
			EditorGUILayout.BeginVertical("box");

			// Filtri dominio — riga unica con toggle button
			EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();

			foreach (var domain in KnownDomains)
			{
				bool wasActive = _activeFilters.Contains(domain);
				var prevBg = GUI.backgroundColor;
				GUI.backgroundColor = wasActive ? GetDomainColor(domain) : Color.gray * 0.5f;

				bool isActive = GUILayout.Toggle(wasActive, domain, "Button", GUILayout.Width(75));
				GUI.backgroundColor = prevBg;

				if (isActive && !wasActive)
					_activeFilters.Add(domain);
				else if (!isActive && wasActive)
					_activeFilters.Remove(domain);
			}

			EditorGUILayout.EndHorizontal();

			// Ricerca testuale
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
			_searchQuery = EditorGUILayout.TextField(_searchQuery);
			if (GUILayout.Button("\u2716", GUILayout.Width(22)))
			{
				_searchQuery = "";
				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
			GUILayout.Space(2);
		}

		private void DrawLog()
		{
			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			var filteredEntries = GetFilteredEntries();

			foreach (var entry in filteredEntries)
			{
				DrawLogEntry(entry);
			}

			// Auto-scroll in basso se ci sono entry
			if (filteredEntries.Count > 0 && !_isPaused)
			{
				_scrollPos.y = float.MaxValue;
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawLogEntry(EventEntry entry)
		{
			var domainColor = GetDomainColor(entry.Domain);
			var timeLabel = $"t:{entry.Timestamp:F2}";
			var subsLabel = entry.SubscriberCount > 0 ? $" [{entry.SubscriberCount}]" : " [-]";

			EditorGUILayout.BeginHorizontal();

			// Timestamp
			var prevColor = GUI.contentColor;
			GUI.contentColor = Color.gray;
			EditorGUILayout.LabelField(timeLabel, GUILayout.Width(85));
			GUI.contentColor = prevColor;

			// Nome evento
			GUI.contentColor = domainColor;
			EditorGUILayout.LabelField($"{entry.EventTypeName}{subsLabel}",
				GUILayout.Width(200));
			GUI.contentColor = prevColor;

			// Dominio badge
			var prevBg = GUI.backgroundColor;
			GUI.backgroundColor = domainColor * 0.7f;
			EditorGUILayout.LabelField(entry.Domain, GUILayout.Width(85));
			GUI.backgroundColor = prevBg;

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();
		}

		private void DrawStats()
		{
			if (_sessionCounts.Count == 0) return;

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("\U0001f4ca Session Breakdown", EditorStyles.boldLabel);

			// Ordina per conteggio decrescente
			var sorted = _sessionCounts.OrderByDescending(kvp => kvp.Value).ToList();

			EditorGUILayout.BeginHorizontal();
			int col = 0;
			foreach (var kvp in sorted)
			{
				EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", GUILayout.Width(160));
				col++;
				if (col % 3 == 0)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
				}
			}
			if (col % 3 != 0) EditorGUILayout.EndHorizontal();
			else EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Filtering

		private List<EventEntry> GetFilteredEntries()
		{
			if (_entries.Count == 0) return _entries;

			var result = new List<EventEntry>(_entries.Count);
			bool hasSearch = !string.IsNullOrWhiteSpace(_searchQuery);
			string searchLower = hasSearch ? _searchQuery.ToLowerInvariant() : "";

			for (int i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];

				// Filtro dominio
				if (!_activeFilters.Contains(entry.Domain))
					continue;

				// Filtro ricerca testuale
				if (hasSearch && !entry.EventTypeName.ToLowerInvariant().Contains(searchLower))
					continue;

				result.Add(entry);
			}

			return result;
		}

		#endregion

		#region Domain Mapping

		private static string DeriveDomain(string eventTypeName)
		{
			if (eventTypeName.StartsWith("Building", StringComparison.Ordinal))
				return "Building";
			if (eventTypeName.StartsWith("Cell", StringComparison.Ordinal) ||
				eventTypeName.StartsWith("Grid", StringComparison.Ordinal) ||
				eventTypeName.StartsWith("Zone", StringComparison.Ordinal))
				return "Grid";
			if (eventTypeName.StartsWith("ResourceAmount", StringComparison.Ordinal) ||
				eventTypeName.StartsWith("ResourcesBatch", StringComparison.Ordinal))
				return "Economy";
			if (eventTypeName.StartsWith("Resource", StringComparison.Ordinal))
				return "Resource";
			if (eventTypeName.StartsWith("Input", StringComparison.Ordinal) ||
				eventTypeName.StartsWith("TileClicked", StringComparison.Ordinal))
				return "Input";
			if (eventTypeName.StartsWith("Entity", StringComparison.Ordinal) ||
				eventTypeName.StartsWith("Selection", StringComparison.Ordinal))
				return "Selection";
			if (eventTypeName.StartsWith("Path", StringComparison.Ordinal))
				return "Pathfinding";
			if (eventTypeName.StartsWith("Game", StringComparison.Ordinal))
				return "GameState";

			return "Other";
		}

		private static Color GetDomainColor(string domain)
		{
			return domain switch
			{
				"Building" => ColorBuilding,
				"Grid" => ColorGrid,
				"Resource" => ColorResource,
				"Economy" => ColorEconomy,
				"Input" => ColorInput,
				"Selection" => ColorSelection,
				"Pathfinding" => ColorPathfinding,
				"GameState" => ColorGameState,
				_ => ColorDefault
			};
		}

		#endregion

		#region Export

		private void ExportToCSV()
		{
			string path = EditorUtility.SaveFilePanel(
				"Export Event Log",
				Application.dataPath,
				$"event_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
				"csv");

			if (string.IsNullOrEmpty(path)) return;

			try
			{
				using var writer = new StreamWriter(path);
				writer.WriteLine("Timestamp,EventName,Domain,Subscribers");

				foreach (var entry in _entries)
				{
					writer.WriteLine($"{entry.Timestamp:F4},{entry.EventTypeName},{entry.Domain},{entry.SubscriberCount}");
				}

				Debug.Log($"[EventBusDebug] Exported {_entries.Count} entries to {path}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[EventBusDebug] Export failed: {ex.Message}");
			}
		}

		#endregion
	}
}
#endif
