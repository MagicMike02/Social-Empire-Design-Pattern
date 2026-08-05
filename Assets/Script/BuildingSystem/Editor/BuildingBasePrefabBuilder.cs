#if UNITY_EDITOR
using System.IO;
using Script.Core.Entities;
using UnityEditor;
using UnityEngine;

namespace Script.BuildingSystem.Editor
{
    /// <summary>
    /// Editor utility che genera <c>Building_Base.prefab</c> con la gerarchia
    /// completa di componenti entity (EntityInfoProvider, SelectableComponent,
    /// HighlightableComponent, Building) e figli visivi (MainSprite, Shadow,
    /// HoverOutline, SelectionCircle, WorldUI/HealthBar).
    ///
    /// Il prefab generato è la base da cui derivare House, Tower, ecc. come
    /// Prefab Variant, garantendo che ogni edificio abbia i componenti entity
    /// già cablati e consistenti.
    /// </summary>
    public static class BuildingBasePrefabBuilder
    {
        private const string OutputPath = "Assets/_Prefabs/Buildings/Building_Base.prefab";
        private const string CircleSpritePath = "Assets/_Sprites/Circle_Outlined.png";

        [MenuItem("Tools/Social Empire/Create Building_Base Prefab")]
        public static void CreateBuildingBasePrefab()
        {
            // Carica la sprite del cerchio outline (opzionale: se manca, i figli
            // visivi vengono creati comunque con sprite null, da assegnare a mano).
            Sprite circleSprite = LoadCircleSprite();

            // Crea la gerarchia temporanea
            var root = CreateRoot(circleSprite);

            // Assicura che la cartella di destinazione esista
            EnsureDirectoryExists(OutputPath);

            // Salva come prefab asset (sovrascrive se esiste già)
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPath);

            // Distruggi il temporaneo nella scena
            Object.DestroyImmediate(root);

            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[BuildingBasePrefabBuilder] Prefab creato: {OutputPath}");
        }

        #region Hierarchy Construction

        private static GameObject CreateRoot(Sprite circleSprite)
        {
            var root = new GameObject("Building_Base");
            root.layer = LayerMask.NameToLayer("Building");

            // --- Componenti entity sulla root ---
            // EntityInfoProvider PRIMA (SelectableComponent ha RequireComponent)
            var infoProvider = root.AddComponent<EntityInfoProvider>();
            root.AddComponent<SelectableComponent>();
            root.AddComponent<HighlightableComponent>();
            root.AddComponent<Building>();

            // --- Figlio: Visuals/MainSprite (renderer principale dell'edificio) ---
            var visuals = CreateChild(root, "Visuals");
            visuals.layer = root.layer;

            var mainSprite = CreateSpriteChild(visuals, "MainSprite", null);
            var mainRenderer = mainSprite.GetComponent<SpriteRenderer>();
            mainRenderer.sortingLayerName = "OnTiles";
            mainRenderer.sortingOrder = 0;

            // --- Figlio: Visuals/Shadow (ombra sotto l'edificio) ---
            var shadow = CreateSpriteChild(visuals, "Shadow", null);
            var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            shadowRenderer.sortingLayerName = "OnTiles";
            shadowRenderer.sortingOrder = -10;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);

            // --- Figlio: HoverOutline (outline bianco su hover) ---
            // Usa la stessa sprite del cerchio outline, sorting sopra MainSprite
            var hoverOutline = CreateSpriteChild(root, "HoverOutline", circleSprite);
            var hoverRenderer = hoverOutline.GetComponent<SpriteRenderer>();
            hoverRenderer.sortingLayerName = "OnTiles";
            hoverRenderer.sortingOrder = -5;
            hoverRenderer.color = new Color(1f, 1f, 1f, 0.8f);

            // --- Figlio: SelectionCircle (cerchio di selezione a terra) ---
            // Sorting sotto MainSprite per apparire "sotto" l'edificio
            var selectionCircle = CreateSpriteChild(root, "SelectionCircle", circleSprite);
            var selectionRenderer = selectionCircle.GetComponent<SpriteRenderer>();
            selectionRenderer.sortingLayerName = "OnTiles";
            selectionRenderer.sortingOrder = -20;
            selectionRenderer.color = new Color(0.6f, 1f, 0.6f, 0.9f);

            // --- Figlio: WorldUI/HealthBar (placeholder UI mondo) ---
            var worldUI = CreateChild(root, "WorldUI");
            var healthBar = CreateChild(worldUI, "HealthBar");

            return root;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.layer = parent.layer;
            return child;
        }

        private static GameObject CreateSpriteChild(GameObject parent, string name, Sprite sprite)
        {
            var child = CreateChild(parent, name);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return child;
        }

        #endregion

        #region Helpers

        private static Sprite LoadCircleSprite()
        {
            if (!File.Exists(CircleSpritePath))
            {
                Debug.LogWarning(
                    $"[BuildingBasePrefabBuilder] Sprite non trovata: {CircleSpritePath}. " +
                    "I figli HoverOutline/SelectionCircle avranno sprite null da assegnare a mano.");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }
}
#endif
