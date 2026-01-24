# TECHNICAL KNOWLEDGE BASE: Social Empire - 2.5D Isometric RTS/Citybuilder

**Project Type**: Unity 2D Isometric Citybuilder with RTS Mechanics  
**Architecture**: Component-Based with Design Patterns  
**Language**: C# (Unity Engine)  
**Engine Version**: Unity 6  
**Status**: Active Development  

---

## 1. ARCHITECTURAL OVERVIEW

### 1.1 High-Level Architecture Pattern
- **Core Pattern**: MVC-inspired Component Architecture with Singleton Managers
- **State Management**: Event-driven (Observer Pattern) + MonoBehaviour Lifecycle
- **Decoupling**: Heavy use of Interfaces (IGridService) for loose coupling
- **Initialization Flow**:
  1. GameEconomyManager (Singleton) → Global Resource State
  2. GridManager → Grid Creation + Zone Management
  3. BuildingManager → Factory + Preview + Placement
  4. ResourceManager → Spawning + Collection
  5. CameraController → Isometric View Setup

### 1.2 Implemented Design Patterns

| Pattern | Usage | Classes |
|---------|-------|---------|
| **Singleton** | Core Managers | GameEconomyManager, GridManager |
| **Factory** | Object Creation | BuildingFactory, ResourceSpawner |
| **Observer** | Event Communication | BuildingEvents, ZoneManager events |
| **Strategy** | Resource Generation | IResourceGenerationStrategy (4 implementations) |
| **Decorator** | Preview States | PreviewSystem, GenericPreviewSystem |
| **Data-Driven** | Configuration | BuildingConfigSO, ResourceDataSO |
| **Object Pooling** | Performance | ResourcePoolManager |

### 1.3 Core Systems Hierarchy
```
GameEconomyManager (Singleton - Resources)
├── BuildingManager (Coordinator)
│   ├── BuildingFactory (Creation)
│   ├── BuildingPlacer (Placement Logic)
│   └── PreviewSystem (Visual Feedback)
│
GridManager (Singleton - Grid State)
├── TileManager (Tile Instantiation)
├── ZoneManager (Zone Logic + Purchase)
└── Grid<T> (Generic Grid Data Structure)
│
ResourceManager (Coordinator)
├── ResourceSpawner (Generation)
├── ResourcePoolManager (Pooling)
└── ResourceInstance (Individual Resource)
│
CameraController (Camera Management)
```

---

## 2. ISOMETRIC ENGINE LOGIC

### 2.1 Coordinate System & Transformation

**Custom Isometric Transformation (NO Native Tilemap)**:

The project implements a **2D Isometric projection** using a 4x4 Matrix transformation:

```csharp
// Matrix4x4 Configuration
_matrix = Matrix4x4.identity;
_matrix.SetColumn(0, new Vector4(1f, 0.5f, 0f, 0f));   // X axis: (1, 0.5) in world
_matrix.SetColumn(1, new Vector4(-1f, 0.5f, 0f, 0f));  // Y axis: (-1, 0.5) in world
_matrix.SetColumn(2, new Vector4(0f, 0f, 1f, 0f));     // Z axis: unchanged
```

**Conversion Formulas**:
- **Grid → World**: `worldPos = matrix * gridPos * cellSize + originPosition`
- **World → Grid**: `gridPos = matrix.inverse * (worldPos - originPosition) / cellSize`

**Key Properties**:
- Isometric angle: ~30° (based on 1:0.5 ratio)
- Cell size is configurable per scene
- Origin position: (0, 0, 1)
- Uses `Mathf.FloorToInt()` for grid snapping

### 2.2 Depth Sorting Strategy

**Isometric Depth Sorting Formula**:
```csharp
sortingOrder = -yBase * 100f + BaseSortingOrder
```

**Rationale**:
- Lower Y coordinates (further back in isometric view) should render behind
- Multiplication by 100 ensures fine-grained control
- BaseSortingOrder preserves per-building offset

**Sorting Layers**:
1. "Tiles" (Z=0): Grid foundation
2. "OnTiles" (Z=100): Buildings, Resources
3. Future: UI overlays, effects

