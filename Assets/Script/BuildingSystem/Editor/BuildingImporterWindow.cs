#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Script.BuildingSystem;
using Script.ResourceSystem.Enums;
using UnityEditor;
using UnityEngine;

namespace Script.BuildingSystem.Editor
{
	/// <summary>
	/// Editor window per automatizzare l'import di nuovi edifici:
	/// PNG → Prefab → BuildingConfigSO
	/// Segue la pipeline definita in Documentation/AssetPipeline.md
	/// </summary>
	public class BuildingImporterWindow : EditorWindow
	{
		[MenuItem("Tools/Building System/Building Importer")]
		public static void ShowWindow()
		{
			GetWindow<BuildingImporterWindow>("Building Importer");
		}

		#region Fields

		[SerializeField] private Texture2D sourcePng;
		[SerializeField] private string spriteName = "";
		[SerializeField] private string entityName = "";
		[SerializeField] private string description = "";
		[SerializeField] private int width = 1;
		[SerializeField] private int height = 1;
		[SerializeField] private List<ResourceCostEntry> costs = new();

		#endregion

		#region Helper Classes

		/// <summary>
		/// Entry serializzabile per la lista costi nell'UI (ResourceCost non è serializzabile direttamente nell'EditorWindow).
		/// </summary>
		[Serializable]
		private struct ResourceCostEntry
		{
			public ResourceType type;
			public int amount;
		}

		#endregion

		#region Unity Lifecycle

		private void OnGUI()
		{
			DrawHeader();
			DrawSourcePngField();
			DrawMetadataFields();
			DrawCostsList();
			DrawImportButton();
		}

		#endregion

		#region UI Drawing

		private void DrawHeader()
		{
			GUILayout.Space(10);
			EditorGUILayout.LabelField("Building Importer", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Automatizza: PNG → Prefab → BuildingConfigSO\n" +
				"Segui la pipeline in Documentation/AssetPipeline.md",
				MessageType.Info);
			GUILayout.Space(10);
		}

		private void DrawSourcePngField()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("1. Sorgente PNG", EditorStyles.boldLabel);
			Texture2D prevPng = sourcePng;
			sourcePng = (Texture2D)EditorGUILayout.ObjectField(
				"PNG Edificio",
				sourcePng,
				typeof(Texture2D),
				false,
				GUILayout.Height(64));

