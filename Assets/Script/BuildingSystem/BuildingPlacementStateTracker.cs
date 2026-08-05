using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Tieni traccia dello stato logico del placement senza dipendere da MonoBehaviour.
    /// Consente test EditMode del comportamento base del BuildingPlacer.
    /// </summary>
    public sealed class BuildingPlacementStateTracker
    {
        public BuildingConfigSO SelectedConfig { get; private set; }
        public Vector3Int CurrentCell { get; private set; }
        public Vector3Int LastPreviewCell { get; private set; } = Vector3Int.one * -1000;
        public bool LastValidState { get; private set; } = true;

        public void Select(BuildingConfigSO config)
        {
            SelectedConfig = config;
            CurrentCell = Vector3Int.zero;
            LastPreviewCell = Vector3Int.one * -1000;
            LastValidState = true;
        }

        public void Clear()
        {
            SelectedConfig = null;
            CurrentCell = Vector3Int.zero;
            LastPreviewCell = Vector3Int.one * -1000;
            LastValidState = true;
        }

        public void SetCurrentCell(Vector3Int cell)
        {
            CurrentCell = cell;
        }

        public bool CanPlaceAtCurrentPosition()
        {
            return SelectedConfig != null && LastValidState;
        }

        public bool ShouldRefreshPreview(Vector3Int cell, bool isValid)
        {
            return cell != LastPreviewCell || LastValidState != isValid;
        }

        public void MarkPreviewState(Vector3Int cell, bool isValid)
        {
            LastPreviewCell = cell;
            LastValidState = isValid;
        }
    }
}
