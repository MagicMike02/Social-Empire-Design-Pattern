using UnityEngine;

namespace Script.UI
{
    /// <summary>
    /// Interface for UI panels that can be shown, hidden, and toggled.
    /// Implemented by concrete panel controllers (e.g., SettingsPanelController).
    /// </summary>
    public interface IPanelToggle
    {
        void Show();
        void Hide();
        void Toggle();
    }
}
