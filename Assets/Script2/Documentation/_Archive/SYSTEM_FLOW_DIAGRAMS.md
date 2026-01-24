# SYSTEM FLOW DIAGRAMS & DEPENDENCY GRAPHS

---

## 1. BUILDING PLACEMENT WORKFLOW

```
┌─────────────────────────────────────────────────────────────────────┐
│ INPUT PHASE                                                         │
└─────────────────────────────────────────────────────────────────────┘

KeyboardPlacementInput.Update()
  └─→ IF (Input.GetKeyDown(Alpha1))
      ├─→ IF (!_placer.IsPlacing)
      │   └─→ BuildingPlacer.StartPlacing(config)
      │       ├─→ _selectedConfig = config
      │       ├─→ _isPlacing = true
      │       └─→ _lastCell = Vector3Int(-1000, -1000, -1000)
      │
      └─→ ELSE
          └─→ BuildingPlacer.ConfirmPlacement()

┌─────────────────────────────────────────────────────────────────────┐
│ PREVIEW PHASE (Every Frame while Placing)                          │
└─────────────────────────────────────────────────────────────────────┘

BuildingPlacer.Update()
  └─→ IF (_isPlacing)
      └─→ UpdatePlacementPreview()
          ├─→ Ray ray = Camera.ScreenPointToRay(Input.mousePosition)
          ├─→ GridManager.TryWorldToCell(ray.origin, out cell)
          │   └─→ IGridService converts world → grid coordinates
          │
          ├─→ _currentCell = cell
          ├─→ Vector3 worldPos = GridManager.CellToWorld(cell)
          │
          ├─→ bool isValid = CanPlaceBuilding(_selectedConfig, cell)
          │   └─→ Checks:
          │       ├─→ Grid.AreCellsFree()
          │       └─→ Resources sufficient? (Economy check)
          │
          ├─→ PreviewSystem.UpdatePreviewIfCellChanged(_currentCell, worldPos, isValid)
          │   └─→ OPTIMIZED: Only updates if cell changed
          │       ├─→ IF (_currentCell != _lastCell)
          │       │   ├─→ Move preview GameObject
          │       │   ├─→ Update color (green/red)
          │       │   └─→ _lastCell = _currentCell
          │       │
          │       └─→ ELSE
          │           └─→ Return (no update)
          │
          └─→ GridManager.SetCellsPreview(_currentCell, width, height, isValid)
              └─→ [See section 3 below]

┌─────────────────────────────────────────────────────────────────────┐
│ PREVIEW TILE VISUAL FEEDBACK                                       │
└─────────────────────────────────────────────────────────────────────┘

GridManager.SetCellsPreview(origin, w, h, isValid)
  │
  ├─→ IF (w <= 0 || h <= 0)
  │   └─→ [CLEAR PREVIOUS PREVIEW]
  │       ├─→ FOR EACH tile in _lastPreviewCells
  │       │   └─→ tile.ResetTint() ⚠️ CRITICAL FIX POINT
  │       │       ├─→ _isShowingPreview = false
  │       │       └─→ Restore state color (grey/green/white)
  │       └─→ _lastPreviewCells.Clear()
  │
  └─→ ELSE
      └─→ [SET NEW PREVIEW]
          ├─→ newPreviewCells = new List<Vector2Int>()
          │
          ├─→ FOR dx IN [0, w) AND dy IN [0, h)
          │   ├─→ cell = origin + (dx, dy)
          │   ├─→ tile = grid.GetValue(cell)
          │   │
          │   ├─→ IF (tile != null)
          │   │   ├─→ IF (isValid) color = GREEN else color = RED
          │   │   ├─→ tile.PreviewTint(color)
          │   │   │   ├─→ _savedColorBeforePreview = current color
          │   │   │   ├─→ _isShowingPreview = true
          │   │   │   └─→ Apply tint
          │   │   │
          │   │   └─→ newPreviewCells.Add(cell)
          │   │
          │   └─→ _previewTileCache[cell] = tile
          │
          └─→ _lastPreviewCells = newPreviewCells

┌─────────────────────────────────────────────────────────────────────┐
│ CONFIRMATION PHASE                                                  │
└─────────────────────────────────────────────────────────────────────┘

BuildingPlacer.ConfirmPlacement()
  │
  ├─→ IF (!_isPlacing || !CanPlaceBuilding())
  │   └─→ Debug.Log("Invalid placement")
  │       └─→ RETURN
  │
  └─→ ELSE [CREATE BUILDING]
      ├─→ worldPos = GridManager.CellToWorld(_currentCell)
      │
      ├─→ building = BuildingManager.Factory.CreateBuilding(config, worldPos)
      │   └─→ BuildingFactory.CreateBuilding()
      │       ├─→ Instantiate(config.Prefab, worldPos, Quaternion.identity)
      │       ├─→ building = go.GetComponent<Building>()
      │       ├─→ building.Init(config)
      │       ├─→ BuildingManager.ApplySorting() [Set Y-based sorting]
      │       └─→ RETURN building
      │
      ├─→ [SPEND RESOURCES]
      │   ├─→ resourceCosts = config.ToDictionary()
      │   └─→ GameEconomyManager.SpendResources(costs)
      │       ├─→ FOR EACH resourceType in costs
      │       │   └─→ IF (current >= cost)
      │       │       ├─→ current -= cost
      │       │       └─→ OnResourceAmountChanged?.Invoke(type, newAmount)
      │       │
      │       └─→ ELSE
      │           └─→ FAIL (but we checked before, so shouldn't happen)
      │
      ├─→ [OCCUPY CELLS]
      │   └─→ GridManager.OccupyCells(origin, width, height, building)
      │       ├─→ FOR dx IN [0, width) AND dy IN [0, height)
      │       │   └─→ _occupiedCells.Add(Vector2Int(x+dx, y+dy))
      │       └─→ [Now these cells blocked for future placements]
      │
      ├─→ BuildingEvents.OnBuildingPlaced?.Invoke(building)
      │   └─→ [Any listeners can now react: UI, effects, etc]
      │
      └─→ CancelPlacement() [Reset state for next building]
          ├─→ _isPlacing = false
          ├─→ _selectedConfig = null
          ├─→ PreviewSystem.HidePreview()
          └─→ GridManager.SetCellsPreview(origin, 0, 0) [Clear tiles]

```

