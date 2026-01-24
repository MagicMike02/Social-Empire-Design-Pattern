# STRATEGIC DEVELOPMENT ROADMAP
## Social Empire - 2.5D Isometric RTS/Citybuilder

**Last Updated**: 2026-01-24  
**Status**: Post-Phase 3 (VContainer DI Migration Complete)  
**Next Milestone**: RTS Foundation Systems  

---

# EXECUTIVE SUMMARY

## Current State Assessment

✅ **COMPLETED SYSTEMS** (Weeks 1-3):
- Custom Isometric Grid System (Matrix4x4-based, no native Tilemap)
- Zone Management (Locked/Buyable/Unlocked)
- Building Placement (Multi-cell validation, preview, confirmation)
- Resource Spawning (4 generation strategies, object pooling)
- Resource Collection & Regeneration
- Global Economy System (resource tracking, cost validation)
- **VContainer Dependency Injection** (all Script2 managers migrated)
- Event-Driven Architecture (BuildingEventBus, ZoneManager events)

⚠️ **IN PROGRESS**:
- Hotspot #4: Input System Optimization (HIGH PRIORITY - blocks RTS features)

✅ **RECENTLY RESOLVED**:
- Hotspot #1: Tile Hover State Persistence (FIXED)
- Hotspot #2: Duplicate Preview Systems (FIXED)
- Hotspot #5: Static Event Memory Leaks (FIXED)

❌ **CLOSED (Not Needed)**:
- Hotspot #3: Zone Boundary Validation (design allows cross-zone buildings)

❌ **NOT STARTED**:
- RTS Unit System (selection, movement, combat)
- Pathfinding (A* on isometric grid)
- Building Production (passive resource generation)
- Save/Load System
- Advanced UI (current: keyboard-only)

---

# DOCUMENTATION CONSOLIDATION RESULTS

## Files Structure

### ✅ **MASTER KNOWLEDGE BASE** (Created 2026-01-24)
**Location**: `Assets/Script2/Documentation/PROJECT_KNOWLEDGE_BASE.md`

**Contents** (79 KB, ~1400 lines):
1. Architectural Overview (VContainer DI, Design Patterns)
2. Custom Isometric System (Math formulas, depth sorting)
3. Systems Breakdown (Building, Grid, Resource, Economy)
4. Current Feature Status
5. Technical Debt Log (Hotspots 1-5)
6. Strategic Roadmap (Short/Mid/Long-term)
7. Code Quality Metrics
8. Testing Strategy (future)
9. File Structure Reference
10. Glossary

**Purpose**: Single source of truth for all technical documentation.

---

### 📋 **FILES TO DELETE** (Redundant)

The following files have been consolidated into `PROJECT_KNOWLEDGE_BASE.md` and should be **safely deleted**:

1. ❌ **TECHNICAL_KNOWLEDGE_BASE.md** (829 lines)
   - Reason: Superset merged into PROJECT_KNOWLEDGE_BASE.md
   
2. ❌ **DOCUMENTATION_INDEX.md**
   - Reason: Index no longer needed with single master file
   
3. ❌ **EXECUTIVE_SUMMARY.md**
   - Reason: Summary now in PROJECT_KNOWLEDGE_BASE.md Section 4
   
4. ❌ **QUICK_REFERENCE.md**
   - Reason: Quick ref embedded in PROJECT_KNOWLEDGE_BASE.md Appendix
   
5. ❌ **SYSTEM_FLOW_DIAGRAMS.md**
   - Reason: Flow diagrams integrated into Section 3 (Systems Breakdown)
   
6. ⚠️ **REFACTORING_ROADMAP.md** (676 lines)
   - **Action**: Keep temporarily until hotspots 3-4 resolved
   - **Reason**: Active development reference
   - **Delete After**: Hotspots closed (estimated 1 week)
   
7. ⚠️ **SETUP_UNITY_DI.md**
   - **Action**: Keep temporarily as VContainer setup guide
   - **Reason**: Useful for onboarding new developers
   - **Consider**: Move to README.md or Wiki
   
8. ⚠️ **PHASE2_BEST_PRACTICE_REFACTORING.md**
   - **Action**: Archive (move to `_Archive/` folder)
   - **Reason**: Historical record of Phase 2 decisions
   
