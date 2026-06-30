using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.UnitSystem
{
    /// <summary>
    /// Gestisce la selezione unita' in modo disaccoppiato (single, additive, box).
    /// </summary>
    public sealed class UnitSelectionService : MonoBehaviour
    {
        private readonly HashSet<UnitController> _selected = new();

        public event Action<int> OnSelectionCountChanged;

        public int SelectedCount => _selected.Count;

        public IReadOnlyCollection<UnitController> GetSelectedUnits()
        {
            return _selected;
        }

        public void ClearSelection()
        {
            if (_selected.Count == 0) return;

            foreach (var unit in _selected)
            {
                unit.SetSelected(false);
            }

            _selected.Clear();
            OnSelectionCountChanged?.Invoke(_selected.Count);
        }

        public void SelectSingle(UnitController unit, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (unit == null)
            {
                OnSelectionCountChanged?.Invoke(_selected.Count);
                return;
            }

            if (additive && _selected.Contains(unit))
            {
                _selected.Remove(unit);
                unit.SetSelected(false);
            }
            else
            {
                _selected.Add(unit);
                unit.SetSelected(true);
            }

            OnSelectionCountChanged?.Invoke(_selected.Count);
        }

        public void SelectFromList(List<UnitController> units, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (units == null)
            {
                OnSelectionCountChanged?.Invoke(_selected.Count);
                return;
            }

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null) continue;

                _selected.Add(u);
                u.SetSelected(true);
            }

            OnSelectionCountChanged?.Invoke(_selected.Count);
        }
    }
}
