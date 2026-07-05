using Script.Core.Events;
using UnityEngine;

namespace Script.UI
{
    /// <summary>
    /// Mediates zone-related events from the GlobalEventBus to UI feedback.
    /// Extracted from UIManager to isolate event handling.
    /// </summary>
    public class ZoneEventUIMediator : MonoBehaviour
    {
        private void OnEnable()
        {
            GlobalEventBus.Subscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
            GlobalEventBus.Subscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
            GlobalEventBus.Unsubscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
        }

        private void OnZoneUnlocked(ZoneUnlockedEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[ZoneEventUIMediator] Zone unlocked: {evt.ZonePosition}");
#endif
            // TODO: Trigger UI animation / popup for zone unlock.
        }

        private void OnZonePurchaseFailed(ZonePurchaseFailedEvent evt)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ZoneEventUIMediator] Zone purchase failed: {evt.ZoneCoord} - {evt.Reason}");
#endif
            // TODO: Show error UI feedback.
        }
    }
}