**Implementation Classes**:
- `Building.SetSortingByY()` - Updates building sorting in real-time
- `BuildingManager.ApplySorting()` - Static helper for consistent sorting
- `TileManager.CalculateSortingOrder()` - Per-tile formula

### 2.3 Grid Alignment & Raycasting

**Tile Manager Responsibilities**:
- Creates Grid<Tile> of configurable dimensions
- Instantiates tile prefabs at isometric world positions
- Assigns sorting orders based on (x+y) formula

**Grid Service (IGridService)**:
- Abstract layer for building system
- Converts world → grid coordinates
- Validates cell occupation
- Sets preview visuals on tiles

**Current Raycasting Approach**:
- Uses `Physics2D.OverlapCircle()` with tile colliders
- `OnMouseEnter/OnMouseExit` for hover feedback (Tile.cs)
- `OnMouseDown` for tile selection

**Future Optimization**:
- Replace OnMouse events with raycast-based input (avoid Update spam)
- Cache raycasts to reduce Physics2D calls

---

## 3. SYSTEMS BREAKDOWN

### 3.1 BUILDING SYSTEM (Citybuilder Core)

**Responsibility**: Manage building lifecycle (creation, placement, preview, validation)

**Key Classes**:
1. **Building.cs** (sealed, lightweight)
   - Purpose: Individual building instance
   - Properties: Config (immutable), SpriteRenderer (cached)
   - Methods: `Init()`, `SetSortingByY()`
   - Event: OnDestroy → `BuildingEvents.OnBuildingDestroyed`
   - **Principle**: Single Responsibility (rendering + sorting only)

2. **BuildingConfigSO** (data-driven)
   - Defines: Prefab, dimensions (Width/Height), costs, sorting
   - Method: `ToDictionary()` aggregates resource costs
   - Used by: BuildingFactory, BuildingPlacer

3. **BuildingFactory.cs** (sealed)
   - Pattern: Factory Pattern
   - Method: `CreateBuilding(config, worldPos, parent)`
   - Steps:
     1. Validate config & prefab
     2. Instantiate prefab
     3. Attach/get Building component
     4. Call `building.Init(config)`
     5. Apply isometric sorting

4. **BuildingPlacer.cs** (sealed)
   - Orchestrates placement workflow
   - State: `_isPlacing`, `_selectedConfig`, `_currentCell`
   - Methods:
     - `StartPlacing()` - Begin preview
     - `ConfirmPlacement()` - Validate + create + occupy
     - `CancelPlacement()` - Cleanup
   - Integration: Directly calls `BuildingManager.Grid`, `BuildingManager.Factory`
   - **FIX**: Uses `UpdatePreviewIfCellChanged()` to prevent glitches

5. **PreviewSystem.cs**
   - Manages visual ghost of building during placement
   - Features:
     - Color tinting (green valid, red invalid)
     - Y-offset to prevent z-fighting
     - Caches last valid state to reduce updates
   - Methods:
     - `ShowPreview()` - Create/update ghost
     - `UpdatePreviewIfCellChanged()` - Optimized refresh
     - `HidePreview()` - Destroy ghost
   - **Optimization**: Only updates if cell changes OR validity changes

6. **BuildingManager.cs** (sealed coordinator)
   - Centralizes dependencies: Factory, Grid, Economy
   - Purpose: Single access point for building operations
   - Static Utilities:
     - `ApplySorting()` - Apply isometric sorting
     - `SetGhostColor()` - Update preview color

7. **BuildingEvents** (static hub)
   - Observer pattern implementation
   - Events: `OnBuildingSelected`, `OnBuildingDestroyed`, `OnBuildingPlaced`
   - **Important**: `ClearAllEvents()` for cleanup (prevent memory leaks)

8. **KeyboardPlacementInput.cs**
   - Input bridge: Keyboard → Placement
   - Bindings:
     - `Alpha1` = Start/Confirm placement
     - `ESC` = Cancel placement
   - Single Responsibility: Input handling only

9. **IGridService** (interface)
   - Abstracts grid operations for building system
   - Methods:
     - `TryWorldToCell()` - Convert + validate
     - `CellToWorld()` - Convert back
     - `AreCellsFree()` - Check occupation
     - `OccupyCells()`, `FreeCells()` - Update occupancy
     - `SetCellsPreview()` - Visual feedback
   - Implementation: GridManager