---

## 2. RESOURCE GENERATION & COLLECTION WORKFLOW

```
┌─────────────────────────────────────────────────────────────────────┐
│ RESOURCE SPAWNING (Initialization)                                 │
└─────────────────────────────────────────────────────────────────────┘

ResourceManager.Start()
  ├─→ _resourceSpawner.OnResourceSpawned += HandleResourceSpawned
  └─→ _resourceSpawner.GenerateAllResources()
      └─→ ResourceSpawner.GenerateAllResources()
          ├─→ FOR EACH ResourceDataSO in _resourceConfigs
          │   │
          │   ├─→ Select IResourceGenerationStrategy (pluggable)
          │   │   ├─→ ClusterGenerationStrategy
          │   │   ├─→ RegularGridGenerationStrategy
          │   │   ├─→ RegularGridWithRandomGenerationStrategy
          │   │   └─→ RegularGridWithSingleRandomGenerationStrategy
          │   │
          │   └─→ strategy.Generate(gridManager, resourceData)
          │       ├─→ Calculate positions based on strategy
          │       └─→ FOR EACH position
          │           ├─→ Instantiate resource prefab
          │           ├─→ position.AddComponent<ResourceInstance>()
          │           └─→ OnResourceSpawned?.Invoke(type, pos, instance)

ResourceManager.HandleResourceSpawned(type, pos, instance)
  ├─→ _activeResources[pos] = instance
  ├─→ _zoneManager.occupiedTiles[pos] = instance [Block for buildings]
  └─→ ri = instance.GetComponent<ResourceInstance>()
      └─→ ri.Initialize(resourceData, pos, this)

┌─────────────────────────────────────────────────────────────────────┐
│ RESOURCE COLLECTION (Player Interacts)                             │
└─────────────────────────────────────────────────────────────────────┘

Player clicks on ResourceInstance
  └─→ ResourceInstance.OnMouseDown()
      └─→ ResourceManager.HandleResourceCollected(pos, data)
          │
          ├─→ [UPDATE ECONOMY]
          │   ├─→ UpdateEconomy(data)
          │   │   └─→ GameEconomyManager.AddResource(type, amount)
          │   │       ├─→ _resources[type] += amount
          │   │       └─→ OnResourceAmountChanged?.Invoke(type, newAmount)
          │   │           └─→ [UI listens and updates display]
          │   │
          │   └─→ OnResourceCollected?.Invoke(type, amount, pos)
          │
          ├─→ [REMOVE FROM GRID]
          │   └─→ RemoveResource(pos)
          │       ├─→ IF (pooling available)
          │       │   └─→ _poolManager.ReturnToPool(instance, data)
          │       │
          │       └─→ ELSE
          │           └─→ Destroy(instance)
          │
          ├─→ _zoneManager.occupiedTiles.Remove(pos)
          └─→ _activeResources.Remove(pos)

┌─────────────────────────────────────────────────────────────────────┐
│ RESOURCE REGENERATION (Delayed Respawn)                            │
└─────────────────────────────────────────────────────────────────────┘

IF (data.isDestroyedOnCollect == false)
  │
  └─→ ScheduleRegeneration(pos, data)
      ├─→ worldPos = GetWorldPosOfTile(pos)
      ├─→ Instantiate(data._regenPrefab, worldPos, ...) [Visual indicator]
      │
      └─→ _regenerationCoroutines[pos] = 
          StartCoroutine(WaitAndRegenerateResource(pos, data))
          │
          ├─→ WaitForSeconds(data.regenerationTime)
          │   └─→ [Player sees countdown/visual]
          │
          └─→ GenerateAllResources() [Re-spawn at position]
              └─→ HandleResourceSpawned(...) [Back in circulation]

```

