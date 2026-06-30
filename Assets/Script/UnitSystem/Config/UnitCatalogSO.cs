using System.Collections.Generic;
using UnityEngine;

namespace Script.UnitSystem.Config
{
    /// <summary>
    /// Catalogo unita': lookup centralizzato per id/archetipo.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/UnitSystem/UnitCatalog", fileName = "UnitCatalog")]
    public sealed class UnitCatalogSO : ScriptableObject
    {
        [SerializeField] private List<UnitConfigSO> _units = new();

        public IReadOnlyList<UnitConfigSO> Units => _units;

        public bool TryGetById(string unitId, out UnitConfigSO config)
        {
            config = null;
            if (string.IsNullOrWhiteSpace(unitId) || _units == null) return false;

            for (int i = 0; i < _units.Count; i++)
            {
                var candidate = _units[i];
                if (candidate != null && candidate.UnitId == unitId)
                {
                    config = candidate;
                    return true;
                }
            }

            return false;
        }

        public List<UnitConfigSO> GetByRole(UnitRole role)
        {
            var result = new List<UnitConfigSO>();
            if (_units == null) return result;

            for (int i = 0; i < _units.Count; i++)
            {
                var candidate = _units[i];
                if (candidate != null && candidate.Role == role)
                {
                    result.Add(candidate);
                }
            }

            return result;
        }
    }
}
