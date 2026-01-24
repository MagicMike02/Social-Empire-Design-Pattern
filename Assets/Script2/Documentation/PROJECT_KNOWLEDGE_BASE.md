# PROJECT KNOWLEDGE BASE: Social Empire
## 2.5D Isometric RTS/Citybuilder (Unity 6 C#)

**Purpose**: Technical architecture reference for autonomous feature implementation.  
**Last Sync**: 2026-01-25  
**Status**: Phase 2 Complete (InputManager live, Pathfinding next)

---

## 1. SYSTEM ARCHITECTURE

**Core Stack**: Component-Based (MonoBehaviour) + VContainer DI (NO Singletons in Script2) + Event-Driven

**Initialization Chain** (GameLifetimeScope.cs):
1. GameEconomyManager (resource state)
2. TileManager → GridManager (IGridService) → Graph structure
3. ZoneManager (state machine)
4. ResourceSpawner → ResourceManager → ResourcePoolManager
5. BuildingFactory → BuildingManager → BuildingPlacer → GenericPreviewSystem
6. InputManager (centralized raycasting)
7. Camera.main (singleton, injected)

**Dependency Injection**: All managers registered in GameLifetimeScope.Configure(). Use `[Inject]` attribute on fields.

**Event Pattern**: BuildingEventBus (MonoBehaviour) dispatches OnBuildingPlaced/OnBuildingDestroyed. Subscribe in Awake(), unsubscribe in OnDestroy().

**Design Patterns**:
- **Factory**: BuildingFactory.CreateBuilding() → BuildingManager placement
- **Strategy**: IResourceGenerationStrategy (4 implementations for spawning)
- **Object Pooling**: ResourcePoolManager caches resource instances
- **Service Locator**: IGridService abstraction → GridManager implementation
- **Data-Driven**: BuildingConfigSO, ResourceDataSO define game content
- **Observer**: BuildingEventBus, ZoneManager.OnZoneUnlocked

---

## 2. CUSTOM ISOMETRIC ENGINE

**No Native Tilemap** - All coordinate math is custom via Matrix4x4.

### Coordinate System

**Grid Space**: (x, y) integer coordinates → 0-50 or 0-100 depending on map size.  
**World Space**: Continuous Vector3 position in 3D scene.

**Transformation**:

```csharp
// Grid → World (see Grid.cs)
Vector3 worldPos = matrix.MultiplyPoint3x4(gridPos * cellSize) + originPosition;

// World → Grid (see Grid.cs)
Vector3 localPos = worldPos - originPosition;
Vector3 gridPos = matrix.inverse.MultiplyPoint3x4(localPos) / cellSize;
int x = Mathf.FloorToInt(gridPos.x), y = Mathf.FloorToInt(gridPos.y);
```

**Matrix4x4 Setup** (Grid.cs → InitMatrix4x4()):
- Column 0 (X-axis): (1, 0.5, 0, 0) → isometric tilt
- Column 1 (Y-axis): (-1, 0.5, 0, 0) → isometric tilt
- Isometric angle: ~26.6° (arctan(0.5))
- Origin: (0, 0, 1) in world space, configurable cellSize

### Depth Sorting

**Strategy**: Y-position determines render order (higher Y = rendered on top).

```csharp
// Tile.SetSortingOrder(), Building sorting, Unit sorting
sortingOrder = -(int)(transform.position.y * 100);
```

**Integration**: Updated every frame during movement/placement.

### Key Methods

- `Grid.GetIsoToWorldPosition(x, y)` → Vector3 world position
- `Grid.GetWorldToIsoPosition(worldPos, out x, out y)` → grid cell
- `IGridService.TryWorldToCell(worldPos, out Vector3Int cell)` → used by InputManager for click detection
- `GridManager.CellToWorld(Vector3Int cell)` → for unit movement targets

---

## 3. INPUT SYSTEM (PHASE 1-2 COMPLETE)

### InputManager Architecture

