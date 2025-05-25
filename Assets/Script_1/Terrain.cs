using UnityEngine;

namespace Script
{
    public class Terrain
    {
        public Color color;
       public bool isPassable;

        public Terrain(Color color, bool isPassable)
        {
            this.color = color;
            this.isPassable = isPassable;
        }

        public Color GetColor() { return color; }
        public void SetColor(Color color) { this.color = color; }
       
        public bool IsPassable() { return isPassable; }
        public void SetPassable(bool passable) { this.isPassable = passable; }
    }
}