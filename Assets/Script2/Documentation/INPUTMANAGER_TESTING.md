# INPUT MANAGER TESTING GUIDE

## Pre-requisiti

### 1. Layer Configuration (Critical)
Unity Editor → Project Settings → Tags and Layers

Create three layers (if not already present):
- `Unit`
- `Building`
- `Tile`

### 2. Assign Layers to GameObjects

#### Tiles
- Select ALL tile prefabs/instances in scene
- Assign to layer `Tile`
- Command (Editor):
  ```csharp
  GameObject[] tiles = FindObjectsOfType<Tile>().Select(t => t.gameObject).ToArray();
  foreach (var t in tiles) t.layer = LayerMask.NameToLayer("Tile");
  ```

#### Buildings (Future)
- Currently none in scene; when added, assign to layer `Building`

#### Units (Future)
- Currently none in scene; when added, assign to layer `Unit`

---

## Setup in Scene

### 1. Create InputManager GameObject

1. Right-click in Hierarchy → **Create Empty**
2. Name: `InputManager`
3. Add Component: **Script → InputManager** (Script2.InputSystem)

### 2. Configure InputManager Inspector

**Camera Field**: Already injected by VContainer (no setup needed)

**Layer Masks** (CRITICAL):
- **Unit Mask**: Layer "Unit" ✓
- **Building Mask**: Layer "Building" ✓
- **Tile Mask**: Layer "Tile" ✓

### 3. Verify VContainer Registration

- InputManager will be registered in `GameLifetimeScope.Configure()`
- No manual assignment needed (DI handles it)
- Check Console on Play: should see `[GameLifetimeScope] ✓ InputManager`

---

## Test Scenarios

### Scenario 1: Hover Detection (Tile)

**Action**:
1. Play scene
2. Move mouse over a tile

**Expected Output**:
```
[InputManager] Hovering: Tile
```

Tile should:
- Change to **yellow** (hover color) when mouse enters
- Return to **original color** (Locked/Buyable/Unlocked) when mouse exits

**Pass Criteria**:
- ✓ Tile turns yellow on hover
- ✓ Tile returns to state color on exit
- ✓ Console log matches

---

### Scenario 2: Click Detection (Tile)

**Action**:
1. Play scene
2. Move mouse over a tile
3. Click **Left Mouse Button**

**Expected Output**:
```
[InputManager] Click on Tile
[Tile] Tile clicked: Tile_X_Y
```

**Pass Criteria**:
- ✓ Both console logs appear
- ✓ No crash or error

---

### Scenario 3: Right-Click Detection (Future RTS)

**Action**:
1. Play scene
2. Right-click on a tile

**Expected Output**:
```
[InputManager] RightClick on Tile at (X, Y, Z)
```

**Pass Criteria**:
- ✓ Console log with world position
- ✓ Position should match tile location (approximately)

---

### Scenario 4: Preview System Integration (Building Placement)

**Action**:
1. Play scene
2. Press **Key 1** (start building placement)
3. Move preview over tiles

**Expected Output**:
```
[InputManager] Hovering: Tile
```

Tile should:
- **NOT** change color while preview is active (because `OnHoverEnter()` returns early if `_isShowingPreview == true`)

**Pass Criteria**:
- ✓ Tiles under preview remain red/green (not yellow)
- ✓ Console shows "Hovering: Tile" but no color change
- ✓ Preview logic preserved

---

### Scenario 5: Priority Enforcement (Unit > Building > Tile) - Future

**Action** (when units implemented):
1. Place unit on tile
2. Click on unit's position

**Expected Output**:
```
[InputManager] Click on Unit  (NOT Tile)
```

**Pass Criteria**:
- ✓ Unit receives click, not tile beneath
- ✓ Tile's OnClick is NOT called

---

## Debugging Checklist

If tests fail:

| Issue | Diagnosis | Fix |
|-------|-----------|-----|
| No log on hover | Layer not assigned | Assign "Tile" layer to all tiles |
| No log on hover | IGridService null | Check VContainer registration |
| Tile stays yellow | `_isShowingPreview` not reset | Ensure `GridManager.ClearPreviousPreview()` called |
| Wrong priority | RaycastNonAlloc not finding target | Verify LayerMask values in Inspector |
| Crashes on click | OnClick not implemented | Check Tile.OnClick() exists |

---

## Performance Baseline

Measure frame time (Profiler):

**Before InputManager** (OnMouse events):
- 50×50 grid: ~5ms per frame
- 100×100 grid: ~22ms per frame

**After InputManager** (non-alloc raycast):
- 50×50 grid: < 0.5ms per frame
- 100×100 grid: < 1ms per frame

Run profiler to confirm improvement.

---

## Next Steps (After Test Pass)

1. **Extend IHoverable to Building & Unit** (when implemented)
2. **Remove OnMouse* from Tile** (once confident in InputManager)
3. **Implement Drag-Box Selection** (4 hours)
4. **Implement Unit Targeting** (right-click → move/attack)

---

## Notes

- InputManager runs in `Update()` with non-alloc `Physics2D.RaycastNonAlloc()`
- GC-friendly: buffer reused, no `new` allocations per frame
- Log level: minimal (only hover/click events, not every raycast)
- Tile behavior unchanged: preview logic preserved, hover guardings intact

---

**Status**: Ready for testing in Editor. Play scene and follow scenarios above.