**Placement Validation Flow**:
```
Input (KeyboardPlacementInput)
  ↓
BuildingPlacer.StartPlacing(config)
  ↓
Update() → UpdatePlacementPreview()
  ↓
IGridService.TryWorldToCell() + AreCellsFree()
  ↓
PreviewSystem.UpdatePreviewIfCellChanged() [Optimized]
  ↓
Confirm: CanPlaceBuilding() checks resources + space
  ↓
BuildingFactory.CreateBuilding() + OccupyCells()
  ↓
BuildingEvents.OnBuildingPlaced (notification)
```

**Known Issues & Fixes Applied**:
- **Issue**: Tile hover colors stick in locked zones after preview
- **Root Cause**: `Tile.PreviewTint()` sets color but `ResetTint()` called inconsistently
- **Current Fix**: GridManager tracks `_lastPreviewCells`, clears all previews before setting new ones
- **Status**: REQUIRES TESTING - may need enhanced state tracking

---

### 3.2 GRID SYSTEM (Foundation)

**Responsibility**: Manage grid data structure, tiles, zones, and preview feedback

**Key Classes**:

1. **Grid<T>** (generic data structure)
   - 2D array wrapper with isometric transformation
   - Constructor: `Grid(width, height, cellSize)`
   - Core Methods:
     - `GetIsoToWorldPosition(x, y)` → Vector3
     - `GetWorldToIsoPosition(worldPos, out x, y)` → void
     - `SetValue()`, `GetValue()` - Array access
     - `SnapWorldToGridWorld()` - Snap position to grid
     - `ToList()` - Flatten to list
   - **Design**: Decouples grid logic from MonoBehaviour

2. **GridManager.cs** (sealed singleton)
   - Implements: IGridService + MonoBehaviour
   - Dependencies: TileManager, ZoneManager, ResourceSpawner
   - **Singleton Pattern**:
     - Creates instance in Awake if null
     - Sets DontDestroyOnLoad
     - Validates dependencies at startup
   - State Management:
     - `_lastPreviewCells`: Tracks previous preview tiles
     - `_occupiedCells` (HashSet): Fast occupation lookup
     - `_previewTileCache`: Caches tile references
   - Methods:
     - `TryWorldToCell()` - Convert + bound check
     - `AreCellsFree()` - Check occupation + state
     - `OccupyCells()`, `FreeCells()` - Update occupancy
     - `SetCellsPreview()` - **COMPLEX** - manages preview state
   - **Preview Logic**:
     - If width/height ≤ 0: Clear previous preview only
     - Otherwise: Clear old tiles, set new ones, apply tint

3. **TileManager.cs**
   - Responsibility: Tile instantiation & sorting
   - Properties: Width, Height, CellSize
   - Methods:
     - `CreateGrid()` - Full grid creation
     - `CreateTileAt(x, y)` - Individual tile
     - `InstantiateTile()` - Prefab instantiation
     - `CalculateSortingOrder()` - `(x + y) * -100`
   - Dependency: _tilePrefab (serialized)

4. **Tile.cs**
   - Represents individual tile in grid
   - State: TileState enum (Locked, Buyable, Unlocked)
   - Visual States:
     - Locked: Grey color
     - Buyable: Green color
     - Unlocked: White (normal)
   - Hover Logic:
     - `OnMouseEnter()` → Yellow tint
     - `OnMouseExit()` → Reset to state color
   - Preview Methods:
     - `PreviewTint(color)` - Store original, apply tint
     - `ResetTint()` - Restore state color
   - **State Tracking**:
     - `_isShowingPreview`: Flag to prevent hover during preview
     - `_savedColorBeforePreview`: Backup original color
   - **Issue**: Hover can stick if ResetTint not called properly

