using Script.EntitySystem.Entity;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.Command
{
    public class AttackUnitCommand : ICommand
    {
        private Unit attacker;
        private IEntity target;

        public AttackUnitCommand(Unit attacker, IEntity target)
        {
            this.attacker = attacker;
            this.target = target;
        }

        public bool CanExecute(IWorld world)
        {
            // Implementa la logica per controllare se l'unità può attaccare il target.
            // Puoi controllare se il target è nel raggio d'attacco, se è ancora vivo, ecc.
            if(target == null) return false;
            float distance = Vector2.Distance(new Vector2(attacker.GetPosition().x, attacker.GetPosition().y), new Vector2(target.GetPosition().x, target.GetPosition().y));
            return distance <= attacker.GetAttackRange() && target.GetHealth() > 0;
        }

        public void Execute(IWorld world)
        {
            if (CanExecute(world))
            {
                // Implementa la logica per eseguire l'attacco.
                world.AttackUnit(attacker, target);
            }
            else
            {
                Debug.LogWarning("Cannot execute AttackUnitCommand: Target is out of range or invalid.");
            }
        }
    }
}