**File**: InputSystem/InputManager.cs  
**Pattern**: Centralized single-raycast-per-frame dispatch  
**Performance**: 0.1ms/frame (220x faster than OnMouse*)

**Logic**:
1. Single Physics2D.RaycastNonAlloc() per Update (skip if mouse stationary)
2. Priority filtering: Unit(10) > Building(9) > Resource(11) > ZoneSign(12) > Tile(8)
3. Dispatch to IHoverable interface (OnHoverEnter/OnHoverExit/OnClick/OnRightClick)
4. Non-allocating (buffer reused, no new[] in loop)

**Layer Priority Order** (see LayerRegistry.cs):
```
Raycast hits → Filter by LayerID → GetComponent<IHoverable>() → Dispatch
```

### IHoverable Interface

**File**: InputSystem/IHoverable.cs  
**Contract**: 4 methods required for any interactive object.

**Implementations**:
- **Tile.cs** → OnHoverEnter (yellow color), OnHoverExit (reset state color), OnClick (debug log), OnRightClick (future)
- **ResourceInstance.cs** → OnClick (calls CollectResource → ResourceManager.HandleResourceCollected), OnHoverEnter/Exit (no-op)
- **PurchaseSign.cs** → OnHoverEnter (set _hovered = true for scale animation), OnHoverExit (set _hovered = false), OnClick (calls PurchaseZone logic)

### LayerRegistry

**File**: Core/LayerRegistry.cs  
**Purpose**: Static centralized layer ID mapping.  
**Usage**: InputManager filters by LayerRegistry.Tile, LayerRegistry.Resource, etc. (no hardcoded layer numbers).

---

## 4. BUILDING SYSTEM

### Placement Flow

1. **KeyboardPlacementInput.cs** → Detects key press (e.g., "1"), calls BuildingPlacer.StartPlacement(config)
2. **BuildingPlacer.cs** → Updates preview via GenericPreviewSystem as mouse moves
3. **BuildingPlacer.CanPlaceBuilding()** → Validates:
   - All cells in footprint are TileState.Unlocked
   - All cells free (not in _occupiedCells)
   - GameEconomyManager.CanAfford(cost)
4. **GenericPreviewSystem.cs** → Renders 2D preview (red if invalid, green if valid)
5. **On Confirm (Enter key)** → BuildingManager.PlaceBuilding() → Factory creates instance → occupies cells → fires BuildingEventBus.OnBuildingPlaced

### Key Classes

- **BuildingConfigSO**: Data asset (type, footprint, cost, prefab, visual)
- **Building.cs**: Runtime instance, minimal logic (stores BuildingConfigSO reference)
- **BuildingManager.cs**: Orchestrates placement, validation, event dispatch
- **BuildingPlacer.cs**: Handles preview updates, validation logic
- **BuildingFactory.cs**: Instantiates Building prefab, initializes
- **BuildingEventBus.cs**: Observer pattern, fires OnBuildingPlaced/OnBuildingDestroyed

### Cell Occupation

**GridManager._occupiedCells**: HashSet<Vector2Int> tracks all occupied cells.  
Updated on placement (add) and destruction (remove).  
Checked by BuildingPlacer.CanPlaceBuilding() and ResourceManager (to avoid resource overlap).

---

## 5. RESOURCE SYSTEM

### Generation Strategies

**IResourceGenerationStrategy** (4 implementations):
1. **Scattered**: Random across map
2. **Clustered**: Groups of resources
3. **Linear**: Strips or lines
4. **Single**: One resource per zone

Selected per resource type in ResourceDataSO.

### Collection Flow

1. Mouse click on ResourceInstance (layer 11)
2. InputManager raycast detects, dispatches ResourceInstance.OnClick()
3. ResourceInstance.OnClick() → CollectResource() → ResourceManager.HandleResourceCollected(gridPos, data)
4. ResourceManager → GameEconomyManager.AddResource(type, amount) → fires event
5. Grid cell marked as free, resource despawned/pooled

### Pooling

