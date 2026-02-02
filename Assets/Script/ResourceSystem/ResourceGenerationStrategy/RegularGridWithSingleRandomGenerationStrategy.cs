using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem.ResourceGenerationStrategy
{
    public class RegularGridWithSingleRandomGenerationStrategy: IResourceGenerationStrategy
    {
        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            List<Vector2Int> pattern = new List<Vector2Int>();
        
            int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(groupSize));
            int gridHeight = Mathf.CeilToInt((float)groupSize / gridWidth);

            //Generate Quadratic Grid Pattern resources
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (pattern.Count < groupSize)
                    {
                        Vector2Int position = origin + new Vector2Int(x, y);
                        pattern.Add(position);
                    }
                }
            }
            
            //Per ogni ciclo di creazione (groupsize del ResourceManager) genera dai 5 ai 10 single per la mappa  
            int numberOfSingleResources = Random.Range(3, 7); 

            int randomRangeX = Mathf.Max(10, gridWidth); // Almeno 5, o la larghezza della griglia
            int randomRangeY = Mathf.Max(10, gridHeight);

            for (int i = 0; i < numberOfSingleResources; i++)
            {
                Vector2Int randomPos;
                int attempts = 0;
                const int maxAttempts = 50; // Limite per evitare loop infiniti

                do
                {
                    // Genera coordinate casuali all'interno del range definito intorno all'origine
                    int randX = Random.Range(-randomRangeX+1, randomRangeX-1);
                    int randY = Random.Range(-randomRangeY+1, randomRangeY-1);
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