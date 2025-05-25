using UnityEngine;

namespace Script
{
    public class TerrainGenerator
    {
        private INoiseGenerator noiseGenerator;
        private int seed;

        public TerrainGenerator(int seed, INoiseGenerator noiseGenerator)
        {
            this.seed = seed;
            this.noiseGenerator = noiseGenerator;
            if (this.noiseGenerator == null)
            {
                this.noiseGenerator = new NoiseGenerator(); // Usa NoiseGenerator come default
            }
        }

        public Terrain[][] GenerateChunkTerrain(Vector2Int chunkPosition, int chunkWidth, int chunkHeight)
        {
            Terrain[][] terrainGrid = new Terrain[chunkWidth][];
            for (int x = 0; x < chunkWidth; x++)
            {
                terrainGrid[x] = new Terrain[chunkHeight];
                for (int y = 0; y < chunkHeight; y++)
                {
                    // Calcola la posizione globale della cella nel mondo di gioco.
                    int globalX = chunkPosition.x * chunkWidth + x;
                    int globalY = chunkPosition.y * chunkHeight + y;

                    // Genera un valore di rumore per determinare il tipo di terreno.
                    float noiseValue = noiseGenerator.GenerateNoise(globalX, globalY, seed);

                    // Determina il tipo di terreno in base al valore di rumore.
                    Terrain terrain;
                    if (noiseValue < 0.3f)
                    {
                        terrain = new Terrain(Color.blue, false);
                    }
                    else if (noiseValue < 0.7f)
                    {
                        terrain = new Terrain(Color.green,  true);
                    }
                    else
                    {
                        terrain = new Terrain(Color.gray,  false);
                    }
                    terrainGrid[x][y] = terrain;
                }
            }
            return terrainGrid;
        }
    }
}