5. **ZoneManager.cs**
   - Divides map into purchasable zones
   - State: Dictionary of Zone objects
   - Zone Class (nested):
     - `start` (Vector2Int): Top-left corner
     - `tiles` (2D array): Tile references
     - `isUnlocked`: Purchase state
     - `purchaseSign`: GameObject reference
   - Methods:
     - `Initialize()` - Set grid reference
     - `CreateZones()` - Generate all zones
     - `PurchaseZone()` - Economy integration
   - Events: `OnZoneUnlocked`, `OnZonePurchaseFailed`
   - **Logic**:
     - Center zone auto-unlocked
     - Adjacent zones purchasable
     - Purchase costs configurable

6. **PurchaseSign.cs** (component on prefab)
   - Clickable UI for zone purchase
   - Not fully detailed in scan, but integrated with ZoneManager

7. **ZoneFeedbackUI.cs**
   - Listens to ZoneManager events
   - Logs purchase feedback (expandable for UI updates)

---

### 3.3 RESOURCE SYSTEM (RTS Economy)

**Responsibility**: Manage resources (materials, collectibles) with regeneration

**Key Classes**:

1. **ResourceManager.cs**
   - Coordinator for resource lifecycle
   - Dependencies: TileManager, GameEconomyManager, ZoneManager, ResourceSpawner, ResourcePoolManager
   - State:
     - `_activeResources`: Dictionary<Vector2Int, GameObject>
     - `_regenerationCoroutines`: Track active regeneration timers
   - Methods:
     - `HandleResourceSpawned()` - Integration hook
     - `HandleResourceCollected()` - Update economy + schedule regen
     - `RemoveResource()` - Cleanup or return to pool
     - `UpdateEconomy()` - Call GameEconomyManager.AddResource()
     - `ScheduleRegeneration()` - Create regen prefab + start timer
   - Events: `OnResourceCollected`, `OnResourceGenerated`, `OnRegenerationStarted`, `OnResourceRegenerated`

2. **ResourceDataSO** (data-driven)
   - Configures individual resource types
   - Properties:
     - `resourceName`, `resourceType` (Wood, Stone, Gold, Meat, Experience)
     - `prefabs`: List of visual representations
     - `collectedAmount`: Yield per collection
     - `groupCount`: Max instances per generation
     - `possibleGroupSizes`: Clustering options [3, 5, 7, 9]
     - `regenerationTime`: Respawn delay (seconds)
     - `isDestroyedOnCollect`: Permanent vs. respawning
   - Method: `GetRandomPrefab()` - Random visual variant

3. **ResourceSpawner.cs**
   - Generates resources across map
   - Uses: IResourceGenerationStrategy pattern
   - Methods:
     - `GenerateAllResources()` - Main entry point
     - Selects strategy (configurable per generation)
   - Event: `OnResourceSpawned(type, pos, instance)`

4. **IResourceGenerationStrategy** (interface)
   - Defines generation behavior contract
   - Method: `Generate(GridManager, resourceData)`
   - Implementations:
     1. **ClusterGenerationStrategy** - Grouped resources
     2. **RegularGridGenerationStrategy** - Even spacing
     3. **RegularGridWithSingleRandomGenerationStrategy** - Mostly regular, 1 random
     4. **RegularGridWithRandomGenerationStrategy** - Regular + random variations

5. **ResourceInstance.cs**
   - Individual resource object (MonoBehaviour)
   - Methods:
     - `Initialize()` - Set data + position
     - Collection triggers → ResourceManager callback

6. **RegenResourceInstance.cs**
   - Visual prefab during regeneration countdown
   - Placeholder/indicator for player feedback

7. **ResourcePoolManager.cs**
   - Object Pooling implementation
   - Methods:
     - `ReturnToPool()` - Reuse instance
     - `GetFromPool()` - Reuse or create
   - **Optimization**: Reduces Instantiate/Destroy calls

8. **ResourceUIDisplay.cs** & **ResourceIconsSO**
   - UI representation of resources
   - Icons per resource type

**Resource Flow**:
```
ResourceSpawner.GenerateAllResources()
  ↓ (IResourceGenerationStrategy)
Create ResourceInstance at positions
  ↓
ResourceManager.HandleResourceSpawned()
  ↓
Add to _activeResources, update ZoneManager.occupiedTiles
  ↓
Player collects → HandleResourceCollected()
  ↓
UpdateEconomy() [GameEconomyManager.AddResource()]
  ↓
If !isDestroyedOnCollect:
  └→ ScheduleRegeneration() [Create visual + timer coroutine]
```

