namespace Script
{
    public interface INoiseGenerator
    {
        float GenerateNoise(int x, int y, int seed);
    }
}