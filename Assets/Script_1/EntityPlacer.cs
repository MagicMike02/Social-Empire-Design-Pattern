using System.Collections.Generic;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Unit;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class EntityPlacer
    {
        private float monsterSpawnChance;
        private List<Vector2Int> resourceLocations;

        public EntityPlacer(float monsterSpawnChance, List<Vector2Int> resourceLocations)
        {
            this.monsterSpawnChance = monsterSpawnChance;
            this.resourceLocations = resourceLocations;
        }

        public void PlaceChunkEntities(Chunk chunk)
        {
            Cell[][] cells = chunk.GetCells();
            for (int x = 0; x < cells.Length; x++)
            {
                for (int y = 0; y < cells[x].Length; y++)
                {
                    // Calcola la posizione globale della cella nel mondo.
                    Vector2Int globalPosition = new Vector2Int(chunk.GetPosition().x * cells.Length + x, chunk.GetPosition().y * cells[x].Length + y);

                    // Posiziona un mostro con una certa probabilità.
                    if (Random.Range(0f, 1f) < monsterSpawnChance)
                    {
                        // Crea un mostro (esempio: livello 1, 100 salute).
                        IEntity monster = GameManager.Instance.entityFactory.CreateEntity("Troll", globalPosition); //usa entity factory
                        GameManager.Instance.CreateEntity(monster);
                    }
                }
            }
        }
    }
}