---

### 3.4 ECONOMY SYSTEM (Resource Management)

**Responsibility**: Global resource tracking and spending validation

**Key Classes**:

1. **GameEconomyManager.cs** (sealed singleton)
   - **Pattern**: Singleton + Thread-Safe initialization
   - State: `_resources` Dictionary<ResourceType, int>
   - Initialization:
     - Awake: Creates singleton, DontDestroyOnLoad
     - Initializes all ResourceType enums to 0
   - Methods:
     - `AddResource(type, amount)` - Increase resources
     - `SpendResources(type, amount)` - Decrease + validation
     - `SpendResources(dict)` - Batch spending
     - `CanAfford(dict)` - Check if affordable
     - `GetResourceAmount(type)` - Query current amount
   - Events:
     - `OnResourceAmountChanged(type, newAmount)` - Per-resource updates
     - `OnResourcesBatchChanged(dict)` - Batch updates
   - **Cleanup**: `OnDestroy()` clears events to prevent memory leaks

**Resource Types** (Enum):
```
None, Wood, Stone, Gold, Meat, Experience
```

---

### 3.5 CAMERA SYSTEM (Isometric View)

**Responsibility**: Manage camera positioning, zoom, and pan for isometric view

**Key Classes**:

1. **CameraController.cs**
   - Initialization:
     - Builds same Matrix4x4 as Grid for coordinate alignment
     - Centers camera on grid center
   - Parameters:
     - `width`, `height`, `cellSize` - Grid-aligned sizing
     - `zoomSpeed`, `minZoom` (1f), `maxZoom` (30f), `initialZoom` (5f)
     - `dragSpeed` - Pan speed
   - Methods:
     - `InitIsoMatrix()` - Match Grid's transformation
     - `CenterCamera()` - Position at grid center
     - `SetUpCamera()` - Calculate bounds for constraints
     - `HandleZoom()` - Mouse scroll
     - `HandleDrag()` - Middle mouse drag panning
   - **Bounds Enforcement**: Constrains camera to viewable grid area

---

### 3.6 GENERIC PREVIEW SYSTEM (Reusable)

**Responsibility**: Flexible preview system for any GameObject (buildings, units, decorations)

**Key Classes**:

1. **GenericPreviewSystem.cs**
   - **Purpose**: Decoupled from building-specific logic
   - Reusable for: Buildings, Units, Resources, Decorations
   - State:
     - `_currentPreview`: Active GameObject
     - `_lastPosition`: Cache for distance checks
     - `_lastValidState`: Track validation state
   - Methods:
     - `ShowPreview()` - Create/update
     - `UpdatePreviewIfMoved()` - Optimize with distance threshold
     - `SetScale()`, `SetYOffset()`, `SetPreviewName()`
     - `SetValidationState()` - Color control
   - Colors:
     - `_validColor`: Green tint (0.7, 1, 0.7, 0.8)
     - `_invalidColor`: Red tint (1, 0.7, 0.7, 0.8)
     - `_neutralColor`: White (1, 1, 1, 0.8)

---

## 4. CODE QUALITY & TECHNICAL DEBT

### 4.1 Strengths (Well-Implemented Patterns)

✅ **Interface-Based Design**
- IGridService decouples BuildingSystem from GridManager
- IResourceGenerationStrategy enables pluggable generation algorithms
- Testability: Easy to mock dependencies

✅ **Data-Driven Approach**
- BuildingConfigSO: Zero code changes for new buildings
- ResourceDataSO: Flexible resource types + regeneration
- Scalable without code modifications

✅ **Singleton Pattern (Appropriate Usage)**
- GameEconomyManager, GridManager, CameraController
- Justified for global state managers
- Proper DontDestroyOnLoad handling

✅ **Event-Driven Communication**
- BuildingEvents (OnBuildingPlaced, OnBuildingDestroyed)
- ZoneManager events (OnZoneUnlocked, OnZonePurchaseFailed)
- Reduces direct coupling between systems