**ResourcePoolManager.cs**: Object pool per resource type to avoid GC allocations.  
ResourceInstance.Awake() auto-registers with pool.

---

## 6. ZONE SYSTEM

### Zone States

- **Locked**: Player cannot build (gray visual)
- **Buyable**: Player can purchase unlock via PurchaseSign (has OnClick logic)
- **Unlocked**: Player can build freely

### Purchase Flow

1. PurchaseSign visible on Buyable zone (layer 12)
2. InputManager click dispatch → PurchaseSign.OnClick()
3. Check GameEconomyManager.CanAfford(cost)
4. ZoneManager.PurchaseZone(zoneCoord) → Updates all tile states in zone to Unlocked
5. BuildingEventBus or custom event fires for UI feedback

**Event Hook**: ZoneManager.OnZoneUnlocked (used by UI, etc.)

---

## 7. ECONOMY SYSTEM

### GameEconomyManager

**Pattern**: Registered via VContainer (not singleton, but effectively one instance per game).  
**State**: Dictionary<ResourceType, int> tracks current quantities.

**Key Methods**:
- AddResource(type, amount)
- RemoveResource(type, amount)
- CanAfford(Dictionary<ResourceType, int> costs)

**Used By**:
- BuildingPlacer (validate cost before placement)
- PurchaseSign (validate zone purchase cost)
- ResourceManager (add collected resources)
- (Future) Building production, unit training

---

## 8. GRID & TILE STRUCTURE

### GridManager (IGridService)

**Responsibilities**:
- Grid.cs instance (2D array of Tile)
- Cell validation (walkable, occupied, free)
- Coordinate conversions (world ↔ cell)
- Preview management (ClearPreviousPreview, SetCellsPreview)

**Key Methods**:
- TryWorldToCell(worldPos, out cell) → used by InputManager
- AreCellsFree(origin, width, height) → used by BuildingPlacer
- CellToWorld(cell) → used by units/buildings for positioning
- GetTileAt(cell) → debug/testing

### Tile.cs

**Implements**: IHoverable  
**State**: TileState (Locked/Buyable/Unlocked)  
**Visual**: Color changes per state (gray/greenish/white)  
**Preview Logic**: OnHoverEnter skips if _isShowingPreview = true (prevents yellow overlay during placement)

### ZoneManager

**Tracks**: Zone → Tile list mapping  
**On Zone Unlock**: Calls Tile.SetState(TileState.Unlocked) for all cells in zone  
**Event**: OnZoneUnlocked fires (optional UI listeners)

---

## 9. DATA LAYER (SCRIPTABLEOBJECTS)

### BuildingConfigSO

**Fields**: Type, Name, Footprint (width/height), Cost dict, Prefab, Sorting order  
**Usage**: Loaded at game start, passed to BuildingFactory/BuildingPlacer  
**Lifecycle**: Immutable at runtime

### ResourceDataSO

**Fields**: Type, Amount, Rarity, Generation strategy  
**Usage**: ResourceSpawner reads to spawn, ResourceInstance stores reference  
**Lifecycle**: Immutable at runtime

### (Future) UnitConfigSO

**Will contain**: Speed, health, attack, prefab, etc.

---

## 10. ARCHITECTURE DECISIONS & IMPLICATIONS

### Why No Native Tilemap?

Custom isometric math allows fine-grained control over rendering, collision, and coordinate space. Native Tilemap adds unnecessary overhead for this use case.

### Why VContainer Instead of Singletons?

DI improves testability, decouples managers, makes lifecycle explicit. GameLifetimeScope owns all service instances.

### Why Separate Placer from Manager?

BuildingPlacer handles preview/validation logic (stateless per placement session).  
BuildingManager handles persistence, events, factory calls.  
Cleaner SRP, easier to test.

### Why IGridService?

Abstracts grid logic from placement/input. Enables pathfinding to use same interface (future).

### Why IHoverable Instead of OnMouse*?

