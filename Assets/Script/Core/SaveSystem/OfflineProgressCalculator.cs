using System;
using System.Collections.Generic;
using Script.ResourceSystem.Enums;

namespace Script.Core.SaveSystem
{
    /// <summary>
    /// Calcola i progressi offline (risorse accumulate) tra l'ultima uscita dal gioco
    /// e il ritorno del giocatore. Classe C# pura: nessuna dipendenza UnityEngine,
    /// riusabile lato server per validazione autoritativa (vedi AGENT_BRIEFING §3.3).
    /// </summary>
    public sealed class OfflineProgressCalculator
    {
        #region Constants

        /// <summary>
        /// Cap massimo di ore offline calcolabili. Oltre questa soglia il calcolo
        /// si ferma per evitare exploit (es. giocatore che resta offline settimane).
        /// </summary>
        private const double MaxOfflineHoursCap = 8.0;

        /// <summary>
        /// Production rate di default per ogni ResourceType, in unità/ora.
        /// Valori conservativi; sovrascrivibili via costruttore per tuning/bilanciamento.
        /// </summary>
        private static readonly Dictionary<ResourceType, double> DefaultProductionRates =
            new()
            {
                { ResourceType.Wood,  10.0 },
                { ResourceType.Stone,  8.0 },
                { ResourceType.Gold,   5.0 },
                { ResourceType.Meat,   6.0 },
                // Experience non è una risorsa di produzione passiva
            };

        #endregion

        #region Fields

        private readonly Dictionary<ResourceType, double> _productionRates;

        #endregion

        #region Construction

        /// <summary>
        /// Crea il calcolatore con i production rate di default.
        /// </summary>
        public OfflineProgressCalculator()
            : this(DefaultProductionRates)
        {
        }

        /// <summary>
        /// Crea il calcolatore con production rate personalizzati (es. da ScriptableObject di bilanciamento).
        /// </summary>
        /// <param name="productionRates">Rate di produzione per ResourceType in unità/ora.</param>
        public OfflineProgressCalculator(IReadOnlyDictionary<ResourceType, double> productionRates)
        {
            _productionRates = productionRates != null
                ? new Dictionary<ResourceType, double>(productionRates)
                : new Dictionary<ResourceType, double>(DefaultProductionRates);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Calcola i delta di risorse accumulati offline a partire dal GameSaveData.
        /// Non modifica lo stato: restituisce solo il risultato del calcolo.
        /// </summary>
        /// <param name="saveData">Stato salvato contenente lastExitAt.</param>
        /// <returns>Risultato del calcolo offline (delta per risorsa + elapsed).</returns>
        public OfflineProgressResult Calculate(GameSaveData saveData)
        {
            if (saveData == null)
            {
                return OfflineProgressResult.Empty("saveData null.");
            }

            if (string.IsNullOrEmpty(saveData.lastExitAt))
            {
                return OfflineProgressResult.Empty("lastExitAt assente.");
            }

            if (!DateTime.TryParse(saveData.lastExitAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastExit))
            {
                return OfflineProgressResult.Empty($"lastExitAt non valido: '{saveData.lastExitAt}'.");
            }

            DateTime nowUtc = DateTime.UtcNow;
            TimeSpan elapsed = nowUtc - lastExit;

            // Tempo negativo (orologio di sistema spostato indietro): nessun progresso
            if (elapsed <= TimeSpan.Zero)
            {
                return OfflineProgressResult.Empty("Elapsed <= 0 (orologio sistema modificato?).");
            }

            // Applica il cap massimo
            double elapsedHours = elapsed.TotalHours;
            bool wasCapped = false;
            if (elapsedHours > MaxOfflineHoursCap)
            {
                elapsedHours = MaxOfflineHoursCap;
                wasCapped = true;
            }

            // Calcola i delta per ogni risorsa con production rate > 0
            var deltas = new Dictionary<ResourceType, int>();
            foreach (var kvp in _productionRates)
            {
                if (kvp.Value <= 0) continue;
                int produced = (int)Math.Floor(kvp.Value * elapsedHours);
                if (produced > 0)
                {
                    deltas[kvp.Key] = produced;
                }
            }

            return new OfflineProgressResult(
                deltas: deltas,
                elapsed: elapsed,
                elapsedHoursCapped: elapsedHours,
                wasCapped: wasCapped,
                isValid: true,
                reason: null);
        }

        #endregion
    }

    /// <summary>
    /// Risultato del calcolo dei progressi offline. Record C# puro, nessuna dipendenza Unity.
    /// </summary>
    public sealed record OfflineProgressResult(
        IReadOnlyDictionary<ResourceType, int> Deltas,
        TimeSpan Elapsed,
        double ElapsedHoursCapped,
        bool WasCapped,
        bool IsValid,
        string Reason)
    {
        /// <summary>
        /// Factory per un risultato vuoto/non valido.
        /// </summary>
        public static OfflineProgressResult Empty(string reason) =>
            new(
                Deltas: new Dictionary<ResourceType, int>(),
                Elapsed: TimeSpan.Zero,
                ElapsedHoursCapped: 0.0,
                WasCapped: false,
                IsValid: false,
                Reason: reason);
    }
}
