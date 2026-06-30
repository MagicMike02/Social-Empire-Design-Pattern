using System.Collections.Generic;
using Script.UnitSystem.Config;
using UnityEngine;

namespace Script.UnitSystem
{
    /// <summary>
    /// Registry centrale delle unita' attive in scena.
    /// </summary>
    public sealed class UnitRegistryService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private UnitSystemConfigSO _config;

        [SerializeField] private int _maxUnits = 35;

        private readonly HashSet<UnitController> _units = new();

        public int MaxUnits => _maxUnits;
        public int ActiveUnits => _units.Count;

        private void Awake()
        {
            if (_config != null)
            {
                _maxUnits = Mathf.Max(1, _config.MaxUnits);
            }
        }

        public bool Register(UnitController unit)
        {
            if (unit != null)
            {
                if (_units.Count >= _maxUnits)
                {
                    Debug.LogWarning($"[UnitRegistryService] Max units reached ({_maxUnits}). Registration denied for {unit.name}.");
                    return false;
                }

                _units.Add(unit);
                return true;
            }

            return false;
        }

        public void Unregister(UnitController unit)
        {
            if (unit != null)
            {
                _units.Remove(unit);
            }
        }

        public IReadOnlyCollection<UnitController> GetAllUnits()
        {
            return _units;
        }
    }
}