OnMouse* doesn't scale beyond 50×50 grids (O(n) raycasts per frame).  
InputManager's single raycast + interface dispatch is O(1) and non-allocating.

---

## 11. PERFORMANCE PROFILE

| System | Metric | Target | Status |
|--------|--------|--------|--------|
| Input | 1 raycast/frame | < 0.5ms | ✅ 0.1ms |
| Building Placement | Validation + preview | < 1ms | ✅ 0.2ms |
| Pathfinding | A* on 100×100 (async) | < 5ms | 🔄 To implement |
| Frame Time | 60 FPS | 16.6ms | ✅ Met |

---

## 12. CURRENT INTEGRATION POINTS FOR NEW FEATURES

### Pathfinding (Next Sprint)

**Needs**: IGridService.IsCellWalkable(cell) → extend GridManager  
**Output**: List<Vector2Int> path (A* algorithm)  
**Consumer**: Future Unit.cs → movement along path  
**Layer**: Units on layer 10

### Unit System (Sprint 2)

**Needs**: UnitConfigSO (data), Unit.cs (script, IHoverable), UnitSelectionManager (input dispatch)  
**Integration**: Right-click on cell via InputManager → pathfinding → unit movement  
**Layer**: Layer 10, sorted by Y-position

### Drag-Box Selection (Sprint 3)

**Needs**: Extend InputManager to detect drag (mouse delta > threshold)  
**Output**: Physics2D.OverlapArea(box bounds) → select units in area  
**Constraint**: Y-sorting updates per frame during selection preview

### Building Production (Sprint 4)

**Needs**: ProductionBuilding.cs component (timer-based resource generation)  
**Hook**: Subscribe to BuildingEventBus.OnBuildingPlaced, attach component if building has production config  
**Output**: GameEconomyManager.AddResource() on cycle completion

---

## 13. FILE STRUCTURE REFERENCE

```
Assets/Script2/
├── Core/
│   ├── GameLifetimeScope.cs (VContainer setup)
│   └── LayerRegistry.cs (layer ID constants)
├── GridSystem/
│   ├── Grid.cs (Matrix4x4 math)
│   ├── GridManager.cs (IGridService impl)
│   ├── Tile.cs (IHoverable)
│   ├── TileManager.cs
│   ├── ZoneManager.cs
│   └── PurchaseSign.cs (IHoverable)
├── BuildingSystem/
│   ├── BuildingManager.cs
│   ├── BuildingPlacer.cs
│   ├── Building.cs
│   ├── BuildingFactory.cs
│   ├── BuildingConfigSO.cs
│   ├── BuildingEventBus.cs
│   └── KeyboardPlacementInput.cs
├── ResourceSystem/
│   ├── ResourceManager.cs
│   ├── ResourceInstance.cs (IHoverable)
│   ├── ResourceSpawner.cs
│   ├── ResourcePoolManager.cs
│   ├── ResourceDataSO.cs
│   └── IResourceGenerationStrategy.cs
├── EconomySystem/
│   └── GameEconomyManager.cs
├── InputSystem/
│   ├── InputManager.cs
│   └── IHoverable.cs
├── Common/
│   └── GenericPreviewSystem.cs
└── CameraSystem/
    └── CameraController.cs
```

---

## 14. DEBUGGING CHECKLIST

- **Click not detected**: Check LayerRegistry values match actual layer assignment in Editor
- **Preview not showing**: Verify GenericPreviewSystem.SetCellsPreview() called by BuildingPlacer
- **Resource overlap buildings**: Check ResourceManager filters _occupiedCells before spawn
- **Zone not unlocking**: Check ZoneManager.PurchaseZone() called, GameEconomyManager cost deduction works
- **Sorting wrong**: Verify sortingOrder = -(int)(y * 100) recalculated on every position change
- **DI injection null**: Check GameLifetimeScope.Configure() registers component, Inspector sees it in DI Container

---

**END OF KNOWLEDGE BASE**

*This file is your long-term technical memory. Update existing sections, never create new versions. Reference freely when implementing new systems.*
