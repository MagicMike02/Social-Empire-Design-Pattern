using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Script.BuildingSystem;

namespace Tests.EditMode.SaveSystem
{
    public class BuildingCatalogTests
    {
        private const string ResourcesFolder = "Assets/Resources/Buildings";
        private const string AssetPath = ResourcesFolder + "/TestBuildingConfigSO.asset";

        [SetUp]
        public void SetUp()
        {
            // Ensure Resources/Buildings folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Buildings"))
                AssetDatabase.CreateFolder("Assets/Resources", "Buildings");

            // Create a dummy BuildingConfigSO asset
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.name = "TestBuilding"; // name used as key in catalog
            // Minimal required fields (width/height) – set to 1 to avoid nulls
            config.Width = 1;
            config.Height = 1;
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            // Delete the created asset and folder
            if (AssetDatabase.LoadAssetAtPath<BuildingConfigSO>(AssetPath) != null)
                AssetDatabase.DeleteAsset(AssetPath);
            // Optionally delete the Buildings folder if empty
            if (AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                var guids = AssetDatabase.FindAssets("", new[] { ResourcesFolder });
                if (guids.Length == 0)
                    AssetDatabase.DeleteAsset(ResourcesFolder);
            }
            AssetDatabase.Refresh();
        }

        [Test]
        public void BuildingCatalog_LoadsAllConfigs_FromResources()
        {
            // Act: instantiate the catalog (loads on construction)
            var catalog = new BuildingCatalog();
            IReadOnlyDictionary<string, BuildingConfigSO> dict = catalog.GetCatalog();

            // Assert: the dictionary contains the test config by its name
            Assert.IsNotNull(dict, "Catalog dictionary should not be null");
            Assert.IsTrue(dict.ContainsKey("TestBuilding"), "Catalog should contain the created TestBuilding config");
            var loadedConfig = dict["TestBuilding"];
            Assert.IsNotNull(loadedConfig, "Loaded config should not be null");
            Assert.AreEqual(1, loadedConfig.Width, "Width should match the value set in the test asset");
            Assert.AreEqual(1, loadedConfig.Height, "Height should match the value set in the test asset");
        }
    }
}
