using System.Collections.Generic;
using UnityEngine;

namespace Script.UnitSystem.Commands
{
    /// <summary>
    /// Command di movimento gruppo unita' verso destinazioni tile.
    /// </summary>
    public sealed class UnitMoveCommand : IUnitCommand
    {
        private readonly List<UnitController> _units;
        private readonly List<Vector2Int> _destinations;

        public UnitMoveCommand(List<UnitController> units, List<Vector2Int> destinations)
        {
            _units = units;
            _destinations = destinations;
        }

        public bool Execute()
        {
            if (_units == null || _destinations == null || _units.Count == 0 || _units.Count != _destinations.Count)
            {
                return false;
            }

            bool anySuccess = false;

            for (int i = 0; i < _units.Count; i++)
            {
                var unit = _units[i];
                if (unit == null) continue;

                bool ok = unit.MoveToCell(_destinations[i]);
                anySuccess |= ok;
            }

            return anySuccess;
        }
    }
}
