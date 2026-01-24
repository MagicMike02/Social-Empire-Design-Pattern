# QUICK REFERENCE GUIDE - SOCIAL EMPIRE ARCHITECTURE

**Use this document for rapid lookups during development**

---

## QUICK ANSWERS

### Q: How do I add a new building type?
1. Create `BuildingConfig_YourBuilding.asset` (ScriptableObject)
2. Set Prefab, Width, Height, Costs, SortingLayer
3. In scene, add to list in `KeyboardPlacementInput._testBuildingConfig`
4. Done! No code changes needed.

### Q: How do I add a new resource type?
1. Add to enum: `ResourceType.NewResource` in `ResourceType.cs`
2. Create `ResourceData_NewResource.asset` (ScriptableObject)
3. Set prefabs, collection amount, regeneration time
4. In ResourceSpawner, add to `_resourceConfigs` list
5. Done! Resources auto-spawn and collect.

### Q: How do I detect when a building is placed?
```csharp
private void OnEnable()
{
    BuildingEvents.OnBuildingPlaced += HandleBuildingPlaced;
}

private void OnDisable()
{
    BuildingEvents.OnBuildingPlaced -= HandleBuildingPlaced;
}

private void HandleBuildingPlaced(Building building)
{
    Debug.Log($"Building placed: {building.Config.name}");
}
```

### Q: How do I check if I have enough resources?
```csharp
var cost = new Dictionary<ResourceType, int> { { Gold, 50 }, { Wood, 20 } };
if (GameEconomyManager.Instance.CanAfford(cost))
{
    // Sufficient resources
}
```

### Q: How do I add/spend resources?
```csharp
// Add
GameEconomyManager.Instance.AddResource(ResourceType.Gold, 100);

// Spend (returns success)
bool spent = GameEconomyManager.Instance.SpendResources(ResourceType.Gold, 50);
```

### Q: How do I get grid position from world position?
```csharp
IGridService grid = GridManager.Instance;
if (grid.TryWorldToCell(worldPosition, out Vector3Int cell))
{
    int x = cell.x;
    int y = cell.y;
    // cell is valid
}
```

### Q: How do I convert grid position to world?
```csharp
Vector3 worldPos = GridManager.Instance.CellToWorld(new Vector3Int(5, 3, 0));
```

### Q: How do I subscribe to tile state changes?
```csharp
// Tile state changes are not currently event-driven
// Access directly:
Tile tile = tileManager.GetGrid().GetValue(x, y);
TileState state = tile.State;
```

### Q: How do I check if a building can be placed?
```csharp
public bool CanPlaceBuilding(BuildingConfigSO config, Vector3Int cell)
{
    IGridService grid = _manager.Grid;
    
    // Check 1: Cells within bounds and free
    if (!grid.AreCellsFree(cell, config.Width, config.Height))
        return false;
    
    // Check 2: Resources available
    var costs = config.ToDictionary();
    if (!_manager.Economy.CanAfford(costs))
        return false;
    
    return true;
}
```

### Q: How do I add a custom preview color?
```csharp
// In PreviewSystem.cs serialized fields
[SerializeField] private Color _validColor = new Color(0.7f, 1f, 0.7f, 0.8f);
[SerializeField] private Color _invalidColor = new Color(1f, 0.7f, 0.7f, 0.8f);

// OR in code:
previewSystem.SetValidationColors(
    validColor: Color.green,
    invalidColor: Color.red,
    neutral: Color.white
);
```

---

## SINGLETON REFERENCE

```csharp
// Global Singletons (always available after Awake)
GameEconomyManager.Instance              // Economy
GridManager.Instance                     // Grid + Zones

// Scene References (check before use)
BuildingManager                          // Building coordination
CameraController                         // Camera control
```

---

## INTERFACE REFERENCE

### IGridService
```csharp
public interface IGridService
{
    bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell);
    Vector3 CellToWorld(Vector3Int cell);
    bool AreCellsFree(Vector3Int originCell, int width, int height);
    void OccupyCells(Vector3Int originCell, int width, int height, Building building);
    void FreeCells(Vector3Int originCell, int width, int height);
    void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid);
}
```

