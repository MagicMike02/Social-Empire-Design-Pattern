using System.Collections.Generic;
using NUnit.Framework;
using Script.EconomySystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Tests.EditMode.Economy
{
    public class GameEconomyManagerTests
    {
        private GameObject _gameObject;
        private GameEconomyManager _economy;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("EconomyManagerTests");
            _economy = _gameObject.AddComponent<GameEconomyManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Awake_InitializesAllResourcesToZero()
        {
            Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(0));
            Assert.That(_economy.GetResourceAmount(ResourceType.Stone), Is.EqualTo(0));
            Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(0));
        }

        [Test]
        public void SpendResources_DeductsAmounts_WhenAffordable()
        {
            _economy.SetResource(ResourceType.Wood, 20);
            _economy.SetResource(ResourceType.Gold, 10);

            var costs = new Dictionary<ResourceType, int>
            {
                [ResourceType.Wood] = 5,
                [ResourceType.Gold] = 3
            };

            Assert.That(_economy.SpendResources(costs), Is.True);
            Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(15));
            Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(7));
        }

        [Test]
        public void CanAfford_ReturnsFalse_WhenAnyResourceIsMissing()
        {
            _economy.SetResource(ResourceType.Wood, 4);

            var costs = new Dictionary<ResourceType, int>
            {
                [ResourceType.Wood] = 5,
                [ResourceType.Gold] = 1
            };

            Assert.That(_economy.CanAfford(costs), Is.False);
        }
    }
}
