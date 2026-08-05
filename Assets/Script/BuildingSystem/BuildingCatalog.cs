using System.Collections.Generic;
using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Implementazione predefinita di <see cref="IBuildingCatalog"/>.
    /// Carica tutti i <see cref="BuildingConfigSO"/> dalla cartella Resources/Buildings
    /// al momento dell'instanziazione e li espone in un dizionario read‑only.
    /// </summary>
    public sealed class BuildingCatalog : IBuildingCatalog
    {
        private readonly Dictionary<string, BuildingConfigSO> _catalog;

        public BuildingCatalog()
        {
            var configs = Resources.LoadAll<BuildingConfigSO>("Buildings");
            _catalog = new Dictionary<string, BuildingConfigSO>(configs.Length);
            foreach (var c in configs)
            {
                if (c != null)
                {
                    _catalog[c.name] = c;
                }
            }
        }

        public IReadOnlyDictionary<string, BuildingConfigSO> GetCatalog()
        {
            return _catalog;
        }
    }
}