### IResourceGenerationStrategy
```csharp
public interface IResourceGenerationStrategy
{
    void Generate(GridManager gridManager, ResourceDataSO resourceData);
}
```

---

## EVENT REFERENCE

```csharp
// Building Events
BuildingEvents.OnBuildingSelected    // building selected
BuildingEvents.OnBuildingDestroyed   // building destroyed
BuildingEvents.OnBuildingPlaced      // building placed

// Economy Events
GameEconomyManager.OnResourceAmountChanged(ResourceType, int)
GameEconomyManager.OnResourcesBatchChanged(Dictionary)

// Zone Events
ZoneManager.OnZoneUnlocked(Vector2Int zoneCoord)
ZoneManager.OnZonePurchaseFailed(Vector2Int zoneCoord)

// Resource Events
ResourceManager.OnResourceCollected(ResourceType, int, Vector2Int)
ResourceManager.OnResourceGenerated(ResourceType, Vector2Int)
ResourceManager.OnRegenerationStarted(Vector2Int, float)
ResourceManager.OnResourceRegenerated(Vector2Int, ResourceType)
```

---

## SCRIPTABLEOBJECT RECIPES

### Creating BuildingConfigSO
```csharp
// In Assets/Resources or anywhere, right-click
// Create > Script2 > Building > Config
// Set fields:
// - Prefab: Your building prefab
// - Width: 2 cells
// - Height: 2 cells
// - Costs: (ResourceType, Amount) pairs
// - SortingLayer: "OnTiles"
// - BaseSortingOrder: 0
```

### Creating ResourceDataSO
```csharp
// Right-click > Create > ScriptableObjects > ResourceDataSO
// Set fields:
// - resourceName: "Wood Pile"
// - resourceType: Wood
// - prefabs: [wood_visual_1, wood_visual_2] (variants)
// - collectedAmount: 10
// - groupCount: 5
// - possibleGroupSizes: [3, 5, 7]
// - regenerationTime: 300 (seconds)
// - isDestroyedOnCollect: true/false
```

---

## COORDINATE SYSTEM

```
Grid Coordinates (Integer):
  (0, 0) ───X──→  (width, 0)
    │
    Y
    │
    ↓
(0, height)

World Coordinates (Float, Isometric):
  Uses Matrix4x4 transformation
  X-axis: (1, 0.5) in world units
  Y-axis: (-1, 0.5) in world units
  
  Grid (1, 0) → World (1, 0.5)
  Grid (0, 1) → World (-1, 0.5)
  Grid (1, 1) → World (0, 1)
```

---

## DEBUGGING TIPS

### Print Grid Coordinates
```csharp
GridManager.Instance.TryWorldToCell(transform.position, out Vector3Int cell);
Debug.Log($"Grid Position: ({cell.x}, {cell.y})");
```

### Check Tile State
```csharp
Tile tile = GridManager.Instance
    ._tileManager.GetGrid()
    .GetValue(x, y);
Debug.Log($"Tile State: {tile.State}");
```

### Check Occupancy
```csharp
// Access internal (not ideal, but works for debug)
// Use SetCellsPreview to visualize instead
GridManager.Instance.SetCellsPreview(cell, 1, 1, isValid: true);
```

### Monitor Resource Changes
```csharp
GameEconomyManager.Instance.OnResourceAmountChanged += (type, amount) =>
{
    Debug.Log($"{type}: {amount}");
};
```

### Check Placement Validation
```csharp
public void DebugPlacementCheck(Vector3 worldPos, BuildingConfigSO config)
{
    IGridService grid = GridManager.Instance;
    grid.TryWorldToCell(worldPos, out Vector3Int cell);
    
    Debug.Log($"Cell: {cell}");
    Debug.Log($"Cells Free: {grid.AreCellsFree(cell, config.Width, config.Height)}");
    
    var economy = GameEconomyManager.Instance;
    var costs = config.ToDictionary();
    Debug.Log($"Can Afford: {economy.CanAfford(costs)}");
    
    foreach (var (type, amount) in costs)
    {
        Debug.Log($"  {type}: {economy.GetResourceAmount(type)}/{amount}");
    }
}
```

