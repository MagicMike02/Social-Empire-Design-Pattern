# CODE REVIEW: InputManager Integration - Pre-Merge Assessment

**Date**: 2026-01-25  
**Branch**: feature/input-manager  
**Status**: Ready for Selective Migration  

---

## 1. CURRENT STATE: OnMouse* Usage in Codebase

### Affected Components

| Component | Location | OnMouse* Methods | Priority |
|-----------|----------|-----------------|----------|
| **Tile.cs** | GridSystem | OnMouseEnter, OnMouseExit, OnMouseDown | ✅ MIGRATED |
| **ResourceInstance.cs** | ResourceSystem | OnMouseDown (CollectResource) | ⚠️ **NEEDS MIGRATION** |
| **PurchaseSign.cs** | GridSystem | OnMouseEnter, OnMouseExit, OnMouseDown | ⚠️ **NEEDS MIGRATION** |
| **Building.cs** | BuildingSystem | OnMouseDown (future) | 🔮 **FUTURE** |
| **Unit.cs** | (Not yet implemented) | N/A | 🔮 **FUTURE** |

---

## 2. RECOMMENDATION: Phased Migration

### ✅ **PHASE 1 (NOW - Merge to main)**
- InputManager + IHoverable interface implemented ✓
- Tile.cs migrated to IHoverable ✓
- LayerRegistry for centralized layer management ✓
- **Rationale**: Core infrastructure is solid, tested, working

### ⚠️ **PHASE 2 (After merge, separate PR)**
**Migrate ResourceInstance & PurchaseSign**

**Why Not Now?**
1. Risk: More components = higher test surface
2. Testing: Resources need explicit testing (collection feedback, etc.)
3. PurchaseSign: Hover animation must work with new input system
4. Safety: Merge smaller, tested chunks

**Timeline**: Week 1 after main merge (3-4 hours work)

### 🔮 **PHASE 3 (RTS Foundation)**
- Building.cs → IHoverable
- Unit.cs → IHoverable
- Implement drag-box selection

---

## 3. MIGRATION CHECKLIST (For Phase 2)

### ResourceInstance.cs

**Current** (OnMouseDown):
```csharp
private void OnMouseDown()
{
    CollectResource();
}
```

**Changes Needed**:
1. Add `using Script2.InputSystem;`
2. Implement `IHoverable`
3. Add methods: `OnHoverEnter()`, `OnHoverExit()`, `OnClick()`, `OnRightClick(Vector3)`
4. Assign layer: `Resource` (via prefab)
5. Remove OnMouseDown

**Implementation** (20 minutes):
```csharp
public class ResourceInstance : MonoBehaviour, IHoverable
{
    // ...existing fields...
    
    public void OnHoverEnter()
    {
        // Optional: glow effect, outline, etc. (future enhancement)
    }
    
    public void OnHoverExit()
    {
        // Optional: remove effect
    }
    
    public void OnClick()
    {
        CollectResource(); // Same logic as old OnMouseDown
    }
    
    public void OnRightClick(Vector3 worldPosition)
    {
        // Future: right-click commands (if needed)
    }
}
```

### PurchaseSign.cs

**Current** (OnMouseEnter/Exit + OnMouseDown):
```csharp
private void OnMouseDown()
{
    if (_purchaseCost == null) { ... }
    if (_economyManager.CanAfford(_purchaseCost)) { ... }
}

private void OnMouseEnter() { _hovered = true; }
private void OnMouseExit() { _hovered = false; }
```

**Changes Needed**:
1. Add `using Script2.InputSystem;`
2. Implement `IHoverable`
3. Move hover logic to `OnHoverEnter/Exit`
4. Move purchase logic to `OnClick`
5. Assign layer: `ZoneSign` (via prefab)

**Implementation** (20 minutes):
```csharp
public class PurchaseSign : MonoBehaviour, IHoverable
{
    // ...existing fields...
    
    public void OnHoverEnter()
    {
        _hovered = true;
    }
    
    public void OnHoverExit()
    {
        _hovered = false;
    }
    
    public void OnClick()
    {
        // Move OnMouseDown logic here
        if (_purchaseCost == null)
        {
            _zoneManager.PurchaseZone(_zoneCoord);
            return;
        }
        
        if (_economyManager?.CanAfford(_purchaseCost) == true)
        {
            _zoneManager.PurchaseZone(_zoneCoord);
        }
        else
        {
            Debug.Log("Non hai abbastanza risorse per sbloccare questa zona!");
        }
    }
    
    public void OnRightClick(Vector3 worldPosition)
    {
        // Not used
    }
}
```

---

## 4. TESTING PLAN

### Pre-Merge (Current)
- ✅ InputManager raycast works
- ✅ Tile hover detection functional
- ✅ Layer system operational
- ✅ No crashes or null refs

### Post-Merge Phase 2
- Resource collection click
- Resource hover (if visual feedback added)
- PurchaseSign hover animation
- PurchaseSign purchase click
- Layer priority (Resource before Tile)

---

## 5. MERGE DECISION

### **GO/NO-GO: ✅ MERGE NOW**

**Rationale**:
- InputManager is independent, non-breaking
- Tile.cs changes are backward-compatible (OnMouse* still present)
- LayerRegistry is additive (no breaking changes)
- ResourceInstance & PurchaseSign unchanged (still work with OnMouse*)
- Parallel development: Phase 2 can start immediately after merge

**Risk Assessment**: **LOW**
- No removed code, only additions
- Full testability with debug flags
- Easy rollback if needed

---

## 6. MERGE STEPS

1. **Ensure feature branch is current**:
   ```bash
   git checkout feature/input-manager
   git pull origin feature/input-manager
   ```

2. **Create Pull Request** on GitHub:
   - Title: "Feature: Centralized InputManager with IHoverable & LayerRegistry"
   - Description: Link to this review
   - Reviewers: (self-review ok)

3. **Merge to main**:
   ```bash
   git checkout main
   git pull origin main
   git merge --no-ff feature/input-manager
   git push origin main
   ```

4. **Start Phase 2 branch**:
   ```bash
   git checkout -b feature/input-migration-phase2
   ```

---

## 7. DECISION SUMMARY

| Question | Answer |
|----------|--------|
| Is InputManager production-ready? | ✅ Yes |
| Should we migrate Resources/PurchaseSign now? | ⚠️ No (Phase 2) |
| Is it safe to merge? | ✅ Yes |
| Breaking changes? | ❌ No |
| Recommended merge timing? | ✅ Now |

---

**Signed Off**: Proceed to Merge
