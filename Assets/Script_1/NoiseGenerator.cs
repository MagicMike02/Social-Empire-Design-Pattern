using UnityEngine;

namespace Script
{
    public class NoiseGenerator : INoiseGenerator
    {
        // Implementazione dell'algoritmo di generazione del rumore (es. Perlin noise).
        public float GenerateNoise(int x, int y, int seed)
        {
            // Implementazione Placeholder: usa Mathf.PerlinNoise o una libreria di terze parti.
            // Assicurati di usare il seed per ottenere risultati consistenti.
            float noiseValue = Mathf.PerlinNoise((x + seed) * 0.1f, (y + seed) * 0.1f);
            return noiseValue;
        }
    }
}