using System.Collections.Generic;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.Command
{
    public class MoveUnitCommand : ICommand
    {
        private Unit unit;
        private Vector2Int targetPosition;
        private List<Unit> group;
        private List<Vector2Int> path;

        public MoveUnitCommand(Unit unit, Vector2Int targetPosition, List<Unit> group)
        {
            this.unit = unit;
            this.targetPosition = targetPosition;
            this.group = group;
        }

        public List<Vector2Int> GetPath() { return path; }
        public void SetPath(List<Vector2Int> path) { this.path = path; }

        public bool CanExecute(IWorld world)
        {
            // Implementa la logica per controllare se l'unità può muoversi nella posizione target.
            // Puoi controllare se la cella è percorribile, se ci sono ostacoli, ecc.
            return world.IsPositionValid(targetPosition) && world.GetCell(targetPosition).IsWalkable();
        }

        public void Execute(IWorld world)
        {
            if (CanExecute(world))
            {
                path = world.FindPath(unit.GetPosition(), targetPosition, group);
                if (path != null && path.Count > 0)
                {
                    //muovi l'unità
                    world.MoveUnit(unit, targetPosition, group);
                } 
                else
                {
                    Debug.LogWarning("Percorso non trovato per l'unità " + unit.GetId());
                    path = new List<Vector2Int>();
                }
            }
            else
            {
                Debug.LogWarning("Cannot execute MoveUnitCommand: Unit cannot move to " + targetPosition);
            }
        }
    }
}