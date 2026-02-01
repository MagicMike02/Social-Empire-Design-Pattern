﻿using UnityEngine;
using Script2.Core.Events;

namespace Script2.GridSystem
{
    /// <summary>
    /// Fornisce feedback UI per gli eventi delle zone (sblocco, acquisto fallito).
    /// REFACTORED: Usa GlobalEventBus invece di eventi nativi ZoneManager.
    /// </summary>
    public class ZoneFeedbackUI : MonoBehaviour
    {
        private void OnEnable()
        {
            // ✅ MIGRATO: GlobalEventBus
            GlobalEventBus.Subscribe<ZoneUnlockedEvent>(HandleZoneUnlocked);
            GlobalEventBus.Subscribe<ZonePurchaseFailedEvent>(HandleZonePurchaseFailed);
        }

        private void OnDisable()
        {
            // ✅ MIGRATO: GlobalEventBus
            GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(HandleZoneUnlocked);
            GlobalEventBus.Unsubscribe<ZonePurchaseFailedEvent>(HandleZonePurchaseFailed);
        }

        #region Event Handlers
        
        private void HandleZoneUnlocked(ZoneUnlockedEvent evt)
        {
            Debug.Log($"[UI] Zona sbloccata: {evt.ZonePosition}");
        }

        private void HandleZonePurchaseFailed(ZonePurchaseFailedEvent evt)
        {
            Debug.LogWarning($"[UI] Acquisto zona fallito: {evt.ZoneCoord} - {evt.Reason}");
        }
        
        #endregion
    }
}