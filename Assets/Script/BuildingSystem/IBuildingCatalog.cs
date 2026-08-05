using System.Collections.Generic;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Catalogo dei BuildingConfigSO caricati da Resources/Buildings.
    /// Fornisce un accesso in sola lettura al dizionario nome → config.
    /// </summary>
    public interface IBuildingCatalog
    {
        /// <summary>
        /// Restituisce il catalogo completo (read‑only).
        /// </summary>
        IReadOnlyDictionary<string, BuildingConfigSO> GetCatalog();
    }
}
