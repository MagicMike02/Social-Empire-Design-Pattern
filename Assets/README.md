# RTS 2.5D - Social Empire Design Pattern

## ARCHITETTURA SCOPERTA

```text
Input System (InputSystem_Actions.inputactions)
  -> InputManager (hover/click su collider, layer priority)
      -> IHoverable (Tile, ResourceInstance, PurchaseSign, ecc.)
      -> PlacementInputHandler (OnTileClicked, OnMapRightClicked)
          -> BuildingPlacer (FSM: Idle/Previewing/Confirming)
              -> PlaceBuildingCommand/DestroyBuildingCommand
                  -> BuildingFactory + PrefabPoolManager
                  -> GridManager (occupazione celle)
                  -> GameEconomyManager (spesa/rimborso)
                      -> GlobalEventBus (Resource/Building/Grid/Zone events)

ResourceSpawner -> ResourceManager -> GridManager + Economy + EventBus
PathfindingManager (A* + Cache Decorator) <- IGridService (GridManager)
CameraController usa Input Action map "Camera" (pan/zoom)
UIManager + ResourceDisplayUI sottoscritti a EventBus
```

**Sistemi ESISTENTI trovati:**
- ✅ Dependency Injection con VContainer (`GameLifetimeScope`)
- ✅ Input centrale con Unity Input System (`InputManager`)
- ✅ Building placement con FSM (`BuildingPlacer` + stati)
- ✅ Command pattern con Undo/Redo (`CommandHistory`, `CommandInputHandler`)
- ✅ Grid system + zone unlock + purchase sign (`GridManager`, `ZoneManager`, `TileManager`)
- ✅ Economy system (`GameEconomyManager`)
- ✅ Resource system con pool dedicato (`ResourceManager`, `ResourceSpawner`, `ResourcePoolManager`)
- ✅ Pathfinding A* con cache (`PathfindingManager`, `CachedPathfindingDecorator`)
- ✅ Event bus globale type-safe (`GlobalEventBus`)
- ✅ Camera drag/zoom con bounds (`CameraController`)
- ✅ Pooling generico prefab (`PrefabPoolManager`)
- [ ] Selezione unita (single/multi-select) RTS completa
- [ ] Drag-box selection collegata ad azione `DragSelect`
- [ ] Unit controller/movement command su right-click
- [ ] Combat/attack orders e command queue per gruppi unita
- [ ] Control groups RTS

**Struttura Single Responsibility:**
| Sistema | Script Principali | Responsabilita |
|---------|-------------------|----------------|
| Core/DI | `GameLifetimeScope` | Composition root, registrazione dipendenze singleton/component |
| Input | `InputManager`, `IHoverable` | Hover/click world-space, dispatch eventi input mondo |
| Building | `BuildingPlacer`, `BuildingManager`, `BuildingFactory`, stati/commands | Placement edifici, validazione, esecuzione command undo/redo |
| Grid | `GridManager`, `TileManager`, `ZoneManager`, `Tile` | Coordinate iso grid, occupazione celle, preview validita, sblocco zone |
| Economy | `GameEconomyManager` | Stato risorse giocatore, affordability, spend/add con eventi |
| Resources | `ResourceManager`, `ResourceSpawner`, `ResourcePoolManager`, `ResourceInstance` | Spawn/raccolta/rigenerazione risorse e occupazione celle |
| Pathfinding | `PathfindingManager`, `AStarAlgorithm`, `CachedPathfindingDecorator` | Path query su griglia con invalidazione cache su cambi occupancy |
| Camera | `CameraController` | Pan drag, zoom scroll, clamp bounds |
| UI | `UIManager`, `ResourceDisplayUI` | Facade UI e aggiornamento display risorse |
| Core Events | `GlobalEventBus`, `GameEvents` | Comunicazione disaccoppiata inter-sistemi |
| Core Commands | `CommandHistory`, `CommandInputHandler` | Pipeline comandi gameplay + undo/redo |
| Optimization | `GridCullingManager` | Culling/ottimizzazioni runtime griglia |

## ROADMAP
COMPLETATO: 2026-04-13 Migrazione Input System

IN CORSO: 2026-04-14 M1 Unita - sistema robusto, generico e disaccoppiato per movimento su tile via pathfinding + command

