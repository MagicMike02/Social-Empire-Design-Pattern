# STRATEGIC DEVELOPMENT ROADMAP
## Social Empire - RTS/Citybuilder

**Status**: Phase 2 Complete (InputManager + IHoverable live)  
**Current Sprint**: Sprint 1 - Pathfinding System  
**Updated**: 2026-01-25  

---

## ACTIVE SPRINT: PATHFINDING SYSTEM (Week 4-5)

**Goal**: A* pathfinding on isometric grid for unit movement (async, non-blocking)

**Parent Task**: Implement Pathfinding Foundation
- **Subtask 1.1**: Extend IGridService → IsCellWalkable(), GetNeighbors()
- **Subtask 1.2**: Create PathfindingManager → A* algorithm
- **Subtask 1.3**: Async execution via Unity Jobs
- **Subtask 1.4**: Integration testing

**Effort**: 16-20 hours | **Dependency**: InputManager (DONE) | **Blocks**: Unit System, Drag-Box

---

## SPRINT 2: UNIT SYSTEM (Week 6-8)

**Goal**: Selectable, movable units with click + right-click commands

**Parent Task**: Implement Unit & Selection System
- **Subtask 2.1**: UnitConfigSO data asset
- **Subtask 2.2**: Unit.cs component (IHoverable)
- **Subtask 2.3**: UnitSelectionManager
- **Subtask 2.4**: UnitFactory

**Effort**: 20-24 hours | **Dependency**: Pathfinding | **Blocks**: Drag-Box, Combat

---

## SPRINT 3: DRAG-BOX SELECTION (Week 9)

**Goal**: Multi-unit selection via mouse drag

**Parent Task**: Extend InputManager for drag selection
- **Subtask 3.1**: Drag detection in InputManager
- **Subtask 3.2**: Area-based unit selection
- **Subtask 3.3**: Selection feedback

**Effort**: 4 hours | **Dependency**: Unit System

---

## SPRINT 4: BUILDING PRODUCTION (Week 10)

**Goal**: Passive resource generation from buildings

**Parent Task**: Production System
- **Subtask 4.1**: ProductionConfigSO
- **Subtask 4.2**: ProductionBuilding component
- **Subtask 4.3**: Integration

**Effort**: 8 hours | **Dependency**: None (parallel track)

---

## SPRINT 5: SAVE/LOAD SYSTEM (Week 11-13)

**Goal**: Persistent game state (grid, buildings, units, resources)

**Parent Task**: Serialization Architecture
- **Subtask 5.1**: SaveData schema (delta compression)
- **Subtask 5.2**: SaveManager
- **Subtask 5.3**: ISaveable interface
- **Subtask 5.4**: Autosave (5 min intervals)

**Effort**: 20-24 hours | **Dependency**: None (parallel track)

---

## SPRINT 6: COMBAT SYSTEM (Week 14-17)

**Goal**: Unit combat mechanics (health, damage, attack range)

**Parent Task**: Combat Framework
- **Subtask 6.1**: Unit combat stats
- **Subtask 6.2**: CombatManager
- **Subtask 6.3**: UI feedback

**Effort**: 24-32 hours | **Dependency**: Unit System

---

## COMPLETED PHASES

✅ **Phase 0-1**: Building System (placement, validation, preview, zones, economy)  
✅ **Phase 2**: InputManager + IHoverable (centralized raycasting, no OnMouse*)  
✅ **Infrastructure**: VContainer DI, event architecture, resource pooling

---

## DEPENDENCY GRAPH

```
Building System (DONE)
  ↓
InputManager (DONE)
  ↓
Pathfinding (SPRINT 1) → async via Jobs
  ↓
Unit System (SPRINT 2)
  ├→ Drag-Box (SPRINT 3)
  └→ Combat (SPRINT 6)

Building Production (SPRINT 4) → parallel
Save/Load (SPRINT 5) → parallel
```

---

**Maintenance**: Update BEFORE implementing. No code snippets (see PROJECT_KNOWLEDGE_BASE.md). Keep JIRA format.