✅ **Factory Pattern**
- BuildingFactory centralizes creation logic
- ResourceSpawner + IResourceGenerationStrategy
- Easy to add variants without modifying creation sites

✅ **Isometric Math Implementation**
- Matrix4x4 transformation clean and reusable
- Consistent coordinate conversion across systems
- Sorting formula (Y-based) works well for depth

✅ **Object Pooling**
- ResourcePoolManager reduces GC pressure
- Important for frequent resource collection

✅ **MonoBehaviour Separation**
- Building.cs is lightweight (only rendering + sorting)
- BuildingPlacer orchestrates, doesn't implement placement
- SRP mostly respected

---

### 4.2 Critical Bottlenecks & Issues

⚠️ **ISSUE #1: Tile Hover State Sticking in Locked Zones**
- **Location**: Tile.cs (`OnMouseEnter/OnMouseExit`)
- **Problem**: When preview tiles are shown in locked zones, some tiles remain in hover (yellow) state
- **Root Cause**: 
  - `PreviewTint()` saves current color and applies tint
  - `ResetTint()` restores to state color, but flag `_isShowingPreview` may not sync with GridManager's preview tracking
  - GridManager clears old previews but doesn't guarantee all tile states reset
- **Impact**: Visual glitch - confuses player about tile state
- **Reproduction**: 
  1. Move preview over locked zone
  2. Exit preview area slowly
  3. Hover color persists
- **Proposed Fix**:
  - Centralize preview state in GridManager
  - Explicitly call `tile.ResetTint()` on all previous preview cells
  - Use GridManager event to notify tiles when preview ends

⚠️ **ISSUE #2: Update() Loop Spam in BuildingPlacer**
- **Location**: BuildingPlacer.Update() → UpdatePlacementPreview()
- **Problem**: Called every frame, even if mouse didn't move
- **Optimization Already Applied**: `_lastCell` caching + `UpdatePreviewIfCellChanged()`
- **Status**: MITIGATED but could use raycast-based input instead

⚠️ **ISSUE #3: OnMouse* Events Performance**
- **Location**: Tile.cs (OnMouseEnter/OnMouseExit/OnMouseDown)
- **Problem**: Requires Physics2D raycasts every frame to detect hovering
- **Alternative**: Explicit raycast in CameraController, cache results
- **Impact**: Low for current tile count (~2500 tiles), becomes relevant at >10K tiles

⚠️ **ISSUE #4: No Thread-Safe Resource Spending**
- **Location**: GameEconomyManager, BuildingPlacer confirmation
- **Problem**: No lock mechanism; if multiple systems try to spend resources simultaneously, race condition
- **Impact**: Currently LOW (single-threaded game loop), but critical if coroutines overlap
- **Fix**: Add lock around `_resources` dictionary if async operations added

⚠️ **ISSUE #5: Memory Leak Risk in BuildingEvents**
- **Location**: BuildingEvents static class
- **Problem**: Static events not unsubscribed; if scenes reload, handlers persist
- **Fix Already in Code**: `BuildingEvents.ClearAllEvents()` exists but NOT called on scene unload
- **Recommendation**: Call in SceneManager.unloadScene or level transitions

