using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// GlobalEventBusMonitor: Strumento di debugging per monitorare eventi del GlobalEventBus.
    /// SOLO PER EDITOR: Disabilita automaticamente in build di produzione.
    /// Attach a GameObject in scena per visualizzare statistiche eventi in Inspector.
    /// </summary>
    #if UNITY_EDITOR
    [AddComponentMenu("Social Empire/Core/Event Bus Monitor")]
    public class GlobalEventBusMonitor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _logAllEvents = false;
        [Tooltip("Se true, logga OGNI evento pubblicato (può generare spam)")]

        [SerializeField] private float _updateInterval = 1f;
        [Tooltip("Intervallo (secondi) per aggiornare statistiche in Inspector")]

        [Header("Statistics (Read-Only)")]
        [SerializeField, TextArea(3, 10)] private string _currentStats = "";

        private float _lastUpdateTime;

        private void OnEnable()
        {
            // Subscribe a TUTTI i tipi di eventi per logging (se abilitato)
            if (_logAllEvents)
            {
                // Resource Events
                GlobalEventBus.Subscribe<ResourceCollectedEvent>(OnResourceCollected);
                GlobalEventBus.Subscribe<ResourceGeneratedEvent>(OnResourceGenerated);
                GlobalEventBus.Subscribe<ResourceRegenerationStartedEvent>(OnResourceRegenerationStarted);
                GlobalEventBus.Subscribe<ResourceRegeneratedEvent>(OnResourceRegenerated);

                // Economy Events
                GlobalEventBus.Subscribe<ResourceAmountChangedEvent>(OnResourceAmountChanged);
                GlobalEventBus.Subscribe<ResourcesBatchChangedEvent>(OnResourcesBatchChanged);

                // Building Events
                GlobalEventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
                GlobalEventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
                GlobalEventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);

                // Grid Events
                GlobalEventBus.Subscribe<CellsOccupiedEvent>(OnCellsOccupied);
                GlobalEventBus.Subscribe<CellsFreedEvent>(OnCellsFreed);
                GlobalEventBus.Subscribe<ZoneUnlockedEvent>(OnZoneUnlocked);

                Debug.Log("[EventBusMonitor] ✓ Subscribed to ALL events for logging");
            }
        }

        private void OnDisable()
        {
            if (_logAllEvents)
            {
                // Unsubscribe (CRITICO per memory leak prevention!)
                GlobalEventBus.Unsubscribe<ResourceCollectedEvent>(OnResourceCollected);
                GlobalEventBus.Unsubscribe<ResourceGeneratedEvent>(OnResourceGenerated);
                GlobalEventBus.Unsubscribe<ResourceRegenerationStartedEvent>(OnResourceRegenerationStarted);
                GlobalEventBus.Unsubscribe<ResourceRegeneratedEvent>(OnResourceRegenerated);

                GlobalEventBus.Unsubscribe<ResourceAmountChangedEvent>(OnResourceAmountChanged);
                GlobalEventBus.Unsubscribe<ResourcesBatchChangedEvent>(OnResourcesBatchChanged);

                GlobalEventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
                GlobalEventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
                GlobalEventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);

                GlobalEventBus.Unsubscribe<CellsOccupiedEvent>(OnCellsOccupied);
                GlobalEventBus.Unsubscribe<CellsFreedEvent>(OnCellsFreed);
                GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(OnZoneUnlocked);

                Debug.Log("[EventBusMonitor] ✓ Unsubscribed from all events");
            }
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void Update()
        {
            // Update stats periodicamente
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateStats();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateStats()
        {
            _currentStats = "=== GlobalEventBus Statistics ===\n\n";
            _currentStats += GlobalEventBus.GetStats() + "\n\n";
            _currentStats += "=== Registered Events ===\n";

            var events = GlobalEventBus.GetRegisteredEvents();
            foreach (var kvp in events)
            {
                _currentStats += $"- {kvp.Key}: {kvp.Value} subscriber(s)\n";
            }
        }

        #region Event Logging Handlers

        private void OnResourceCollected(ResourceCollectedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourceCollected: {evt.Type} x{evt.Amount} at {evt.Position}");
        }

        private void OnResourceGenerated(ResourceGeneratedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourceGenerated: {evt.Type} at {evt.Position}");
        }

        private void OnResourceRegenerationStarted(ResourceRegenerationStartedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourceRegenerationStarted: {evt.Position} (time: {evt.RegenerationTime}s)");
        }

        private void OnResourceRegenerated(ResourceRegeneratedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourceRegenerated: {evt.Type} at {evt.Position}");
        }

        private void OnResourceAmountChanged(ResourceAmountChangedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourceAmountChanged: {evt.Type} = {evt.CurrentAmount} (delta: {evt.Delta:+#;-#;0})");
        }

        private void OnResourcesBatchChanged(ResourcesBatchChangedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ResourcesBatchChanged: {evt.NewBalances.Count} resources updated");
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] BuildingPlaced: {evt.BuildingName} at {evt.GridPosition}");
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] BuildingDestroyed at {evt.GridPosition} (refunded: {evt.WasRefunded})");
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] BuildingSelected: {evt.BuildingInstance?.name}");
        }

        private void OnCellsOccupied(CellsOccupiedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] CellsOccupied: {evt.OriginCell} ({evt.Width}x{evt.Height}) by {evt.Occupant?.name}");
        }

        private void OnCellsFreed(CellsFreedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] CellsFreed: {evt.OriginCell} ({evt.Width}x{evt.Height})");
        }

        private void OnZoneUnlocked(ZoneUnlockedEvent evt)
        {
            Debug.Log($"[EventBusMonitor] ZoneUnlocked: #{evt.ZoneIndex} at {evt.ZonePosition}");
        }

        #endregion

        #region Context Menu Utilities

        [ContextMenu("Print Current Stats")]
        private void PrintStats()
        {
            UpdateStats();
            Debug.Log(_currentStats);
        }

        [ContextMenu("Clear All Subscriptions (DANGEROUS)")]
        private void ClearAllSubscriptions()
        {
            if (Application.isPlaying)
            {
                GlobalEventBus.ClearAllSubscriptions();
                Debug.LogWarning("[EventBusMonitor] ⚠️ All subscriptions cleared!");
            }
            else
            {
                Debug.LogWarning("[EventBusMonitor] Can only clear subscriptions in Play mode");
            }
        }

        #endregion
    }
    #endif
}
