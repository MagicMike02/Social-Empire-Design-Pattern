using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json;
using Script.Core.SaveSystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Tests.PlayMode.SaveSystem
{
    /// <summary>
    /// Test PlayMode end-to-end per il SaveSystem.
    /// Verifica il roundtrip completo: serializzazione → deserializzazione → validazione dati.
    /// </summary>
    public class SaveSystemRoundtripTests
    {
        #region Constants

        private const int TestGridWidth = 100;
        private const int TestGridHeight = 100;
        private const int TestZoneSize = 20;

        #endregion

        #region Setup / Teardown

        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testSavePath = Path.Combine(Application.persistentDataPath, "test_savegame.json");
            CleanupTestFile();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupTestFile();
        }

        private void CleanupTestFile()
        {
            if (File.Exists(_testSavePath))
            {
                File.Delete(_testSavePath);
            }
        }

        #endregion

        #region S3-07: Roundtrip Completo

        [Test]
        public void GameSaveData_Roundtrip_SerializzaEDeserializza_Correttamente()
        {
            // Arrange: crea un GameSaveData con dati significativi
            var original = new GameSaveData
            {
                schemaVersion = 1,
                savedAt = System.DateTime.UtcNow.ToString("o"),
                lastExitAt = System.DateTime.UtcNow.AddHours(-2).ToString("o"),
                resources = new Dictionary<string, int>
                {
                    { "Wood", 150 },
                    { "Stone", 80 },
                    { "Gold", 45 },
                    { "Meat", 30 }
                },
                playerLevel = 5,
                gridWidth = TestGridWidth,
                gridHeight = TestGridHeight,
                placedBuildings = new Dictionary<string, string>
                {
                    { "10,15", "House_L1" },
                    { "12,15", "Farm_L1" },
                    { "50,60", "Barracks_L2" },
                    { "0,0", "TownHall_L1" },
                    { "99,99", "Watchtower_L3" }
                },
                unlockedZoneIndices = new List<int> { 0, 1, 2, 5, 6 },
                zoneSize = TestZoneSize
            };

            // Act: serializza in JSON
            string json = JsonConvert.SerializeObject(original, Formatting.Indented);

            // Assert: il JSON contiene i dati attesi
            Assert.That(json, Does.Contain("\"schemaVersion\": 1"));
            Assert.That(json, Does.Contain("\"Wood\""));
            Assert.That(json, Does.Contain("\"House_L1\""));
            Assert.That(json, Does.Contain("\"10,15\""));

            // Act: deserializza
            var restored = JsonConvert.DeserializeObject<GameSaveData>(json);

            // Assert: verifica integrità roundtrip
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.schemaVersion, Is.EqualTo(original.schemaVersion));
            Assert.That(restored.playerLevel, Is.EqualTo(original.playerLevel));
            Assert.That(restored.gridWidth, Is.EqualTo(original.gridWidth));
            Assert.That(restored.gridHeight, Is.EqualTo(original.gridHeight));
            Assert.That(restored.zoneSize, Is.EqualTo(original.zoneSize));
            Assert.That(restored.savedAt, Is.EqualTo(original.savedAt));
            Assert.That(restored.lastExitAt, Is.EqualTo(original.lastExitAt));

            // Risorse
            Assert.That(restored.resources, Is.Not.Null);
            Assert.That(restored.resources.Count, Is.EqualTo(original.resources.Count));
            Assert.That(restored.resources["Wood"], Is.EqualTo(150));
            Assert.That(restored.resources["Stone"], Is.EqualTo(80));
            Assert.That(restored.resources["Gold"], Is.EqualTo(45));
            Assert.That(restored.resources["Meat"], Is.EqualTo(30));

            // Edifici
            Assert.That(restored.placedBuildings, Is.Not.Null);
            Assert.That(restored.placedBuildings.Count, Is.EqualTo(original.placedBuildings.Count));
            Assert.That(restored.placedBuildings["10,15"], Is.EqualTo("House_L1"));
            Assert.That(restored.placedBuildings["99,99"], Is.EqualTo("Watchtower_L3"));

            // Zone
            Assert.That(restored.unlockedZoneIndices, Is.Not.Null);
            Assert.That(restored.unlockedZoneIndices.Count, Is.EqualTo(original.unlockedZoneIndices.Count));
            Assert.That(restored.unlockedZoneIndices, Is.EquivalentTo(new[] { 0, 1, 2, 5, 6 }));

            // Validazione
            Assert.That(restored.IsValid(), Is.True);
        }

        [Test]
        public void GameSaveData_Roundtrip_ConEdificiVuoti_MantieneIntegrita()
        {
            var original = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            original.resources["Wood"] = 100;

            string json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var restored = JsonConvert.DeserializeObject<GameSaveData>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.placedBuildings, Is.Not.Null);
            Assert.That(restored.placedBuildings.Count, Is.EqualTo(0));
            Assert.That(restored.resources["Wood"], Is.EqualTo(100));
            Assert.That(restored.IsValid(), Is.True);
        }

        #endregion

        #region S3-07: Validazione GameSaveData

        [Test]
        public void IsValid_DefaultData_ReturnsTrue()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            Assert.That(data.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_SchemaVersionZero_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.schemaVersion = 0;
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_GridWidthZero_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(0, TestGridHeight, TestZoneSize);
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_ZoneSizeZero_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, 0);
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_NullResources_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.resources = null;
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_NullPlacedBuildings_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.placedBuildings = null;
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_BuildingOutOfBounds_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.placedBuildings["200,50"] = "House_L1"; // x > gridWidth
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_BuildingKeyMalformed_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.placedBuildings["abc,def"] = "House_L1";
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_BuildingKeySinglePart_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.placedBuildings["10"] = "House_L1";
            Assert.That(data.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_EmptyBuildingId_ReturnsFalse()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            data.placedBuildings["10,15"] = "";
            Assert.That(data.IsValid(), Is.False);
        }

        #endregion

        #region S3-07: JsonSaveSystem I/O con Try/Catch (S3-06)

        [Test]
        public void JsonSaveSystem_SaveAndLoad_RoundtripCompleto()
        {
            var persistence = new JsonSaveSystem();
            var original = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            original.resources["Wood"] = 200;
            original.resources["Gold"] = 100;
            original.placedBuildings["5,5"] = "Farm_L1";
            original.playerLevel = 3;

            // Save
            persistence.SaveGame(original);

            // Load
            var loaded = persistence.LoadGame();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.schemaVersion, Is.EqualTo(original.schemaVersion));
            Assert.That(loaded.playerLevel, Is.EqualTo(3));
            Assert.That(loaded.resources["Wood"], Is.EqualTo(200));
            Assert.That(loaded.resources["Gold"], Is.EqualTo(100));
            Assert.That(loaded.placedBuildings["5,5"], Is.EqualTo("Farm_L1"));
            Assert.That(loaded.IsValid(), Is.True);
        }

        [Test]
        public void JsonSaveSystem_LoadGame_FileInesistente_ReturnsNull()
        {
            CleanupTestFile();
            var persistence = new JsonSaveSystem();
            var loaded = persistence.LoadGame();
            Assert.That(loaded, Is.Null);
        }

        [Test]
        public void JsonSaveSystem_DeleteGame_RimuoveFile()
        {
            var persistence = new JsonSaveSystem();
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);

            persistence.SaveGame(data);
            Assert.That(File.Exists(_testSavePath), Is.True);

            persistence.DeleteGame();
            Assert.That(File.Exists(_testSavePath), Is.False);
        }

        [Test]
        public void JsonSaveSystem_DeleteGame_FileInesistente_NoException()
        {
            CleanupTestFile();
            var persistence = new JsonSaveSystem();
            Assert.DoesNotThrow(() => persistence.DeleteGame());
        }

        [Test]
        public void JsonSaveSystem_SaveGame_DataNull_NoException()
        {
            var persistence = new JsonSaveSystem();
            Assert.DoesNotThrow(() => persistence.SaveGame(null));
        }

        #endregion

        #region S3-07: OfflineProgressCalculator (S3-04)

        [Test]
        public void OfflineProgressCalculator_LastExit2HoursAgo_CalcolaDelta()
        {
            var calculator = new OfflineProgressCalculator();
            var saveData = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            saveData.lastExitAt = System.DateTime.UtcNow.AddHours(-2).ToString("o");

            var result = calculator.Calculate(saveData);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Elapsed.TotalHours, Is.InRange(1.9, 2.1));
            Assert.That(result.WasCapped, Is.False);
            Assert.That(result.Deltas[ResourceType.Wood], Is.EqualTo(20));  // 10/h * 2h
            Assert.That(result.Deltas[ResourceType.Stone], Is.EqualTo(16)); // 8/h * 2h
            Assert.That(result.Deltas[ResourceType.Gold], Is.EqualTo(10));  // 5/h * 2h
            Assert.That(result.Deltas[ResourceType.Meat], Is.EqualTo(12));  // 6/h * 2h
        }

        [Test]
        public void OfflineProgressCalculator_LastExitNull_ReturnsEmpty()
        {
            var calculator = new OfflineProgressCalculator();
            var saveData = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            saveData.lastExitAt = null;

            var result = calculator.Calculate(saveData);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Does.Contain("lastExitAt assente"));
        }

        [Test]
        public void OfflineProgressCalculator_SaveDataNull_ReturnsEmpty()
        {
            var calculator = new OfflineProgressCalculator();
            var result = calculator.Calculate(null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Does.Contain("saveData null"));
        }

        [Test]
        public void OfflineProgressCalculator_LastExitInvalidFormat_ReturnsEmpty()
        {
            var calculator = new OfflineProgressCalculator();
            var saveData = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            saveData.lastExitAt = "not-a-date";

            var result = calculator.Calculate(saveData);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Does.Contain("lastExitAt non valido"));
        }

        [Test]
        public void OfflineProgressCalculator_Over8Hours_CappedAt8()
        {
            var calculator = new OfflineProgressCalculator();
            var saveData = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            saveData.lastExitAt = System.DateTime.UtcNow.AddHours(-24).ToString("o");

            var result = calculator.Calculate(saveData);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.WasCapped, Is.True);
            Assert.That(result.ElapsedHoursCapped, Is.EqualTo(8.0));
            Assert.That(result.Deltas[ResourceType.Wood], Is.EqualTo(80));  // 10/h * 8h (capped)
        }

        [Test]
        public void OfflineProgressCalculator_FutureTimestamp_ReturnsEmpty()
        {
            var calculator = new OfflineProgressCalculator();
            var saveData = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);
            saveData.lastExitAt = System.DateTime.UtcNow.AddHours(1).ToString("o");

            var result = calculator.Calculate(saveData);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Does.Contain("Elapsed <= 0"));
        }

        #endregion

        #region S3-07: CreateDefault Factory

        [Test]
        public void CreateDefault_ImpostaValoriCorretti()
        {
            var data = GameSaveData.CreateDefault(TestGridWidth, TestGridHeight, TestZoneSize);

            Assert.That(data.schemaVersion, Is.EqualTo(1));
            Assert.That(data.playerLevel, Is.EqualTo(1));
            Assert.That(data.gridWidth, Is.EqualTo(TestGridWidth));
            Assert.That(data.gridHeight, Is.EqualTo(TestGridHeight));
            Assert.That(data.zoneSize, Is.EqualTo(TestZoneSize));
            Assert.That(data.resources, Is.Not.Null.And.Empty);
            Assert.That(data.placedBuildings, Is.Not.Null.And.Empty);
            Assert.That(data.unlockedZoneIndices, Is.EquivalentTo(new[] { 0 }));
            Assert.That(data.savedAt, Is.Not.Null.And.Not.Empty);
            Assert.That(data.lastExitAt, Is.Not.Null.And.Not.Empty);
            Assert.That(data.IsValid(), Is.True);
        }

        #endregion
    }
}