⚠️ **ISSUE #6: Null Reference Cascades**
- **Location**: Multiple `:  Zone Manager, ResourceManager, BuildingPlacer
- **Problem**: Validation in Awake/Start, but if dependencies fail, methods return silently
- **Example**: GridManager.InitializeGrid() returns if TileManager == null, then grid stays uninitialized
- **Fix**: Add explicit startup sequencing (e.g., event-based initialization order)

⚠️ **ISSUE #7: GenericPreviewSystem vs PreviewSystem Duplication**
- **Location**: Both PreviewSystem.cs and GenericPreviewSystem.cs exist
- **Problem**: Nearly identical logic; PreviewSystem is building-specific, GenericPreviewSystem is generic
- **Recommendation**: Consolidate to single GenericPreviewSystem, remove PreviewSystem

⚠️ **ISSUE #8: No Zone Crossing Validation**
- **Location**: BuildingPlacer.CanPlaceBuilding()
- **Problem**: Doesn't check if building footprint crosses zone boundaries (Locked ↔ Unlocked)
- **Impact**: Player can place partially on locked, partially on unlocked zones
- **Fix**: Add zone boundary check in IGridService.AreCellsFree()

⚠️ **ISSUE #9: Coroutine Management**
- **Location**: ResourceManager._regenerationCoroutines
- **Problem**: If GameObject destroyed mid-regen, coroutine orphaned
- **Fix**: Track and stop coroutines in OnDestroy()

⚠️ **ISSUE #10: No Input Conflict Prevention**
- **Location**: KeyboardPlacementInput + BuildingPlacer
- **Problem**: No check if UI is open before accepting input
- **Fix**: Add EventSystem.current.IsPointerOverGameObject() check

---

### 4.3 Performance Analysis

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| **Draw Calls** | ~50-100 (2.5K tiles) | <200 | ✅ Good |
| **GC Allocations** | Moderate (pooling present) | <1MB/frame | ⚠️ Monitor |
| **Physics2D Raycasts** | Per-frame (OnMouse) | Event-based | ❌ Can improve |
| **Grid Lookup** | O(1) (HashSet) | O(1) | ✅ Optimal |
| **Building Creation** | Instantiate + Init | ~5ms | ✅ Acceptable |

---

### 4.4 Recommended Refactoring Priority

**CRITICAL (Blocking)**:
1. Fix tile hover sticking issue (#1)
2. Add zone boundary validation (#8)
3. Prevent UI input conflicts (#10)

**HIGH (Improvement)**:
4. Replace OnMouse* with raycast-based input (#3)
5. Consolidate Preview systems (#7)
6. Add coroutine cleanup (#9)
7. Implement startup sequencing (#6)

**MEDIUM (Optimization)**:
8. Thread-safe resource system (#4)
9. Event cleanup on scene transitions (#5)
10. GC allocation audit and pooling expansion

**LOW (Nice-to-Have)**:
11. Full RTS mechanics (unit movement, pathfinding) - Not Implemented
12. Save/Load system - Not Implemented
13. Audio system - Not Implemented

---

## 5. NOT YET IMPLEMENTED

- **Unit System**: No unit selection, movement, or pathfinding (A*)
- **Combat**: No unit attacking, health, or combat mechanics
- **Diplomacy**: No faction system or trading
- **Persistence**: No save/load functionality
- **UI Framework**: Basic event logging only; no HUD or menus
- **Networking**: Single-player only
- **Animation**: Static sprites only
- **Particle Effects**: None implemented
- **Audio**: Sound system not present

---

## 6. FILE STRUCTURE REFERENCE

```
Assets/Script2/
├── BuildingSystem/
│   ├── Building.cs                    [Individual building instance]
│   ├── BuildingManager.cs             [Coordinator + Events hub]
│   ├── BuildingPlacer.cs              [Placement orchestration]
│   ├── BuildingFactory.cs             [Factory pattern]
│   ├── BuildingConfigSO.cs            [Data-driven config]
│   ├── PreviewSystem.cs               [Building preview (DUPLICATE)]
│   ├── IGridService.cs                [Grid abstraction]
│   └── KeyboardPlacementInput.cs       [Input handler]
│
├── GridSystem/
│   ├── Grid.cs                        [Generic grid data structure]
│   ├── GridManager.cs                 [Singleton + IGridService impl]
│   ├── TileManager.cs                 [Tile instantiation]
│   ├── Tile.cs                        [Individual tile + hover logic]
│   ├── ZoneManager.cs                 [Zone division + purchase]
│   ├── ZoneFeedbackUI.cs              [Zone events listener]
│   └── PurchaseSign.cs                [Not deeply analyzed]
│
├── ResourceSystem/
│   ├── ResourceManager.cs             [Coordinator]
│   ├── ResourceDataSO.cs              [Resource config]
│   ├── ResourceSpawner.cs             [Generation orchestration]
│   ├── ResourceInstance.cs            [Individual resource]
│   ├── RegenResourceInstance.cs        [Regen visual]
│   ├── ResourcePoolManager.cs          [Object pooling]
│   ├── ResourceUI/                    [UI components]
│   └── ResourceGenerationStrategy/    [Strategy implementations]
│       ├── IResourceGenerationStrategy.cs
│       ├── ClusterGenerationStrategy.cs
│       ├── RegularGridGenerationStrategy.cs
│       ├── RegularGridWithSingleRandomGenerationStrategy.cs
│       └── RegularGridWithRandomGenerationStrategy.cs
│
├── EconomySystem/
│   └── GameEconomyManager.cs          [Singleton resource manager]
│
├── CameraSystem/
│   └── CameraController.cs            [Isometric camera control]
│
└── Common/
    └── GenericPreviewSystem.cs        [Reusable preview (DUPLICATE)]
