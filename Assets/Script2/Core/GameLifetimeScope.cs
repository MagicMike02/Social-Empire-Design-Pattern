﻿using UnityEngine;
using VContainer;
using VContainer.Unity;
using Script2.BuildingSystem;
using Script2.CameraSystem;
using Script2.Common;
using Script2.Economy;
using Script2.GridSystem;
using Script2.ResourceSystem;

namespace Script2.Core
{
    /// <summary>
    /// VContainer Lifetime Scope per Social Empire.
    /// BEST PRACTICE: Dependency Injection container che gestisce tutte le dipendenze.
    /// 
    /// Setup:
    /// 1. Crea GameObject in scena: "[DI] GameLifetimeScope"
    /// 2. Aggiungi questo componente
    /// 3. Tutti i sistemi saranno iniettati automaticamente
    /// 
    /// Vantaggi:
    /// - No Singleton pattern
    /// - No initialization order problems
    /// - Testabile (dependency injection)
    /// - Chiare dipendenze nel codice
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ========================================
            // CORE SYSTEMS (Single Instance)
            // ========================================
            
            // Economy System
            builder.RegisterComponentInHierarchy<GameEconomyManager>()
                .AsSelf()
                .AsImplementedInterfaces();
            
            // Grid System
            builder.RegisterComponentInHierarchy<GridManager>()
                .As<IGridService>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<TileManager>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<ZoneManager>()
                .AsSelf();
            
            // Resource System
            builder.RegisterComponentInHierarchy<ResourceManager>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<ResourceSpawner>()
                .AsSelf();
            
            // Building System
            builder.RegisterComponentInHierarchy<BuildingManager>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<BuildingFactory>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<BuildingPlacer>()
                .AsSelf();
            
            builder.RegisterComponentInHierarchy<KeyboardPlacementInput>()
                .AsSelf();
            
            // Event Bus (sostituisce BuildingEvents static)
            builder.RegisterComponentInHierarchy<BuildingEventBus>()
                .AsSelf();
            
            // Preview System (MonoBehaviour registrato nella scena)
            builder.RegisterComponentInHierarchy<GenericPreviewSystem>()
                .AsSelf();
            
            // Camera (Main Camera)
            builder.RegisterInstance(Camera.main);
            
            // Camera Controller (se esiste)
            builder.RegisterComponentInHierarchy<CameraController>()
                .AsSelf();
            
            Debug.Log("[GameLifetimeScope] Dependency Injection configurato con successo!");
        }
    }
}
