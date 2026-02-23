using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.ResourceSystem.ResourceGenerationStrategy
{
    public class RegularGridWithSingleRandomGenerationStrategy: IResourceGenerationStrategy
    {
        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            // O(1) Lookups for procedural validation
            HashSet<Vector2Int> patternSet = new HashSet<Vector2Int>();
        
            int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(groupSize));
            int gridHeight = Mathf.CeilToInt((float)groupSize / gridWidth);

            // Generate Quadratic Grid Pattern resources fully
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector2Int position = origin + new Vector2Int(x, y);
                    patternSet.Add(position);
                }
            }
            
            // Per ogni ciclo di creazione (groupsize del ResourceManager) genera vari single
            int numberOfSingleResources = Random.Range(3, 7); 

            int randomRangeX = Mathf.Max(10, gridWidth); 
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
                    
                } while (patternSet.Contains(randomPos) && attempts < maxAttempts);

                // Se abbiamo trovato una posizione unica entro i tentativi, la HashSet l'aggiunge saltando i doppioni
                patternSet.Add(randomPos);
            }
            
            return patternSet.ToList();
        }
    }
}