using UnityEngine;

namespace Script2.GridSystem
{
    public class ZoneFeedbackUI : MonoBehaviour
    {
        [SerializeField] private ZoneManager _zoneManager;

        private void Awake()
        {
            if (_zoneManager == null)
            {
                _zoneManager = FindFirstObjectByType<ZoneManager>();
            }
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