---

## COMMON PITFALLS & SOLUTIONS

| Problem | Cause | Solution |
|---------|-------|----------|
| Building not visible after placement | Sorting layer wrong | Check BuildingConfigSO.SortingLayer |
| Building placed in wrong location | Cell calculation error | Verify grid.TryWorldToCell() result |
| Preview colors wrong (violet tint) | Multiple color multiplications | Use RGB (not alpha) for tinting |
| Hover state sticks on tiles | ResetTint() not called | Call GridManager.ClearPreviousPreview() |
| Null reference in BuildingPlacer | GridManager not initialized | Ensure GridManager in scene before BuildingPlacer.Start() |
| Resources not spending | Spending validated but failed | Check GameEconomyManager.Instance exists |
| Zones locked but shouldn't be | Zone initialization missed | Verify ZoneManager.CreateZones() called |
| Camera showing black screen | Isometric math off | Verify CameraController.InitIsoMatrix() matches Grid's |

---

## PERFORMANCE CHECKLIST

- [ ] No GetComponent<>() in Update()
- [ ] No new Dictionary/List allocations in Update()
- [ ] Grid lookups use HashSet (_occupiedCells)
- [ ] Preview updates throttled with cell-change check
- [ ] Coroutines cleaned up in OnDestroy()
- [ ] Static events unsubscribed on scene unload
- [ ] Object pooling used for frequently created objects
- [ ] Raycast results cached instead of per-frame

---

## FILE LOCATIONS (Quick Navigation)

| Feature | File | Namespace |
|---------|------|-----------|
| **Building Creation** | BuildingFactory.cs | Script2.BuildingSystem |
| **Building Placement** | BuildingPlacer.cs | Script2.BuildingSystem |
| **Grid System** | GridManager.cs | Script2.GridSystem |
| **Resources** | ResourceManager.cs | Script2.ResourceSystem |
| **Economy** | GameEconomyManager.cs | Script2.Economy |
| **Camera** | CameraController.cs | Script2.CameraSystem |
| **Tiles** | Tile.cs | Script2.GridSystem |
| **Zones** | ZoneManager.cs | Script2.GridSystem |
| **Building Config** | BuildingConfigSO.cs | Script2.BuildingSystem |
| **Resource Config** | ResourceDataSO.cs | Script2.ResourceSystem |

---

## EXAMPLE: COMPLETE PLACEMENT FLOW

```csharp
// In a UI button or input system
public void PlaceHouse()
{
    // 1. Get building config
    var houseConfig = Resources.Load<BuildingConfigSO>("Buildings/House");
    
    // 2. Start placement
    _placer.StartPlacing(houseConfig);
    
    // 3. In BuildingPlacer.Update():
    // - Ray from camera
    // - Convert to grid cell
    // - Check AreCellsFree()
    // - Check CanAfford()
    // - Update preview color
    
    // 4. On confirm (Input.Alpha1):
    _placer.ConfirmPlacement();
    
    // 5. BuildingPlacer does:
    // - Creates building via factory
    // - Spends resources
    // - Occupies cells
    // - Fires OnBuildingPlaced event
}

// 6. Listen to placement event
private void OnEnable()
{
    BuildingEvents.OnBuildingPlaced += (building) =>
    {
        Debug.Log($"House placed at {building.transform.position}!");
    };
}
```

---

## NEXT DEVELOPER CHECKLIST

- [ ] Read TECHNICAL_KNOWLEDGE_BASE.md (full architecture)
- [ ] Read SYSTEM_FLOW_DIAGRAMS.md (workflow clarity)
- [ ] Read REFACTORING_ROADMAP.md (known issues)
- [ ] Run project, place a building successfully
- [ ] Modify a BuildingConfigSO and test changes
- [ ] Add a new ResourceType and verify spawning
- [ ] Use debugger with GridManager.Instance
- [ ] Check performance in Profiler window
- [ ] Review code style (SRP, interfaces, events)

---

**Last Updated**: 2026-01-24  
**Complexity Level**: Medium  
**Estimated Learning Curve**: 2-3 hours for experienced Unity developers

