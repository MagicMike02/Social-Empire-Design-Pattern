using UnityEngine;
using VContainer;

namespace Script2.GridSystem
{
    /// <summary>
    /// Fornisce feedback UI per gli eventi delle zone (sblocco, acquisto fallito).
    /// REFACTORED: Usa Dependency Injection invece di FindFirstObjectByType.
    /// </summary>
    public class ZoneFeedbackUI : MonoBehaviour
    {
        private ZoneManager _zoneManager;

        [Inject]
        public void Construct(ZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        private void OnEnable()
        {
            if (_zoneManager != null)
            {
                _zoneManager.OnZoneUnlocked += HandleZoneUnlocked;
                _zoneManager.OnZonePurchaseFailed += HandleZonePurchaseFailed;
            }
        }

        private void OnDisable()
        {
            if (_zoneManager != null)
            {
                _zoneManager.OnZoneUnlocked -= HandleZoneUnlocked;
                _zoneManager.OnZonePurchaseFailed -= HandleZonePurchaseFailed;
            }
        }

        #region Event Handlers
        private void HandleZoneUnlocked(Vector2Int zoneCoord)
        {
            Debug.Log($"[UI] Zona sbloccata: {zoneCoord}");
        }

        private void HandleZonePurchaseFailed(Vector2Int zoneCoord)
        {
            Debug.LogWarning($"[UI] Acquisto zona fallito: {zoneCoord}");
        }
        #endregion
    }
}