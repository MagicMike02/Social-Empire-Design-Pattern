using Script.EntitySystem.Resource;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class ResourceGenerator
    {
        private INoiseGenerator noiseGenerator;
        private int seed;

        public ResourceGenerator(int seed, INoiseGenerator noiseGenerator)
        {
            this.seed = seed;
            this.noiseGenerator = noiseGenerator;
            if (this.noiseGenerator == null)
            {
                this.noiseGenerator = new NoiseGenerator(); // Usa NoiseGenerator come default
            }
        }

        public void GenerateChunkResources(Chunk chunk)
        {
            Cell[][] cells = chunk.GetCells();
            for (int x = 0; x < cells.Length; x++)
            {
                for (int y = 0; y < cells[x].Length; y++)
                {
                    // Calcola la posizione globale della cella nel mondo.
                    Vector2Int globalPosition = new Vector2Int(chunk.GetPosition().x * cells.Length + x, chunk.GetPosition().y * cells[x].Length + y);
                    // Genera un valore di rumore per determinare la quantità di risorse.
                    float noiseValue = noiseGenerator.GenerateNoise(globalPosition.x, globalPosition.y, seed);

                    // Determina il tipo e la quantità di risorse in base al valore di rumore.
                    if (noiseValue > 0.8f)
                    {
                        // Genera una risorsa di pietra.
                        Resource stoneResource = (Resource) GameManager.Instance.entityFactory.CreateEntity("Stone", globalPosition); 
                        GameManager.Instance.GetCell(globalPosition).SetResource(ResourceType.Stone);
                        GameManager.Instance.CreateEntity(stoneResource);
                    }
                    else if (noiseValue > 0.6f)
                    {
                        // Genera una risorsa d'oro
                        Resource goldResource = (Resource) GameManager.Instance.entityFactory.CreateEntity("Gold", globalPosition);
                        GameManager.Instance.GetCell(globalPosition).SetResource(ResourceType.Gold);
                        GameManager.Instance.CreateEntity(goldResource);
                    }
                }
            }
        }
    }
}