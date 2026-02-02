using UnityEngine;

namespace Script.InputSystem
{
    // Interfaccia comune per oggetti interattivi (Tile, Building, Unit)
    public interface IHoverable
    {
        void OnHoverEnter();
        void OnHoverExit();
        void OnClick();
        void OnRightClick(Vector3 worldPosition);
    }
}