			// Auto-deriva spriteName (con _) e entityName (con spazi) dal nome del PNG
			if (sourcePng != null && sourcePng != prevPng)
			{
				spriteName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sourcePng));
				if (string.IsNullOrWhiteSpace(entityName))
				{
					entityName = spriteName.Replace("_", " ");
				}
			}

			if (sourcePng != null)
			{
				string path = AssetDatabase.GetAssetPath(sourcePng);
				if (!path.StartsWith("Assets/_Sprites/Buildings/"))
				{
					EditorGUILayout.HelpBox(
						"Consigliato: posiziona il PNG in Assets/_Sprites/Buildings/",
						MessageType.Warning);
				}

				// Mostra dimensioni per calcolo PPU
				EditorGUILayout.LabelField($"Dimensioni: {sourcePng.width} x {sourcePng.height} px");
				EditorGUILayout.LabelField($"PPU calcolato: {sourcePng.width / 2} (width / 2)");
			}

			EditorGUILayout.EndVertical();
			GUILayout.Space(5);
		}

		private void DrawMetadataFields()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("2. Metadati Edificio", EditorStyles.boldLabel);

			if (!string.IsNullOrWhiteSpace(spriteName))
			{
				EditorGUILayout.LabelField($"Nome Sprite (file): {spriteName}");
			}

			entityName = EditorGUILayout.TextField("Entity Name (SO)", entityName);
			description = EditorGUILayout.TextArea(description, GUILayout.Height(50));

			EditorGUILayout.BeginHorizontal();
			width = EditorGUILayout.IntField("Width (celle)", width);
			height = EditorGUILayout.IntField("Height (celle)", height);
			EditorGUILayout.EndHorizontal();

			if (width < 1) width = 1;
			if (height < 1) height = 1;

			EditorGUILayout.EndVertical();
			GUILayout.Space(5);
		}

		private void DrawCostsList()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("3. Costi Risorse", EditorStyles.boldLabel);

			// Header
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tipo", GUILayout.Width(150));
			EditorGUILayout.LabelField("Amount", GUILayout.Width(80));
			EditorGUILayout.LabelField("", GUILayout.Width(25));
			EditorGUILayout.EndHorizontal();

			// List entries
			for (int i = 0; i < costs.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();

				var entry = costs[i];
				entry.type = (ResourceType)EditorGUILayout.EnumPopup(entry.type, GUILayout.Width(150));
				entry.amount = EditorGUILayout.IntField(entry.amount, GUILayout.Width(80));
				costs[i] = entry;

				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					costs.RemoveAt(i);
					// Exit early to avoid index issues after removal
					EditorGUILayout.EndHorizontal();
					break;
				}

				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(5);
			if (GUILayout.Button("Aggiungi Costo"))
			{
				costs.Add(new ResourceCostEntry { type = ResourceType.Wood, amount = 10 });
			}

			EditorGUILayout.EndVertical();
			GUILayout.Space(10);
		}

		private void DrawImportButton()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("4. Esegui Import", EditorStyles.boldLabel);

			bool canImport = sourcePng != null
						   && !string.IsNullOrWhiteSpace(spriteName)
						   && width >= 1
						   && height >= 1;

			using (new EditorGUI.DisabledScope(!canImport))
			{
				if (GUILayout.Button("IMPORTA EDIFICIO", GUILayout.Height(40)))
				{
					ImportBuilding();
				}
			}

			if (!canImport)
			{
				EditorGUILayout.HelpBox("Completa tutti i campi obbligatori per abilitare l'import.", MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Import Logic

		private void ImportBuilding()
		{
			try
			{
				ValidateInput();

				// Check for existing assets (duplicate detection)
				if (!ConfirmOverwriteIfExists())
				{
					return; // User cancelled
				}

				// Step 3: Configura PNG meta
				ConfigureSpriteMeta(sourcePng);
				AssetDatabase.Refresh();

				// Step 4: Crea Prefab
				GameObject prefab = CreatePrefab(sourcePng, width, height);

				// Step 5: Crea BuildingConfigSO
				CreateBuildingConfigSO(prefab, width, height);

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				Debug.Log($"[BuildingImporter] '{spriteName}' importato con successo!");
				EditorUtility.DisplayDialog("Successo", $"Edificio '{spriteName}' importato correttamente!\nEntityName SO: {entityName}", "OK");

				// Reset campi per il prossimo import
				ResetFields();
			}
			catch (Exception e)
			{
				Debug.LogError($"[BuildingImporter] Errore durante l'import: {e.Message}\n{e.StackTrace}");
				EditorUtility.DisplayDialog("Errore", $"Import fallito:\n{e.Message}", "OK");
			}
		}

		private bool ConfirmOverwriteIfExists()
		{
			string prefabPath = "Assets/_Prefabs/Buildings/" + spriteName + ".prefab";
			string soPath = "Assets/Resources/Buildings/" + spriteName + ".asset";

			bool prefabExists = File.Exists(prefabPath);
			bool soExists = File.Exists(soPath);

			if (prefabExists || soExists)
			{
				string message = $"Esiste già un edificio con nome '{spriteName}':\n";
				if (prefabExists) message += $"• Prefab: {prefabPath}\n";
				if (soExists) message += $"• SO: {soPath}\n";
				message += "\nSovrascrivere?";

				return EditorUtility.DisplayDialog("Duplicato rilevato", message, "Sovrascrivi", "Annulla");
			}

			return true; // No duplicates, proceed
		}

		private void ResetFields()
		{
			sourcePng = null;
			spriteName = "";
			entityName = "";
			description = "";
			width = 1;
			height = 1;
			costs.Clear();
		}

		private void ValidateInput()
		{
			if (sourcePng == null)
				throw new ArgumentException("Seleziona un PNG sorgente.");

			if (string.IsNullOrWhiteSpace(spriteName))
				throw new ArgumentException("Nome sprite non valido. Seleziona un PNG.");

			if (width < 1 || height < 1)
				throw new ArgumentException("Width e Height devono essere ≥ 1.");
		}

		/// <summary>
		/// Step 3: Configura le impostazioni di import del PNG (PPU, pivot, filter, compressione).
		/// </summary>
		private void ConfigureSpriteMeta(Texture2D png)
		{
			string path = AssetDatabase.GetAssetPath(png);
			var importer = (TextureImporter)AssetImporter.GetAtPath(path);

			if (importer == null)
				throw new InvalidOperationException($"Impossibile ottenere TextureImporter per: {path}");

			// Configurazione base
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.filterMode = FilterMode.Point;
			importer.mipmapEnabled = false;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.textureCompression = TextureImporterCompression.Uncompressed;

			// Calcola PPU = width_png / 2 (formula da AssetPipeline.md §2)
			int ppu = png.width / 2;

			// Configura texture settings
			var settings = new TextureImporterSettings();
			importer.ReadTextureSettings(settings);

			settings.spritePixelsPerUnit = ppu;
			settings.spritePivot = new Vector2(0.5f, 0f); // Bottom center
			settings.spriteAlignment = (int)SpriteAlignment.Custom;

			importer.SetTextureSettings(settings);
			importer.SaveAndReimport();

			Debug.Log($"[BuildingImporter] PNG configurato: PPU={ppu}, Pivot=Bottom, Filter=Point");
		}

		/// <summary>
		/// Step 4: Crea il Prefab con gerarchia Root → Renderer.
		/// </summary>
		private GameObject CreatePrefab(Texture2D png, int width, int height)
		{
			string prefabName = spriteName + ".prefab";
			string prefabPath = "Assets/_Prefabs/Buildings/" + prefabName;

			// Assicura che la directory esista
			EnsureDirectoryExists("Assets/_Prefabs/Buildings/");

			// Crea GameObject root temporaneo
			GameObject root = new GameObject(spriteName);
			Undo.RegisterCreatedObjectUndo(root, "Create Building Prefab");

			// Configura root
			root.tag = "Buildings";
			root.layer = LayerMask.NameToLayer("Building");
			root.AddComponent<Building>();

			// Crea child Renderer
			GameObject rendererObj = new GameObject("Renderer");
			Undo.RegisterCreatedObjectUndo(rendererObj, "Create Renderer Child");
			rendererObj.transform.SetParent(root.transform, false);

			// Configura Transform
			rendererObj.transform.localPosition = new Vector3(0, -0.5f, 0);
			rendererObj.transform.localScale = new Vector3(width, height, 1f);

			// SpriteRenderer
			var sr = rendererObj.AddComponent<SpriteRenderer>();
			Undo.RegisterCreatedObjectUndo(sr, "Add SpriteRenderer");

			// Carica lo sprite dal PNG
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(png));
			if (sprite == null)
			{
				throw new InvalidOperationException("Sprite non trovato dopo reimport. Controlla il PNG.");
			}

			sr.sprite = sprite;
			sr.sortingLayerName = "OnTiles";
			sr.sortingOrder = 0;
			sr.spriteSortPoint = SpriteSortPoint.Pivot;

			// PolygonCollider2D (Unity lo rigenera automaticamente dallo sprite)
			var collider = rendererObj.AddComponent<PolygonCollider2D>();
			collider.isTrigger = true;

			// Salva come prefab
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

			// Imposta il riferimento Building._renderer via SerializedObject
			// Recupera i componenti dal prefab salvato (non dalla scena temporanea)
			var buildingOnPrefab = prefab.GetComponentInChildren<Building>();
			var srOnPrefab = prefab.GetComponentInChildren<SpriteRenderer>();

			if (buildingOnPrefab != null && srOnPrefab != null)
			{
				var buildingSo = new SerializedObject(buildingOnPrefab);
				var rendererProp = buildingSo.FindProperty("_renderer");
				if (rendererProp != null)
				{
					rendererProp.objectReferenceValue = srOnPrefab;
					buildingSo.ApplyModifiedPropertiesWithoutUndo();
				}
				else
				{
					Debug.LogWarning("[BuildingImporter] Campo '_renderer' non trovato su Building. Impostalo manualmente nel prefab.");
				}
			}

			// Cleanup oggetto temporaneo
			DestroyImmediate(root);

			Debug.Log($"[BuildingImporter] Prefab creato: {prefabPath}");
			return prefab;
		}

		/// <summary>
		/// Step 5: Crea il BuildingConfigSO e popola i campi.
		/// </summary>
		private void CreateBuildingConfigSO(GameObject prefab, int width, int height)
		{
			string soName = spriteName + ".asset";
			string soPath = "Assets/Resources/Buildings/" + soName;

			EnsureDirectoryExists("Assets/Resources/Buildings/");

			var so = ScriptableObject.CreateInstance<BuildingConfigSO>();
			Undo.RegisterCreatedObjectUndo(so, "Create BuildingConfigSO");

			// Campi automatici
			so.Prefab = prefab;
			so.SortingLayer = "OnTiles";
			so.BaseSortingOrder = 0;
			so.Width = width;
			so.Height = height;

			// Campi dall'UI
			so.EntityName = entityName;
			so.Description = description;

			// Icona = Sprite derivato dal PNG sorgente (sub-asset dello stesso Texture2D)
			string pngPath = AssetDatabase.GetAssetPath(sourcePng);
			so.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

			// Costi
			so.Costs = new List<BuildingConfigSO.ResourceCost>();
			foreach (var entry in costs)
			{
				if (entry.amount > 0)
				{
					so.Costs.Add(new BuildingConfigSO.ResourceCost
					{
						Type = entry.type,
						Amount = entry.amount
					});
				}
			}

			AssetDatabase.CreateAsset(so, soPath);

			Debug.Log($"[BuildingImporter] BuildingConfigSO creato: {soPath}");
		}

		/// <summary>
		/// Assicura che una directory esista, creandola se necessario.
		/// </summary>
		private void EnsureDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
				AssetDatabase.Refresh();
			}
		}

		#endregion
	}
}
#endif