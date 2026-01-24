# 🎯 SETUP UNITY - DEPENDENCY INJECTION

**Guida rapida per configurare VContainer in Unity Editor**

---

## STEP 1: Crea GameObject GameLifetimeScope

1. **Hierarchy** → Click destro → **Create Empty**
2. Rinomina: `[DI] GameLifetimeScope`
3. **Add Component** → Cerca `GameLifetimeScope`
4. Aggiungi il componente

✅ **Fatto!** Questo GameObject gestisce tutte le dependency injection.

---

## STEP 2: Crea GameObject BuildingEventBus

1. **Hierarchy** → Click destro → **Create Empty**
2. Rinomina: `[Events] BuildingEventBus`
3. **Add Component** → Cerca `BuildingEventBus`
4. Aggiungi il componente

✅ **Fatto!** Questo sostituisce BuildingEvents static class.

---

## STEP 3: Aggiungi GenericPreviewSystem

Sul GameObject che ha **BuildingManager**:

1. Seleziona il GameObject `[Manager] Building` (o come l'hai chiamato)
2. **Add Component** → Cerca `GenericPreviewSystem`
3. Aggiungi il componente

✅ **Fatto!** Ora BuildingPlacer riceverà questo componente via DI.

---

## STEP 4: Verifica Hierarchy

La tua scena dovrebbe avere questi GameObject:

```
Hierarchy:
├── [DI] GameLifetimeScope        ← Nuovo! (VContainer)
├── [Events] BuildingEventBus     ← Nuovo! (EventBus)
├── [Manager] Economy             (GameEconomyManager)
├── [Manager] Grid                (GridManager, TileManager, ZoneManager)
├── [Manager] Building            (BuildingManager, BuildingFactory, GenericPreviewSystem)
├── [Manager] Camera              (CameraController)
└── ... altri GameObject
```

---

## STEP 5: Premi Play e Verifica

1. **Premi Play** ▶️
2. **Controlla Console**:
   - ✅ Dovrebbe apparire: `[GameLifetimeScope] Dependency Injection configurato con successo!`
   - ❌ Se ci sono errori, leggi sotto

---

## 🐛 TROUBLESHOOTING

### Errore: "Cannot find component X in hierarchy"

**Causa**: Un componente registrato in GameLifetimeScope non è presente in scena.

**Soluzione**: 
1. Apri `GameLifetimeScope.cs`
2. Guarda quali componenti sono registrati con `RegisterComponentInHierarchy`
3. Verifica che TUTTI siano presenti nella scena come GameObject

**Esempio**:
```csharp
builder.RegisterComponentInHierarchy<GameEconomyManager>()
```
→ Devi avere un GameObject in scena con componente `GameEconomyManager`

---

### Errore: "VContainer namespace not found"

**Causa**: VContainer non è installato correttamente.

**Soluzione**:
1. **Window** → **Package Manager**
2. Cerca "VContainer" nella lista
3. Se non c'è:
   - Click "+" → "Add package from git URL"
   - Incolla: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.15.4`
   - Click "Add"

---

### Errore: "BuildingPlacer has null dependencies"

**Causa**: BuildingPlacer non riceve le dependency injection.

**Soluzione**:
1. Verifica che `[DI] GameLifetimeScope` sia in scena
2. Verifica che tutti i componenti necessari siano in scena:
   - BuildingManager
   - Camera (Main Camera)
   - GenericPreviewSystem
   - BuildingEventBus

---

## ✅ COME VERIFICARE CHE FUNZIONA

1. **Premi Play**
2. **Seleziona BuildingPlacer** nell'Inspector
3. **Guarda i campi privati** (se Debug mode attivo):
   - `_manager` dovrebbe essere **non null** ✅
   - `_camera` dovrebbe essere **non null** ✅
   - `_previewSystem` dovrebbe essere **non null** ✅
   - `_eventBus` dovrebbe essere **non null** ✅

Se sono tutti non-null → **VContainer funziona perfettamente!** 🎉

---

## 🎓 COSA È CAMBIATO

### Prima (Singleton):
```csharp
// Nel codice:
var economy = GameEconomyManager.Instance;

// In scena:
Nessun setup speciale necessario
```

### Dopo (Dependency Injection):
```csharp
// Nel codice:
[Inject]
public void Construct(GameEconomyManager economy)
{
    _economy = economy;
}

// In scena:
[DI] GameLifetimeScope deve essere presente
Tutti i manager devono essere GameObject in scena
```

**Vantaggi**:
- ✅ No Singleton pattern
- ✅ No initialization order problems
- ✅ Testabile (mock dependencies)
- ✅ Chiare dipendenze

---

**Setup completato!** Se tutto è corretto, il gioco funziona esattamente come prima, ma con architettura migliore! 🚀

