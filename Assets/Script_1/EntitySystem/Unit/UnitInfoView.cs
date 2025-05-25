using Script.EntitySystem.Unit;
using TMPro;
using UnityEngine;

namespace Script
{
    public class UnitInfoView : MonoBehaviour
    {
        private UnitData unitData;
        public TextMeshProUGUI unitIdText;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI unitHealthText;
        public TextMeshProUGUI unitAttackText;
        public TextMeshProUGUI unitAttackRangeText;
        public TextMeshProUGUI unitAttackSpeedText;
        public TextMeshProUGUI unitSpeedText;

        public void DisplayUnitInfo(UnitData unitData)
        {
            this.unitData = unitData;
            unitIdText.text = "Name: " + unitData.id;
            unitNameText.text = "Name: " + unitData.name;
            unitHealthText.text = "Health: " + unitData.health;
            unitAttackText.text = "Attack: " + unitData.attackDamage;
            unitAttackRangeText.text = "Attack Range: " + unitData.attackRange;
            unitAttackSpeedText.text = "Attack Speed: " + unitData.attackSpeed;
            unitSpeedText.text = "Speed: " + unitData.speed;
        }
    }
}