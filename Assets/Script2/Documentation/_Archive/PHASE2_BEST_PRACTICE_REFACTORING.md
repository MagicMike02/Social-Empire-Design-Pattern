# 🎯 PHASE 2 BEST PRACTICE REFACTORING

**Data inizio**: 2026-01-24  
**Status**: 🚧 IN PROGRESS  

---

## 📋 OBIETTIVO

Refactoring dell'architettura da **Singleton pattern** a **Dependency Injection** seguendo le best practice moderne.

---

## ✅ COSA È STATO FATTO

### 1️⃣ Installato VContainer (DI Container)

**File modificato**: `Packages/manifest.json`

```json
"jp.hadashikick.vcontainer": "1.15.4"
```

**Why**: VContainer è il DI container più performante per Unity, più leggero di Zenject.

---

### 2️⃣ Creato BuildingEventBus

**File nuovo**: `Assets/Script2/BuildingSystem/BuildingEventBus.cs`

**Sostituisce**: `BuildingEvents` static class

**Vantaggi**:
- ✅ Auto-cleanup quando GameObject distrutto
- ✅ No memory leaks
- ✅ Testabile (mockable)
- ✅ No EventCleanupManager necessario

**Come usare**:
```csharp
// Invece di:
BuildingEvents.OnBuildingPlaced?.Invoke(building);

// Ora:
_eventBus.RaiseBuildingPlaced(building);
```

---

### 3️⃣ Creato GameLifetimeScope

**File nuovo**: `Assets/Script2/Core/GameLifetimeScope.cs`

**Purpose**: DI Container che registra tutti i sistemi

**Registrazioni**:
- `GameEconomyManager` → Iniettabile
- `GridManager` → Iniettabile come `IGridService`
- `TileManager` → Iniettabile
- `ZoneManager` → Iniettabile
- `BuildingManager` → Iniettabile
- `BuildingFactory` → Iniettabile
- `BuildingEventBus` → Iniettabile
- `CameraController` → Iniettabile

**Come usare**:
1. Crea GameObject in scena: `[DI] GameLifetimeScope`
2. Aggiungi componente `GameLifetimeScope`
3. Tutti i sistemi saranno iniettati automaticamente

---

### 4️⃣ Refactoring BuildingPlacer

**File modificato**: `Assets/Script2/BuildingSystem/BuildingPlacer.cs`

**Prima** (Singleton + GetComponent):
```csharp
[SerializeField] private BuildingManager _manager;
[SerializeField] private Camera _camera;

private void Awake()
{
    if (_manager == null) _manager = GetComponent<BuildingManager>();
    if (_camera == null) _camera = Camera.main;
    ValidateDependencies();
}

BuildingEvents.OnBuildingPlaced?.Invoke(building);
```

**Dopo** (Dependency Injection):
```csharp
[Inject]
public void Construct(
    BuildingManager manager,
    Camera camera,
    GenericPreviewSystem previewSystem,
    BuildingEventBus eventBus)
{
    _manager = manager;
    _camera = camera;
    _previewSystem = previewSystem;
    _eventBus = eventBus;
}

_eventBus.RaiseBuildingPlaced(building);
```

**Benefici**:
- ✅ No GetComponent() calls
- ✅ No ValidateDependencies() needed
- ✅ Chiare dipendenze nel constructor
- ✅ Container garantisce dipendenze all'avvio

---

## 🚧 TODO (Prossimi Passi)

### Refactoring rimanenti:

- [ ] **Building.cs** - Usa EventBus per OnBuildingDestroyed
- [ ] **GameEconomyManager** - Rimuovi Singleton pattern
- [ ] **GridManager** - Rimuovi Singleton pattern
- [ ] **ResourceManager** - Dependency Injection
- [ ] **ZoneManager** - Dependency Injection
- [ ] **CameraController** - Rimuovi Camera.main (inject)

### Cleanup:

- [ ] **EventCleanupManager.cs** - Eliminare (non serve più!)
- [ ] **BuildingEvents static class** - Deprecare o eliminare
- [ ] Singleton pattern nei manager - Rimuovere Instance property

---

## 📊 PROGRESSO

| Sistema | Status | Note |
|---------|--------|------|
| **VContainer Setup** | ✅ DONE | Manifest + GameLifetimeScope |
| **BuildingEventBus** | ✅ DONE | Sostituisce static events |
| **BuildingPlacer** | ✅ DONE | DI refactoring completo |
| **Building.cs** | 🚧 TODO | Usa EventBus |
| **GameEconomyManager** | 🚧 TODO | Rimuovi Singleton |
| **GridManager** | 🚧 TODO | Rimuovi Singleton |
| **ResourceManager** | 🚧 TODO | DI integration |
| **ZoneManager** | 🚧 TODO | DI integration |

**Progress**: 30% completato

---

## 🎓 COME FUNZIONA DEPENDENCY INJECTION

### Prima (Singleton):
```csharp
var economy = GameEconomyManager.Instance; // ❌ Tight coupling
economy.AddResource(ResourceType.Gold, 10);
```

### Dopo (DI):
```csharp
public class MyClass : MonoBehaviour
{
    private GameEconomyManager _economy;
    
    [Inject]
    public void Construct(GameEconomyManager economy)
    {
        _economy = economy; // ✅ Injected dependency
    }
    
    public void DoSomething()
    {
        _economy.AddResource(ResourceType.Gold, 10);
    }
}
```

**Vantaggi**:
- ✅ Testabile (puoi iniettare un mock)
- ✅ Chiare dipendenze (vedi nel constructor)
- ✅ No initialization order problems
- ✅ Scalabile (aggiungi dipendenze facilmente)

---

## 🔧 SETUP UNITY EDITOR

Quando riapri Unity:

1. **Attendi che VContainer si installi** (Unity scarica il package)
2. **Se dà errore "VContainer not found"**:
   - Window → Package Manager
   - Click "+" → Add package from git URL
   - Inserisci: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.15.4`

3. **Crea GameObject per DI**:
   - Hierarchy → Create Empty
   - Rinomina: `[DI] GameLifetimeScope`
   - Add Component → GameLifetimeScope

4. **Verifica setup**:
   - Premi Play
   - Controlla Console: "Dependency Injection configurato con successo!"

---

## 🎯 NEXT ACTIONS

Continua il refactoring con:
1. Building.cs refactoring
2. GameEconomyManager Singleton removal
3. GridManager Singleton removal

**Estimated time**: 2-3 ore

---

**Last Updated**: 2026-01-24  
**Author**: Copilot + Team  

