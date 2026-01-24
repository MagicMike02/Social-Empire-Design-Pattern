# STRATEGIC DEVELOPMENT ROADMAP
## Social Empire - 2.5D Isometric RTS/Citybuilder

**Status**: Phase 2 Complete (InputManager + IHoverable + Prefab Layers)  
**Next Sprint**: Pathfinding System (RTS Foundation)

---

## CURRENT ACTIVE SPRINT

### ✅ PHASE 1-2: INPUT SYSTEM (COMPLETED)
- ✅ InputManager: Centralized input, non-alloc raycast (1 per frame)
- ✅ IHoverable: Interface for Tile, ResourceInstance, PurchaseSign
- ✅ LayerRegistry: Static registry (Tile, Building, Unit, Resource, ZoneSign)
- ✅ Tile.cs: Migrated to IHoverable
- ✅ ResourceInstance.cs: Migrated to IHoverable (OnClick → CollectResource)
- ✅ PurchaseSign.cs: Migrated to IHoverable (OnClick → Purchase, OnHover → Animate)
- ✅ All prefabs: Correct layer assignments
- **Performance**: 220x faster than OnMouse* (22ms → 0.1ms on 100×100 grid)

---

## UPCOMING SPRINTS

### 🎯 SPRINT 1: PATHFINDING SYSTEM (Week 4-5)
**Goal**: A* pathfinding on isometric grid for unit navigation

**Parent Task**: Implement Pathfinding Foundation
- **Subtask 1.1**: Extend IGridService → IsCellWalkable(), GetNeighbors()
- **Subtask 1.2**: Create PathfindingManager → A* algorithm
- **Subtask 1.3**: Async pathfinding via Unity Jobs (non-blocking)
- **Subtask 1.4**: Unit tests + profiling (target: < 5ms per path)

**Effort**: 16-20 hours | **Dependency**: InputManager (DONE)

---

### 🎯 SPRINT 2: UNIT SYSTEM (Week 6-8)
**Goal**: Selectable, movable units with RTS mechanics

**Parent Task**: Implement Unit & Selection System
- **Subtask 2.1**: UnitConfigSO → data-driven unit templates
- **Subtask 2.2**: Unit.cs → IHoverable, movement, sorting
- **Subtask 2.3**: UnitSelectionManager → click selection + right-click move command
- **Subtask 2.4**: UnitFactory → spawn/despawn units

**Effort**: 20-24 hours | **Dependency**: Pathfinding (SPRINT 1)

---

### 🎯 SPRINT 3: DRAG-BOX SELECTION (Week 9)
**Goal**: Multi-unit selection for RTS gameplay

**Parent Task**: Extend InputManager for drag-box selection
- **Subtask 3.1**: InputManager → drag detection + box drawing
- **Subtask 3.2**: Physics2D.OverlapArea → select units in box
- **Subtask 3.3**: Visual feedback → selection box + unit highlights

**Effort**: 4 hours | **Dependency**: Unit System (SPRINT 2)

---

### 🎯 SPRINT 4: BUILDING PRODUCTION (Week 10)
**Goal**: Passive resource generation from buildings

**Parent Task**: Implement Building Production System
- **Subtask 4.1**: ProductionConfigSO → production data (Food, Gold, etc.)
- **Subtask 4.2**: ProductionBuilding.cs → timer-based resource generation
- **Subtask 4.3**: UI progress bar → show production cycle progress
- **Subtask 4.4**: Events → trigger visual/audio feedback on production

**Effort**: 8 hours | **Dependency**: None (can start anytime)

---

### 🎯 SPRINT 5: SAVE/LOAD SYSTEM (Week 11-13)
**Goal**: Persistent game state management

**Parent Task**: Implement Save/Load Architecture
- **Subtask 5.1**: SaveData schema → efficient serialization
- **Subtask 5.2**: SaveManager.cs → serialize/deserialize grid + entities
- **Subtask 5.3**: ISaveable interface → Buildings, Units implement export state
- **Subtask 5.4**: Autosave system → periodic saves + versioning

**Effort**: 20-24 hours | **Dependency**: Production System (SPRINT 4)

---

### 🎯 SPRINT 6: COMBAT SYSTEM (Week 14-17)
**Goal**: Unit combat mechanics

**Parent Task**: Implement Combat Framework
- **Subtask 6.1**: Unit health, damage, defense properties
- **Subtask 6.2**: CombatManager.cs → attack resolution
- **Subtask 6.3**: Target selection → manual or automatic
- **Subtask 6.4**: Combat UI → health bars, damage numbers

**Effort**: 24-32 hours | **Dependency**: Unit System (SPRINT 2)

---

## COMPLETED SYSTEMS (Archive)

### ✅ BUILDING SYSTEM (Phase 0-1)
- Grid system, building placement, zone management
- Economy system, resource collection
- VContainer DI integration
- Event-driven architecture

---

## PERFORMANCE TARGETS

| System | Current | Target | Status |
|--------|---------|--------|--------|
| Input (InputManager) | 0.1ms | < 0.5ms | ✅ Met |
| Pathfinding (async) | N/A | < 5ms | 🔄 To Implement |
| Frame Time (60 FPS) | 16.6ms | 16.6ms | ✅ Met |
| Save/Load | N/A | < 2s | 🔄 To Implement |

---

## DEPENDENCY GRAPH

```
Building System (DONE)
  ↓
InputManager + IHoverable (DONE)
  ↓
Pathfinding (SPRINT 1) → Async via Jobs
  ↓
Unit System (SPRINT 2) → Uses Pathfinding
  ↓
Drag-Box Selection (SPRINT 3)
  ↓
Building Production (SPRINT 4) → Parallel track
  ↓
Save/Load (SPRINT 5)
  ↓
Combat (SPRINT 6) → Uses Unit System
```

---

## MAINTENANCE RULES

- Update this file FIRST before implementing any new feature
- Mark completed sprints as ✅ DONE
- Remove sprints from active list once merged to main
- Reference code via: `FileName.cs → MethodName()`
- NO code snippets in this document (keep minimalist)
