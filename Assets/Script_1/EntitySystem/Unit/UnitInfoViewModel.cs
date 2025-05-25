namespace Script.EntitySystem.Unit
{
    public class UnitInfoViewModel
    {
        private Unit selectedUnit;

        public void SetSelectedUnit(Unit unit)
        {
            selectedUnit = unit;
        }

        public UnitData GetSelectedUnitData()
        {
            if (selectedUnit != null)
            {
                return new UnitData
                {
                    id = selectedUnit.GetId(),
                    name = selectedUnit.GetUnitName(),
                    health = selectedUnit.GetHealth(),
                    attackDamage = selectedUnit.GetAttackDamage(),
                    attackRange = selectedUnit.GetAttackRange(),
                    attackSpeed = selectedUnit.GetAttackSpeed(),
                    speed = selectedUnit.GetSpeed(),
                    maxHealth = selectedUnit.GetMaxHealth()
                };
            }
            else
            {
                return new UnitData();
            }
        }
    }
}