---

## 3. ZONE UNLOCK & PURCHASE WORKFLOW

```
┌─────────────────────────────────────────────────────────────────────┐
│ ZONE INITIALIZATION                                                │
└─────────────────────────────────────────────────────────────────────┘

GridManager.InitializeGrid()
  └─→ _zoneManager.CreateZones(width, height)
      ├─→ FOR EACH zone position
      │   ├─→ Assign Tile references to Zone
      │   │
      │   ├─→ IF (zone is center)
      │   │   └─→ zone.isUnlocked = true
      │   │       └─→ FOR EACH tile in zone
      │   │           └─→ tile.SetState(Unlocked)
      │   │               └─→ _renderer.color = WHITE
      │   │
      │   └─→ ELSE IF (zone is adjacent to center)
      │       ├─→ zone.isUnlocked = false
      │       ├─→ FOR EACH tile in zone
      │       │   └─→ tile.SetState(Buyable)
      │       │       └─→ _renderer.color = GREEN
      │       │
      │       └─→ CreatePurchaseSign(zone)
      │           ├─→ Instantiate(purchaseSignPrefab, zoneCenter)
      │           ├─→ sign.Setup(zone, economyManager, this, cost)
      │           └─→ zone.purchaseSign = signGameObject
      │
      └─→ ELSE
          ├─→ zone.isUnlocked = false
          └─→ FOR EACH tile in zone
              └─→ tile.SetState(Locked)
                  └─→ _renderer.color = GREY

┌─────────────────────────────────────────────────────────────────────┐
│ PURCHASE SIGN INTERACTION (Click to Unlock)                       │
└─────────────────────────────────────────────────────────────────────┘

PurchaseSign.OnMouseDown()
  └─→ ZoneManager.PurchaseZone(zoneCoord)
      │
      ├─→ IF (!zone exists || zone.isUnlocked)
      │   └─→ RETURN
      │
      ├─→ IF (GameEconomyManager.CanAfford(cost))
      │   │
      │   ├─→ [SPEND RESOURCES]
      │   │   └─→ GameEconomyManager.SpendResources(cost)
      │   │
      │   ├─→ [UNLOCK ZONE]
      │   │   ├─→ zone.isUnlocked = true
      │   │   └─→ FOR EACH tile in zone
      │   │       └─→ tile.Unlock()
      │   │           └─→ tile.SetState(Unlocked)
      │   │               └─→ _renderer.color = WHITE
      │   │
      │   ├─→ [REMOVE SIGN]
      │   │   ├─→ signGridPos = zone.start + (zoneSize/2, zoneSize/2)
      │   │   ├─→ _zoneManager.occupiedTiles.Remove(signGridPos)
      │   │   └─→ Destroy(zone.purchaseSign)
      │   │
      │   └─→ OnZoneUnlocked?.Invoke(zoneCoord)
      │       └─→ [ZoneFeedbackUI or other listeners react]
      │
      └─→ ELSE
          └─→ OnZonePurchaseFailed?.Invoke(zoneCoord)

```