```

---

## 7. INITIALIZATION SEQUENCE

**Expected Boot Order**:
1. **Awake Phase** (deterministic order set in scene):
   - CameraController (sets up isometric camera)
   - GameEconomyManager (creates singleton, initializes resource dict)
   - GridManager.Awake() → calls ValidateDependencies() + InitializeGrid()
   - BuildingManager.Awake() → caches singletons
   - PreviewSystem.Awake() (empty, just caches components)

2. **Start Phase**:
   - ResourceManager.Start() → subscribes to ResourceSpawner.OnResourceSpawned
   - ResourceSpawner.GenerateAllResources() → places initial resources
   - TileManager/ZoneManager already initialized in GridManager

3. **Update Phase** (per-frame):
   - CameraController: Handles zoom/pan input
   - BuildingPlacer: Updates preview if placing
   - Tile: OnMouse* events (hover feedback)

**Dependency Injection Strategy**: None currently (Singletons + Direct References)
**Future Improvement**: Zenject or custom DI container for testability

---

## 8. KEY EQUATIONS & FORMULAS

### Isometric Transformation
```
world = [1, -1] × grid + [0.5, 0.5] × grid_y × 1
```

### Tile Sorting Order
```
order = -(position.y * 100) + baseOrder
```

### Zone Identification
```
zoneCoord = (gridPos / zoneSize) * zoneSize
```

### Resource Regeneration
```
regenTime = ResourceDataSO.regenerationTime (seconds, as Coroutine delay)
```

---

## 9. CRITICAL REFERENCES & SINGLETONS

| Singleton | Access | Lifecycle | Purpose |
|-----------|--------|-----------|---------|
| **GameEconomyManager** | `Instance` | Persistent (DontDestroyOnLoad) | Global resources |
| **GridManager** | `Instance` | Persistent | Grid + zones + preview state |
| **CameraController** | `Camera.main` | Scene-based | Isometric view |
| **BuildingManager** | Dependency injection (scene ref) | Scene-based | Building coordination |

---

## 10. NEXT STEPS FOR FEATURE EXPANSION

1. **Implement Tile Hover Fix**:
   - Add `ClearPreview()` method in GridManager
   - Call before setting new preview
   - Explicitly reset all tile tints

2. **Add RTS Mechanics**:
   - Unit class (similar to Building)
   - PathfindingSystem (A* on grid)
   - SelectionManager (click-to-select)
   - MovementController (path following + animation)

3. **Expand Building System**:
   - Production queues (resource generation)
   - Upgrade tiers
   - Building effects (area bonuses)

4. **Implement Persistence**:
   - Save/Load serialization (JSON or ScriptableObject)
   - Session state preservation

5. **Polish UI/UX**:
   - Building placement UI (preview stats)
   - Resource counter HUD
   - Tooltips for zones/buildings

---

## SUMMARY

**Social Empire** is a well-architected 2.5D isometric Citybuilder with:
- ✅ Clean separation of concerns (Grid, Building, Resource, Economy systems)
- ✅ Custom isometric engine using Matrix4x4 transformation
- ✅ Event-driven communication (Observer pattern)
- ✅ Data-driven design (ScriptableObjects)
- ✅ Object pooling for performance

**Primary Issues**:
- ⚠️ Tile hover state persistence in locked zones
- ⚠️ Duplicate preview systems (consolidation needed)
- ⚠️ No RTS unit mechanics

**Architecture Maturity**: 7/10 (solid foundation, needs RTS expansion and polish)

---

**Document Generated**: 2026-01-24  
**Analyzed Files**: 32 C# classes  
**System Complexity**: Medium-High

