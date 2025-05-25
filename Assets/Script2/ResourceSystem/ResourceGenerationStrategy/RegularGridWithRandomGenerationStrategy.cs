using System.Collections.Generic;
using UnityEngine;

namespace Script2.GridSystem.ResourceSystem.ResourceGenerationStrategy
{
    public class RegularGridWithRandomGenerationStrategy : IResourceGenerationStrategy
    {
        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            List<Vector2Int> pattern = new List<Vector2Int>();

            int numberOfSingleResources = Random.Range(5, 20);

            // Ensure groupSize is at least numberOfSingleResources
            int gridResourceCount = Mathf.Max(0, groupSize - numberOfSingleResources);

            // 1. Genera le posizioni a griglia (come prima)
            int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(gridResourceCount));
            int gridHeight = Mathf.CeilToInt((float)gridResourceCount / gridWidth);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (pattern.Count < gridResourceCount)
                    {
                        Vector2Int position = origin + new Vector2Int(x, y);
                        pattern.Add(position);
                    }
                }
            }

            // 2. Aggiungi le posizioni singole randomiche
            // Definiamo un'area intorno all'origine o alla griglia per la generazione randomica
            // Qui usiamo un'area basata sulla dimensione della griglia per semplicità.
            // Puoi aggiustare il 'randomRange' a seconda delle dimensioni della tua mappa e del tuo desiderio.
            int randomRangeX = Mathf.Max(5, gridWidth); // Almeno 5, o la larghezza della griglia
            int randomRangeY = Mathf.Max(5, gridHeight); // Almeno 5, o l'altezza della griglia

            for (int i = 0; i < numberOfSingleResources; i++)
            {
                Vector2Int randomPos;
                int attempts = 0;
                const int maxAttempts = 50; // Limite per evitare loop infiniti

                do
                {
                    // Genera coordinate casuali all'interno del range definito intorno all'origine
                    int randX = Random.Range(-randomRangeX, randomRangeX + 1);
                    int randY = Random.Range(-randomRangeY, randomRangeY + 1);
                    randomPos = origin + new Vector2Int(randX, randY);
                    attempts++;

                    // Continua a generare finché non trovi una posizione che non sia già nella lista
                    // e non superi il numero massimo di tentativi.
                } while (pattern.Contains(randomPos) && attempts < maxAttempts);

                // Se abbiamo trovato una posizione unica entro i tentativi, aggiungila
                if (!pattern.Contains(randomPos))
                {
                    pattern.Add(randomPos);
                }
                // Se non riusciamo a trovare una posizione unica dopo maxAttempts, semplicemente
                // aggiungeremo meno risorse singole di quelle richieste.
            }

            return pattern;
        }
    }
}