PROSSIMO:
Week 1: 🔴 M1 Unita - dominio unita runtime, selezione robusta, right-click move command, mini pannello conteggio selezione
Week 2: 🔴 M2 Command Layer - command queue (Shift), control groups, stop/hold, policy priorita comandi
Week 3: 🔴 Baseline RTS Building - coda costruzione robusta, stato costruzione/upgrading, cancellazione/refund, validazioni placement complete
Week 4: 🟡 Hardening performance - cache costi building preview, pooling completo place/undo/destroy, ottimizzazioni input/pathfinding per 35 unita (configurabile)
Week 5: 🟡 UX/controlli - smoothing camera, WASD opzionale, feedback visivi selezione/comando, stabilizzazione bugfix

Nota decisione 2026-04-14: command queue spostata a M2 per mantenere M1 focalizzata su architettura unita robusta e disaccoppiata.

ATTIVITA DA FARE (sintesi operativa):
1. Completare adozione ScriptableObject in stile Social Empire (`UnitConfigSO`, `UnitCatalogSO`, `UnitSystemConfigSO`) su tutti i componenti UnitSystem.
2. Completare wiring servizi produzione unita (`UnitUnlockService`, `UnitSpawnService`, `UnitProductionQueueService`, `UnitRewardGrantService`).
3. Collegare `DragSelect`, `AdditiveSelect`, `CommandModifier` al flusso runtime.
4. Implementare `UnitMoveCommand` con pipeline right-click contestuale (M1).
5. Implementare mini pannello UI con conteggio unita selezionate (M1).
6. Introdurre command queue su Shift, stop/hold e control groups (M2).
7. Introdurre `BuildingQueueService` per costruzione/upgrading/cancel con stati espliciti (M3).
8. Eliminare `Object.Destroy` dai command edificio e usare rilascio su pool.
9. Ridurre allocazioni nel placement (`ToDictionary`) con cache precomputata.

Decisione input 2026-04-14: in modalita placement edificio, gli input unita vengono sospesi (priorita assoluta al building mode).

## TRACKING SVILUPPO (%)

Snapshot aggiornato al 2026-04-14.

| Area | Stato | Avanzamento | Cosa manca | Migliorabile |
|------|-------|-------------|------------|--------------|
| M1 Unita - dominio/runtime | In corso | 82% | Setup scena definitivo e validazione runtime completa | Ridurre fallback runtime e consolidare bootstrap DI |
| M1 Unita - selezione | In corso | 62% | QA su edge case drag/deselect e layering collider | Migliorare feedback visivo selezione (shape/outline) |
| M1 Unita - movimento pathfinding command | In corso | 70% | Stress test collisione/crowding su gruppi multipli | Migliorare formazione slot e retry policy su celle occupate |
| M1 UI conteggio selezione | In corso | 50% | Collegamento definitivo a Canvas/TMP in scena | Aggiungere stato vuoto/non selezionato e stile coerente HUD |
| Configurazione via ScriptableObject | In corso | 78% | Creazione asset catalogo/ruoli/livelli e assegnazione in inspector | Validazione fallback default quando SO non assegnato |
| Produzione e spawn unita (Building/Reward) | In corso | 65% | Wiring in scena e test end-to-end enqueue->spawn/reward->spawn | Centralizzare eventi dominio e telemetria pipeline |
| M2 Command Layer | Non iniziato | 0% | Command queue, control groups, stop/hold | Definire policy interrupt/accodamento prima dell'implementazione |
| M3 Building robusto | Non iniziato | 0% | Queue build/upgrade, stati building, refund policy centralizzata | Introdurre test regressione costi e stati building |
| Performance hardening | Non iniziato | 0% | Benchmark stabile 35 unita configurabile | Telemetria frame-time per action critiche |

Avanzamento complessivo progetto (roadmap corrente): 49%

Modello configurazione unita (Social Empire-style):
1. `UnitConfigSO`: identita, ruolo, costo/tempo produzione, progressione per livello.
2. `UnitCatalogSO`: catalogo centralizzato per lookup unita per id/ruolo.
3. `UnitSystemConfigSO`: config globale (cap, selection/command tuning, default unit + livello).

Regola operativa di aggiornamento:
1. Aggiornare questa tabella a fine ogni sessione di sviluppo.
2. Aggiornare percentuali solo su feature verificata in scena.
3. Annotare sempre una voce "Cosa manca" e una "Migliorabile" per area attiva.

Registro dettagliato sessioni: `Script/Documentation/RTS_Progress_Log.md`

## CONTROLLI ATTUALI
Input reale trovato in `InputSystem_Actions.inputactions` e negli script che consumano le action reference:

- Mappa `Gameplay`
- `Point`: posizione mouse (usata da `InputManager`)
- `LeftClick`: click sinistro world (usata da `InputManager`)
- `RightClick`: click destro world (usata da `InputManager`)
- `Confirm`: Enter
- `Cancel`: Escape (usata in `PlacementInputHandler`)
- `SelectBuilding1..9`: tasti 1..9 (usata in `PlacementInputHandler`)
- `Undo`: Ctrl+Z (usata in `CommandInputHandler`)
- `Redo`: Ctrl+Shift+Z e Ctrl+Y (usata in `CommandInputHandler`)
- `DragSelect`, `AdditiveSelect`, `CommandModifier`: definite ma non ancora collegate in codice gameplay

- Mappa `Camera`
- `Point`: posizione mouse (usata da `CameraController`)
- `Pan`: hold mouse left button (drag pan)
- `Zoom`: mouse scroll Y

- Mappa `Debug`
- `FindPath`: tasto F (usata da `PathfindingTester`)
- `SimulateGridChange`: tasto P (definita, non trovata wiring attivo)

- Mappa `UI`
- Navigate/Submit/Cancel/Point/Click/RightClick presenti per integrazione UI Input System

## SETUP SVILUPPO
Setup reale rilevato da `Packages/manifest.json` e `ProjectSettings/ProjectSettings.asset`:

- Unity project name: `Social Empire Design Pattern`
- Rendering: URP (`com.unity.render-pipelines.universal` 17.3.0)
- Input: New Input System (`com.unity.inputsystem` 1.18.0)
- DI Container: `jp.hadashikick.vcontainer` 1.15.4 (OpenUPM scoped registry)
- UI: `com.unity.ugui` 2.0.0
- Test framework: `com.unity.test-framework` 1.6.0

Avvio consigliato per team:
1. Aprire il progetto root Unity in Unity 6.
2. Verificare Package Manager con i pacchetti sopra installati.
3. Aprire la scena runtime principale: `Assets/Scenes/SceneScript2.unity`.
4. Controllare in scena che `GameLifetimeScope` sia presente e senza errori DI.
5. Avviare Play Mode e validare input base: placement edificio, undo/redo, pan/zoom camera, raccolta risorse.

## PROBLEMI CRITICI 🔴
1. Mancano i meccanismi RTS core di selezione unita (single/multi/drag-box): le action esistono ma non sono cablate.
2. Manca la catena comando unita su right-click: esiste `OnMapRightClicked`, ma non c'e un `UnitMoveCommand`/`UnitController`.
3. Hot-path alloc durante placement preview: `BuildingPlacer` usa `config.ToDictionary()` in `CanPlaceBuilding` durante aggiornamenti frequenti della preview.
4. Undo/Destroy edificio usa `Object.Destroy` in `PlaceBuildingCommand` e `DestroyBuildingCommand`, creando churn memoria invece di release sul pool.
5. `ResourceManager` concentra troppe responsabilita (spawn/collection/regen/economy/occupancy), rischio colli di bottiglia manutentivi su scala 35 unita (configurabile).

### GAP DI ROBUSTEZZA RTS (BUILDING / UNITA)

**Building System - non ancora robusto per RTS completo:**
1. Assente una pipeline di produzione robusta (queue build, queue upgrade, cancel task, priorita lavori).
2. Mancano stati gameplay completi dell'edificio (`Planned`, `UnderConstruction`, `Active`, `Disabled`, `Destroyed`).
3. Mancano policy centralizzate di refund/costo su cancel/destroy/upgrade (attualmente logica dispersa nei command).
4. Mancano workflow di massa (multi-place, snapping avanzato, validazioni area estese per scenari ad alta densita).

**Unit System - non ancora robusto per RTS completo:**
1. Assente dominio unita runtime (nessun `UnitController`/`UnitAgent` dedicato).
2. Assente command layer completo (move/stop/attack/gather/patrol/hold position).
3. Assente gestione gruppi (control groups 1..9) e command queue (Shift).
4. Assente separazione chiara tra selection, command issuing e locomotion execution.
5. Assenti test di carico specifici su 35 unita (configurabile) con comandi simultanei.

## METRICHE PROGETTO
LOC: 7014 (solo `.cs` in Assets)
Scene: 2 (`Assets/Scenes/SceneScript2.unity`, `Assets/Settings/Scenes/URP2DSceneTemplate.unity`)
Prefabs: 20
Target unita runtime: 35 (configurabile)

---
*Aggiornato: 2026-04-14 da Copilot*