---

## 4. ISOMETRIC COORDINATE TRANSFORMATION

```
┌─────────────────────────────────────────────────────────────────────┐
│ GRID COORDINATE SYSTEM                                             │
└─────────────────────────────────────────────────────────────────────┘

Grid Initialization:
  Grid<T>(width=100, height=100, cellSize=1.0f)
    ├─→ Build Matrix4x4 transformation:
    │   ├─→ Column 0 (X-axis): [1, 0.5, 0, 0]   ← (1 right, 0.5 up)
    │   ├─→ Column 1 (Y-axis): [-1, 0.5, 0, 0]  ← (-1 left, 0.5 up)
    │   └─→ Column 2 (Z-axis): [0, 0, 1, 0]     ← No change
    │
    └─→ Initialize 2D array _grid[100, 100]

┌─────────────────────────────────────────────────────────────────────┐
│ GRID → WORLD CONVERSION (Isometric Positioning)                   │
└─────────────────────────────────────────────────────────────────────┘

Grid.GetIsoToWorldPosition(int x, int y)
  │
  ├─→ cartesianPos = new Vector3(x, y, 0) * cellSize
  │   Example: (5, 3, 0) * 1.0f = (5, 3, 0)
  │
  ├─→ isoPos = matrix.MultiplyPoint3x4(cartesianPos)
  │   Matrix multiplication:
  │   ├─→ isoPos.x = (5 * 1) + (3 * -1) = 2
  │   ├─→ isoPos.y = (5 * 0.5) + (3 * 0.5) = 4
  │   └─→ isoPos.z = 0
  │
  ├─→ Add origin offset
  │   └─→ finalPos = isoPos + Vector3(0, 0, 1) = (2, 4, 1)
  │
  └─→ RETURN (2, 4, 1)

Visual Result: Grid (5, 3) → World (2, 4)
  └─→ Appears at 45° isometric angle on screen

┌─────────────────────────────────────────────────────────────────────┐
│ WORLD → GRID CONVERSION (Raycasting to Grid)                      │
└─────────────────────────────────────────────────────────────────────┘

Grid.GetWorldToIsoPosition(Vector3 worldPos, out int x, out int y)
  │
  ├─→ localPos = worldPos - origin
  │   Example: (2, 4, 1) - (0, 0, 1) = (2, 4, 0)
  │
  ├─→ cartesianPos = matrix.inverse.MultiplyPoint3x4(localPos)
  │   Inverse matrix undoes isometric rotation:
  │   ├─→ cartesianPos.x = (2 * 0.5) + (4 * 0.5) = 3  ⚠️ Approx!
  │   ├─→ cartesianPos.y = (-2 * 0.5) + (4 * 0.5) = 1
  │   └─→ cartesianPos.z = 0
  │
  ├─→ x = Mathf.FloorToInt(cartesianPos.x / cellSize) = 3
  ├─→ y = Mathf.FloorToInt(cartesianPos.y / cellSize) = 1
  │
  └─→ OUT parameters: x=3, y=1

Note: Floating-point precision may accumulate; FloorToInt handles rounding

┌─────────────────────────────────────────────────────────────────────┐
│ DEPTH SORTING FOR ISOMETRIC RENDERING                             │
└─────────────────────────────────────────────────────────────────────┘

SpriteRenderer.sortingOrder = -(position.y * 100) + baseOrder

Example:
  Building at world Y = 5.0
  ├─→ sortingOrder = -(5.0 * 100) + 100 = -400
  │
  Building at world Y = 2.0
  └─→ sortingOrder = -(2.0 * 100) + 100 = -100

Rendering Order (higher sortingOrder = rendered last, on top):
  -500  ← Further back (lower Y), rendered first (behind)
  -400
  -100  ← Closer to front (higher Y), rendered last (in front)

This creates correct isometric depth perception

```

