using System.Collections.Generic;
using Script.UnitSystem.Config;
using UnityEngine;

namespace Script.UnitSystem.Production
{
    /// <summary>
    /// Gestisce quali unita' sono disponibili/sbloccate per il giocatore.
    /// </summary>
    public sealed class UnitUnlockService : MonoBehaviour
    {
        [SerializeField] private UnitSystemConfigSO _config;
        private readonly HashSet<string> _unlockedUnitIds = new();

        private void Awake()
        {
            if (_config == null) return;

            var defaults = _config.DefaultUnlockedUnitIds;
            if (defaults == null) return;

            for (int i = 0; i < defaults.Count; i++)
            {
                var id = defaults[i];
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _unlockedUnitIds.Add(id);
                }
            }

            if (!string.IsNullOrWhiteSpace(_config.DefaultUnitId))
            {
                _unlockedUnitIds.Add(_config.DefaultUnitId);
            }
        }

        public bool IsUnlocked(string unitId)
        {
            return !string.IsNullOrWhiteSpace(unitId) && _unlockedUnitIds.Contains(unitId);
        }

        public bool Unlock(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            return _unlockedUnitIds.Add(unitId);
        }

        public bool Lock(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            return _unlockedUnitIds.Remove(unitId);
        }

        public IReadOnlyCollection<string> GetUnlockedUnits()
        {
            return _unlockedUnitIds;
        }
    }
}