9. ⚠️ **CHANGELOG.md**
   - **Action**: Keep (standard for projects)
   - **Reason**: Git commit history supplement

---

### 📁 **RECOMMENDED FOLDER STRUCTURE**

```
Assets/Script2/Documentation/
├── PROJECT_KNOWLEDGE_BASE.md  ← MASTER FILE (read this first)
├── CHANGELOG.md               ← Git history supplement
├── SETUP_UNITY_DI.md         ← Onboarding guide (keep for now)
│
└── _Archive/                  ← Historical documents
    ├── PHASE2_BEST_PRACTICE_REFACTORING.md
    ├── REFACTORING_ROADMAP.md (after hotspots resolved)
    └── (old Technical_Knowledge_Base, Executive_Summary, etc.)
```

**Deletion Script** (PowerShell):
```powershell
cd "Assets/Script2/Documentation"
mkdir _Archive
mv TECHNICAL_KNOWLEDGE_BASE.md _Archive/
mv DOCUMENTATION_INDEX.md _Archive/
mv EXECUTIVE_SUMMARY.md _Archive/
mv QUICK_REFERENCE.md _Archive/
mv SYSTEM_FLOW_DIAGRAMS.md _Archive/
mv PHASE2_BEST_PRACTICE_REFACTORING.md _Archive/
```

---

# STRATEGIC DEVELOPMENT PLAN

## SHORT-TERM: Critical Refinements (Week 4)

### ⚡ **Task 1: Implement Centralized InputManager** (Hotspot #4)

**Priority**: HIGH (Blocks RTS features, critical performance bottleneck)  
**Estimated Effort**: 12-16 hours  

**Problem**: Current `OnMouseEnter/Exit` system doesn't scale beyond 50×50 grids and blocks RTS features.

**Current Performance** (Profiler estimates):
- 50×50 grid (2,500 tiles): ~5ms/frame (manageable)
- 100×100 grid (10,000 tiles): ~22ms/frame (**unplayable - 30 FPS instead of 60**)

**Why This Blocks RTS Development**:
1. ❌ Unit selection requires simultaneous click detection on units + tiles
2. ❌ Drag-box selection impossible with OnMouse events
3. ❌ Multi-layer click priority (Unit > Building > Tile) not supported
4. ❌ Large maps (100×100) needed for RTS gameplay are unplayable
5. ❌ Right-click commands (e.g., "move here") not possible

**Solution**: Centralized InputManager with single raycast per frame + interface-based design