---

## 5. ECONOMY & RESOURCE FLOW

```
┌─────────────────────────────────────────────────────────────────────┐
│ ECONOMY STATE MACHINE                                              │
└─────────────────────────────────────────────────────────────────────┘

GameEconomyManager._resources = {
  Wood:       0,
  Stone:      0,
  Gold:       0,
  Meat:       0,
  Experience: 0
}

┌─────────────────────────────────────────────────────────────────────┐
│ RESOURCE INCOME SOURCES                                            │
└─────────────────────────────────────────────────────────────────────┘

Source 1: Resource Collection
  Player clicks ResourceInstance
    └─→ ResourceManager.HandleResourceCollected()
        └─→ GameEconomyManager.AddResource(Wood, 10)
            ├─→ _resources[Wood] += 10
            └─→ OnResourceAmountChanged?.Invoke(Wood, newAmount)

Source 2: Zone Purchase (Future: Building Production)
  [Not yet implemented]

┌─────────────────────────────────────────────────────────────────────┐
│ RESOURCE SPENDING EVENTS                                           │
└─────────────────────────────────────────────────────────────────────┘

Event 1: Building Placement
  BuildingPlacer.ConfirmPlacement()
    └─→ costs = {Gold: 50, Wood: 20}
        └─→ GameEconomyManager.SpendResources(costs)
            ├─→ FOR EACH (type, amount) in costs
            │   ├─→ IF (_resources[type] >= amount)
            │   │   ├─→ _resources[type] -= amount
            │   │   └─→ OnResourceAmountChanged?.Invoke(type, newAmount)
            │   │
            │   └─→ ELSE
            │       └─→ RETURN false [Insufficient funds]
            │
            └─→ RETURN true [All spent successfully]

Event 2: Zone Purchase
  ZoneManager.PurchaseZone()
    └─→ IF (GameEconomyManager.CanAfford(zoneCost))
        └─→ GameEconomyManager.SpendResources(zoneCost)

┌─────────────────────────────────────────────────────────────────────┐
│ VALIDATION: CAN AFFORD CHECK                                       │
└─────────────────────────────────────────────────────────────────────┘

GameEconomyManager.CanAfford(Dictionary<ResourceType, int> costs)
  │
  └─→ RETURN costs.All(kvp => _resources[kvp.Key] >= kvp.Value)
      │
      Example: CanAfford({Gold: 50, Wood: 20})
      ├─→ Check: _resources[Gold] (30) >= 50? FALSE
      └─→ RETURN false

```

---

## 6. DEPENDENCY INJECTION GRAPH (Current & Proposed)

```
┌─────────────────────────────────────────────────────────────────────┐
│ CURRENT: Singleton + Direct References                            │
└─────────────────────────────────────────────────────────────────────┘

GameEconomyManager (Singleton)
  ↑
  ├─ BuildingPlacer → GridManager.Instance
  ├─ ZoneManager → GameEconomyManager.Instance
  └─ ResourceManager → GameEconomyManager.Instance + ZoneManager

GridManager (Singleton + IGridService)
  ↑
  ├─ BuildingPlacer
  ├─ BuildingManager
  └─ CameraController

BuildingManager (Scene Reference)
  ├─ BuildingFactory
  ├─ BuildingPlacer
  └─ PreviewSystem

Issues:
  ❌ Tight coupling (Singleton references everywhere)
  ❌ Difficult to test (can't mock singletons easily)
  ❌ Order-dependent initialization (risk of null references)

┌─────────────────────────────────────────────────────────────────────┐
│ PROPOSED: Dependency Injection Container (Zenject)               │
└─────────────────────────────────────────────────────────────────────┘

Container Setup (SceneContext.cs):
  ├─→ Bind<GameEconomyManager>().ToSelf().AsSingle()
  ├─→ Bind<IGridService>().To<GridManager>().AsSingle()
  ├─→ Bind<BuildingFactory>().ToSelf().AsSingle()
  ├─→ Bind<IResourceGenerationStrategy>()
  │    .To<ClusterGenerationStrategy>()
  │    .AsTransient()
  └─→ [Each system declares dependencies via constructor]

Example Constructor Injection:
  public class BuildingPlacer : MonoBehaviour
  {
      private IGridService _grid;
      private GameEconomyManager _economy;
      private BuildingFactory _factory;

      public BuildingPlacer(
          IGridService gridService,
          GameEconomyManager economy,
          BuildingFactory factory)
      {
          _grid = gridService;
          _economy = economy;
          _factory = factory;
      }
  }

Benefits:
  ✅ Loose coupling (depends on interfaces, not implementations)
  ✅ Easy to test (inject mocks)
  ✅ Clear dependency graph
  ✅ Guaranteed initialization order

Timeline: Post-v1.0 (requires external library)

```

