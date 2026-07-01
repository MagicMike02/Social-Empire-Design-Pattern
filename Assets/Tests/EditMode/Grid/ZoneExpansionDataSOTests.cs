using System.Collections.Generic;
using NUnit.Framework;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Tests.EditMode.Grid
{
    public class ZoneExpansionDataSOTests
    {
        [Test]
        public void GetCostForNextZone_WhenLevelIsConfigured_ReturnsConfiguredCosts()
        {
            var so = ScriptableObject.CreateInstance<ZoneExpansionDataSO>();
            so.expansionLevels = new List<ZoneExpansionLevel>
            {
                new ZoneExpansionLevel
                {
                    costs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceType = ResourceType.Gold, amount = 100 },
                        new ResourceCost { resourceType = ResourceType.Wood, amount = 25 },
                        new ResourceCost { resourceType = ResourceType.Gold, amount = 50 }
                    }
                }
            };

            var costs = so.GetCostForNextZone(0);

            Assert.That(costs[ResourceType.Gold], Is.EqualTo(150));
            Assert.That(costs[ResourceType.Wood], Is.EqualTo(25));
        }

        [Test]
        public void GetCostForNextZone_WhenBeyondConfiguredLevels_UsesFallbackFormula()
        {
            var so = ScriptableObject.CreateInstance<ZoneExpansionDataSO>();
            so.expansionLevels = new List<ZoneExpansionLevel>();
            so.useFormulaFallback = true;
            so.baseFallbackCost = new ResourceCost { resourceType = ResourceType.Gold, amount = 100 };
            so.fallbackMultiplierPerLevel = 1.5f;

            var costs = so.GetCostForNextZone(2);

            Assert.That(costs[ResourceType.Gold], Is.EqualTo(225));
        }

        [Test]
        public void GetCostForNextZone_WhenFallbackDisabled_ReturnsEmptyDictionary()
        {
            var so = ScriptableObject.CreateInstance<ZoneExpansionDataSO>();
            so.expansionLevels = new List<ZoneExpansionLevel>();
            so.useFormulaFallback = false;

            var costs = so.GetCostForNextZone(4);

            Assert.That(costs, Is.Empty);
        }
    }
}