**See**: `PROJECT_KNOWLEDGE_BASE.md` Section 5 (Hotspot #4) for complete analysis including:
- Detailed performance profiling
- Architecture comparison (OnMouse vs InputManager)
- Phase-by-phase implementation guide
- Risk assessment and migration strategy
- Future RTS features unlocked (drag-box, touch support, keyboard shortcuts)

**Quick Architecture**:
```csharp
// IHoverable interface (all interactive objects)
public interface IHoverable
{
    void OnHoverEnter();
    void OnHoverExit();
    void OnClick();
    void OnRightClick(); // For RTS move commands
}

// Centralized InputManager (single raycast per frame)
public class InputManager : MonoBehaviour
{
    [Inject] private Camera _camera;
    private IHoverable _lastHoveredObject;
    
    void Update()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        IHoverable currentHover = hit.collider?.GetComponent<IHoverable>();
        
        if (currentHover != _lastHoveredObject)
        {
            _lastHoveredObject?.OnHoverExit();
            currentHover?.OnHoverEnter();
            _lastHoveredObject = currentHover;
        }
        
        if (Input.GetMouseButtonDown(0)) currentHover?.OnClick();
        if (Input.GetMouseButtonDown(1)) currentHover?.OnRightClick();
    }
}
```

**Migration Steps**:
1. Create `IHoverable` interface (2 hours)
2. Create `InputManager.cs`, register in `GameLifetimeScope` (2 hours)
3. Refactor `Tile.cs` to implement `IHoverable` (keep OnMouse* temporarily) (2 hours)
4. Test both systems in parallel (Inspector toggle) (2 hours)
5. Profile performance: verify 22ms → 0.1ms improvement (1 hour)
6. Remove OnMouse* methods from `Tile.cs` (1 hour)
7. Extend to `Building.cs`, `ResourceInstance.cs` (2 hours)
8. OPTIONAL: Spatial partitioning optimization (6 hours - defer to Week 5)

**Testing**:
- Profile frame time with 100×100 grid: target < 1ms input overhead
- Test hover feedback on all tile states (Locked/Buyable/Unlocked)
- Test click detection: verify all tiles clickable
- Test preview system: verify no regression with building placement

**Expected Benefits**:
- ✅ **Performance**: 220x faster (22ms → 0.1ms on 100×100 grid)
- ✅ **Scalability**: Enables maps up to 200×200 tiles
- ✅ **RTS Unit Selection**: Foundation for click + drag-box selection
- ✅ **Multi-Layer Interaction**: Easy priority system (Unit > Building > Tile)
- ✅ **Future-Proof**: Touch input, keyboard shortcuts trivial to add

**Impact**:
- ⚠️ Requires testing all tile interactions (hover, click, preview)
- ⚠️ Potential edge cases with camera bounds (add 5% buffer)
- ✅ Unlocks pathfinding + unit system implementation (Week 5-8)

---

### 🌲 **Task 2: Resource Placement Validation**

### 🌲 **Task 2: Resource Placement Validation**

**Priority**: LOW  
**Estimated Effort**: 2 hours  

**Problem**: Resources can spawn inside buildings if building placed after resource generation.

**Solution**:
```csharp
// In ResourceSpawner.GenerateAllResources()
List<Vector2Int> occupiedCells = new();

// Get occupied cells from GridManager
var gridManager = FindFirstObjectByType<GridManager>();
// ... add logic to retrieve _occupiedCells (make it public getter)

// Filter resource positions
var validPositions = positions.Where(pos => !occupiedCells.Contains(pos)).ToList();
```

**Testing**: Place building, regenerate resources → verify no overlap.

---

## MID-TERM: RTS Foundation (3-6 Weeks)

### 🛤️ **Milestone 1: Pathfinding System** (Week 4-5)

**Goal**: Implement A* pathfinding on isometric grid for unit navigation.

**Requirements**:
1. **Navigation Grid Layer**: Mark tiles as walkable/unwalkable
2. **A* Algorithm**: Find shortest path avoiding obstacles
3. **Dynamic Updates**: Recalculate paths when buildings placed/destroyed

**Implementation Plan**:

#### Step 1: Extend IGridService (Day 1)
```csharp
public interface IGridService
{
    // ...existing methods...
    
    // NEW: Pathfinding support
    bool IsCellWalkable(Vector2Int cell);
    List<Vector2Int> GetNeighbors(Vector2Int cell); // 4-directional or 8-directional
}

// In GridManager.cs
public bool IsCellWalkable(Vector2Int cell)
{
    Tile tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
    if (tile == null || tile.State != TileState.Unlocked) return false;
    if (_occupiedCells.Contains(cell)) return false; // Buildings block
    return true;
}

public List<Vector2Int> GetNeighbors(Vector2Int cell)
{
    // 4-directional (cross pattern) for isometric
    return new List<Vector2Int>
    {
        cell + new Vector2Int(1, 0),  // East
        cell + new Vector2Int(-1, 0), // West
        cell + new Vector2Int(0, 1),  // North
        cell + new Vector2Int(0, -1)  // South
    }.Where(c => IsCellWalkable(c)).ToList();
}
```

#### Step 2: Create PathfindingManager.cs (Day 2-3)
```csharp
public class PathfindingManager : MonoBehaviour
{
    [Inject] private IGridService _gridService;
    
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        // A* implementation
        var openSet = new PriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();
        
        openSet.Enqueue(start, 0);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        
        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();
            
            if (current == goal)
                return ReconstructPath(cameFrom, current);
            
            foreach (var neighbor in _gridService.GetNeighbors(current))
            {
                float tentativeGScore = gScore[current] + 1f; // Uniform cost
                
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }
        
        return new List<Vector2Int>(); // No path found
    }
    
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance (isometric grid)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}
```

#### Step 3: Optimization - Unity Job System (Day 4-5)
**Problem**: A* on 100×100 grid can take 50ms+ (blocks main thread).

**Solution**: Offload pathfinding to separate thread using Unity Jobs.

```csharp
using Unity.Jobs;
using Unity.Collections;

struct PathfindingJob : IJob
{
    public Vector2Int Start;
    public Vector2Int Goal;
    [ReadOnly] public NativeArray<bool> WalkableGrid; // Flattened 2D grid
    public NativeList<Vector2Int> ResultPath;
    
    public void Execute()
    {
        // A* implementation (same logic as above)
        // Write result to ResultPath
    }
}

// In PathfindingManager
public void FindPathAsync(Vector2Int start, Vector2Int goal, System.Action<List<Vector2Int>> callback)
{
    var walkableGrid = GenerateWalkableGridNativeArray();
    var resultPath = new NativeList<Vector2Int>(Allocator.TempJob);
    
    var job = new PathfindingJob
    {
        Start = start,
        Goal = goal,
        WalkableGrid = walkableGrid,
        ResultPath = resultPath
    };
    
    JobHandle handle = job.Schedule();
    StartCoroutine(WaitForJob(handle, resultPath, callback));
}
```

**Testing**:
- Unit at (0, 0) pathfind to (50, 50) around buildings
- Verify path avoids locked zones
- Measure performance: target < 5ms on 100×100 grid (async)

**Estimated Effort**: 1.5 weeks (40 hours)

---

### 🕹️ **Milestone 2: Unit System** (Week 6-8)

**Goal**: Create selectable, movable units (workers, soldiers).

**Implementation Plan**:

#### Step 1: UnitConfigSO (Day 1)
```csharp
[CreateAssetMenu(fileName = "UnitConfig", menuName = "Social Empire/Unit Config")]
public class UnitConfigSO : ScriptableObject
{
    public GameObject Prefab;
    public float MoveSpeed;
    public int MaxHealth;
    public UnitType Type; // Worker, Soldier, etc.
    public int BaseSortingOrder;
}
```

#### Step 2: Unit.cs (Day 2-4)
```csharp
public class Unit : MonoBehaviour
{
    private UnitConfigSO _config;
    private SpriteRenderer _renderer;
    private Vector2Int _currentGridCell;
    private List<Vector2Int> _pathToFollow;
    private int _currentPathIndex;
    private bool _isMoving;
    
    [Inject] private IGridService _gridService;
    [Inject] private PathfindingManager _pathfinding;
    
    public void Init(UnitConfigSO config, Vector2Int startCell)
    {
        _config = config;
        _currentGridCell = startCell;
        _renderer = GetComponent<SpriteRenderer>();
        SetSortingByY();
    }
    
    public void MoveTo(Vector2Int targetCell)
    {
        _pathfinding.FindPathAsync(_currentGridCell, targetCell, OnPathFound);
    }
    
    private void OnPathFound(List<Vector2Int> path)
    {
        _pathToFollow = path;
        _currentPathIndex = 0;
        _isMoving = true;
    }
    
    void Update()
    {
        if (!_isMoving || _pathToFollow == null) return;
        
        Vector2Int nextCell = _pathToFollow[_currentPathIndex];
        Vector3 targetWorldPos = _gridService.CellToWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
        
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, _config.MoveSpeed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            _currentGridCell = nextCell;
            _currentPathIndex++;
            
            if (_currentPathIndex >= _pathToFollow.Count)
            {
                _isMoving = false; // Destination reached
            }
        }
        
        SetSortingByY(); // Update sorting during movement
    }
    
    private void SetSortingByY()
    {
        int yBase = Mathf.FloorToInt(transform.position.y);
        _renderer.sortingOrder = -yBase * 100 + _config.BaseSortingOrder;
    }
}
```

#### Step 3: UnitSelectionManager.cs (Day 5-7)
```csharp
public class UnitSelectionManager : MonoBehaviour
{
    [Inject] private Camera _camera;
    [Inject] private IGridService _gridService;
    
    private List<Unit> _selectedUnits = new();
    private Vector3 _dragStartPos;
    private bool _isDragging;
    
    void Update()
    {
        // Left click: Single unit selection
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics2D.Raycast(ray.origin, ray.direction, out RaycastHit2D hit))
            {
                Unit unit = hit.collider.GetComponent<Unit>();
                if (unit != null)
                {
                    SelectSingleUnit(unit);
                }
            }
        }
        
        // Right click: Move command
        if (Input.GetMouseButtonDown(1) && _selectedUnits.Count > 0)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            if (_gridService.TryWorldToCell(worldPos, out Vector3Int cell))
            {
                foreach (var unit in _selectedUnits)
                {
                    unit.MoveTo(new Vector2Int(cell.x, cell.y));
                }
            }
        }
        
        // Drag box selection (future enhancement)
        // ...
    }
    
    private void SelectSingleUnit(Unit unit)
    {
        DeselectAll();
        _selectedUnits.Add(unit);
        // Show selection indicator (sprite overlay, etc.)
    }
    
    private void DeselectAll()
    {
        _selectedUnits.Clear();
        // Hide selection indicators
    }
}
```

#### Step 4: UnitFactory.cs (Day 8)
```csharp
public class UnitFactory : MonoBehaviour
{
    [Inject] private IGridService _gridService;
    
    public Unit CreateUnit(UnitConfigSO config, Vector2Int spawnCell)
    {
        Vector3 worldPos = _gridService.CellToWorld(new Vector3Int(spawnCell.x, spawnCell.y, 0));
        GameObject go = Instantiate(config.Prefab, worldPos, Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Init(config, spawnCell);
        return unit;
    }
}
```

**Testing**:
- Spawn worker unit at (5, 5)
- Click unit → verify selection
- Right-click (20, 20) → verify unit moves along path
- Place building mid-path → verify unit recalculates route

**Estimated Effort**: 3 weeks (60 hours)

---

### 🏭 **Milestone 3: Building Production System** (Week 9)

**Goal**: Buildings generate resources passively over time.

**Requirements**:
1. Farm → +5 Food every 30 seconds
2. Mine → +3 Gold every 60 seconds
3. UI progress bar showing next production cycle

**Implementation**:

#### ProductionConfigSO
```csharp
[System.Serializable]
public class ProductionConfig
{
    public ResourceType OutputType;
    public int AmountPerCycle;
    public float CycleTime; // seconds
}
```

#### ProductionBuilding.cs (Component)
```csharp
public class ProductionBuilding : MonoBehaviour
{
    [SerializeField] private ProductionConfig _production;
    [Inject] private GameEconomyManager _economy;
    
    private float _timer;
    
    void Update()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= _production.CycleTime)
        {
            _economy.AddResource(_production.OutputType, _production.AmountPerCycle);
            _timer = 0f;
            
            // Trigger visual feedback (particle effect, sound)
        }
    }
    
    public float GetProductionProgress() => _timer / _production.CycleTime;
}
```

**UI Integration** (future):
- Show progress bar above building sprite
- Display tooltip: "Next harvest: 15s"

**Testing**:
- Place Farm → verify +5 Food added every 30s
- Destroy building mid-cycle → verify production stops

**Estimated Effort**: 1 week (20 hours)

---

## LONG-TERM: Advanced Features (8-12 Weeks)

### 💾 **Save/Load System** (Week 10-12)

**Challenge**: Serialize 100×100 grid (10,000 tiles) + 200 buildings + 50 units efficiently.

**Approach**:
1. **Delta Compression**: Save only modified tiles (not Locked state)
2. **Binary Serialization**: Use Protobuf or MessagePack for compact saves
3. **Async Loading**: Load in chunks via coroutines

**SaveData Schema**:
```csharp
[System.Serializable]
public class SaveData
{
    public List<TileStateData> ModifiedTiles; // Only Unlocked/Buyable
    public List<BuildingData> Buildings;
    public List<UnitData> Units;
    public Dictionary<ResourceType, int> Resources;
    public long Timestamp; // For autosave versioning
}

[System.Serializable]
public struct TileStateData
{
    public Vector2Int Cell;
    public TileState State;
}

[System.Serializable]
public struct BuildingData
{
    public string ConfigID; // BuildingConfigSO name
    public Vector3Int GridCell;
    public int CurrentHealth; // For future damage system
}
```

**Implementation**:
- `SaveManager.cs`: Handles serialization/deserialization
- `ISaveable` interface: Buildings/Units implement to export state
- Autosave every 5 minutes

**Testing**:
- Save game with 100 buildings
- Load save → verify all buildings/resources restored
- Measure: target < 2 seconds load time

**Estimated Effort**: 3 weeks (60 hours)

---

### ⚔️ **Combat System** (Week 13-16)

**Requirements**:
1. Unit Health & Damage
2. Attack Range (melee vs ranged)
3. Target Selection (automatic or manual)
4. Combat Resolution (turn-based or real-time?)

**Implementation**:
- Extend `Unit.cs` with `Health`, `Attack`, `Defense` properties
- Add `CombatManager.cs` to resolve attacks
- Implement fog of war (optional)

**Estimated Effort**: 4 weeks (80 hours)

---

# GAP ANALYSIS: RTS vs Citybuilder

## Current Focus: **70% Citybuilder, 30% RTS**

| Feature | Citybuilder | RTS | Status |
|---------|-------------|-----|--------|
| **Grid System** | ✅ Essential | ✅ Essential | Complete |
| **Building Placement** | ✅ Core Mechanic | ⚠️ Secondary | Complete |
| **Zone Unlocking** | ✅ Progression System | ❌ Not Used | Complete |
| **Resource Collection** | ✅ Click-to-Gather | ⚠️ Units Auto-Gather | Partial |
| **Unit Movement** | ❌ Not Used | ✅ Core Mechanic | **Missing** |
| **Pathfinding** | ❌ Not Used | ✅ Essential | **Missing** |
| **Unit Selection** | ❌ Not Used | ✅ Essential | **Missing** |
| **Combat** | ❌ Optional | ✅ Core Mechanic | **Missing** |
| **Building Production** | ⚠️ Nice-to-Have | ✅ Resource Flow | **Missing** |

**Recommendation**: Prioritize RTS Foundation (Pathfinding + Units) to balance genre hybrid.

---

# CRITICAL SUCCESS FACTORS

## Performance Targets

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| **Frame Time (60 FPS)** | 16.6ms | 16.6ms | ✅ Met |
| **Grid Rendering** | 5ms (2500 tiles) | < 1ms (10k tiles) | ⚠️ Needs Optimization |
| **Pathfinding** | N/A | < 5ms (async) | 🚧 To Implement |
| **Save/Load Time** | N/A | < 2 seconds | 🚧 To Implement |
| **Build Placement** | 0.2ms | < 0.5ms | ✅ Optimized |

---

# NEXT ACTIONS (IMMEDIATE)

## Week 4 Checklist

1. ✅ **Documentation Consolidation** (COMPLETED 2026-01-24)
   - Created PROJECT_KNOWLEDGE_BASE.md (single source of truth)
   - Created STRATEGIC_ROADMAP.md (prioritized development plan)
   - Moved old .md files to `_Archive/` folder

2. **Implement InputManager** (12-16 hours) - PRIORITY #1
   - Create `IHoverable` interface (2h)
   - Create `InputManager.cs`, register in VContainer (2h)
   - Refactor `Tile.cs` to implement `IHoverable` (2h)
   - Test both systems in parallel (2h)
   - Profile performance: verify 22ms → 0.1ms (1h)
   - Remove OnMouse* methods, extend to Building/Resource (3h)
   - **Goal**: 220x performance improvement, unlock RTS features

3. **Resource Placement Validation** (2 hours) - PRIORITY #2
   - Filter resource spawn positions by `_occupiedCells`
   - Subscribe to `BuildingEventBus.OnBuildingDestroyed` to respawn

4. **Begin Pathfinding System** (16 hours) - PRIORITY #3 (Week 5 if time allows)
   - Extend IGridService with `IsCellWalkable()`, `GetNeighbors()`
   - Implement A* in PathfindingManager.cs
   - Write unit tests for pathfinding
   - **Note**: Depends on InputManager completion (RTS foundation)

**Total Estimated Time Week 4**: ~18-20 hours (focused sprint)  
**Deferred to Week 5**: Pathfinding system (requires InputManager first)

---

**END OF STRATEGIC ROADMAP**

*Update this file after each milestone completion. Do not create new roadmap files.*