---

## 7. PREVIEW STATE MACHINE

```
┌─────────────────────────────────────────────────────────────────────┐
│ PREVIEW LIFECYCLE                                                  │
└─────────────────────────────────────────────────────────────────────┘

STATE 1: NOT PLACING
  ├─→ _isPlacing = false
  ├─→ PreviewSystem._currentPreviewInstance = null
  ├─→ Tiles show state color (grey/green/white)
  └─→ Input: Alpha1 → START PLACING

    ↓

STATE 2: PLACING (Preview Active)
  ├─→ _isPlacing = true
  ├─→ _selectedConfig = building config
  ├─→ PreviewSystem._currentPreviewInstance = ghost building (visible)
  │
  ├─→ EVERY FRAME:
  │   ├─→ UpdatePlacementPreview()
  │   │   ├─→ Get mouse world position
  │   │   ├─→ Snap to grid cell
  │   │   ├─→ Validate placement (cells free? resources enough?)
  │   │   └─→ Update preview color & tile tints
  │   │
  │   ├─→ Tile Behavior:
  │   │   ├─→ Preview tiles: PreviewTint(green or red)
  │   │   │   └─→ _isShowingPreview = true
  │   │   │   └─→ Block OnMouseEnter yellow tint
  │   │   │
  │   │   └─→ Non-preview tiles:
  │   │       ├─→ OnMouseEnter: Yellow tint (if !_isShowingPreview)
  │   │       └─→ OnMouseExit: Reset to state color
  │   │
  │   └─→ Input: Alpha1 → CONFIRM PLACEMENT
  │      OR Input: ESC → CANCEL PLACEMENT

    ├─→ (Confirm)

STATE 3: CONFIRMED → BUILDING CREATED
  ├─→ CanPlaceBuilding() validated again
  ├─→ BuildingFactory.CreateBuilding()
  │   ├─→ Instantiate prefab
  │   ├─→ Attach Building component
  │   └─→ Apply sorting
  ├─→ GridManager.OccupyCells() [Mark as blocked]
  ├─→ GameEconomyManager.SpendResources() [Cost deducted]
  ├─→ BuildingEvents.OnBuildingPlaced?.Invoke(building)
  └─→ → STATE 1 (Placement done, back to normal)

    └─→ (OR Cancel)

    └─→ STATE 1: NOT PLACING
        ├─→ PreviewSystem.HidePreview() [Ghost destroyed]
        ├─→ GridManager.SetCellsPreview(0, 0) [Tile tints reset]
        │   └─→ ⚠️ BUGGY STEP: Some tiles may keep yellow tint
        │       (FIX: Explicitly call tile.ResetTint())
        └─→ Ready for next building

```

---

## SUMMARY CONNECTIONS

- **Grid ↔ Building**: IGridService interface decouples both
- **Economy ↔ Building**: BuildingPlacer checks CanAfford before confirming
- **Economy ↔ Zone**: ZoneManager charges for unlocking
- **Grid ↔ Resource**: GridManager tracks occupiedCells for resources
- **Camera ↔ Grid**: CameraController uses same Matrix4x4 for consistency
- **Preview ↔ Tiles**: GridManager notifies tiles to tint/reset

All systems communicate via Events (Observer Pattern) to minimize direct coupling.

