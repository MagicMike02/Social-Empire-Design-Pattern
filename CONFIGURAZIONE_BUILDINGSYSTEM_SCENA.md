# 🎮 CONFIGURAZIONE BUILDINGSYSTEM SULLA SCENA - GUIDA PASSO-PASSO

## 📋 PANORAMICA

Dopo il refactoring e consolidamento, ecco come configurare correttamente il BuildingSystem nella tua scena Unity.

---

## 🏗️ STRUTTURA GERARCHIA SCENA

### Setup Completo Consigliato

```
Scene Hierarchy:
│
├─ 🎯 GridManager (GameObject)
│  ├─ GridManager (Component)
│  ├─ TileManager (Component)
│  └─ ZoneManager (Component)
│
├─ 💰 GameEconomyManager (GameObject)
│  └─ GameEconomyManager (Component)
│
├─ 🏢 BuildingSystem (GameObject) ← QUESTO È IL PRINCIPALE
│  ├─ BuildingManager (Component)
│  ├─ BuildingFactory (Component)
│  ├─ BuildingPlacer (Component)
│  ├─ PreviewSystem (Component) ← OPZIONE A: Preview specifico edifici
│  │   └─── OPPURE ───
│  ├─ GenericPreviewSystem (Component) ← OPZIONE B: Preview generico
│  └─ KeyboardPlacementInput (Component)
│
└─ 📷 Main Camera
```

---

## ⚙️ CONFIGURAZIONE PASSO-PASSO

### STEP 1: Crea GameObject BuildingSystem

1. **Hierarchy** → Click destro → `Create Empty`
2. Rinomina in **"BuildingSystem"**
3. Posizione: `(0, 0, 0)`

---

### STEP 2: Aggiungi Componenti BuildingSystem

Seleziona "BuildingSystem" e aggiungi questi componenti in ordine:

#### A) BuildingManager
```
Add Component → Script2.BuildingSystem → BuildingManager
```

**Inspector - BuildingManager:**
```
┌─ Building Manager ─────────────────────────┐
│ Root:                                      │
│   ⚪ None (Transform)                      │ ← Lascia vuoto (usa stesso GameObject)
│                                            │
│ Factory:                                   │
│   ⚪ None (BuildingFactory)                │ ← Auto-trovato dopo Step B
└────────────────────────────────────────────┘
```

#### B) BuildingFactory
```
Add Component → Script2.BuildingSystem → BuildingFactory
```

**Inspector - BuildingFactory:**
- Nessuna configurazione richiesta ✅

#### C) BuildingPlacer
```
Add Component → Script2.BuildingSystem → BuildingPlacer
```

**Inspector - BuildingPlacer:**
```
┌─ Building Placer ──────────────────────────┐
│ Dependencies                               │
│   Manager:                                 │
│     ⚪ None (BuildingManager)              │ ← Auto-trovato
│                                            │
│   Camera:                                  │
│     ⚪ None (Camera)                        │ ← Auto-trovato (Camera.main)
│                                            │
│   Preview System:                          │
│     ⚪ None (PreviewSystem)                 │ ← DA CONFIGURARE in Step D
│                                            │
│ State (Debug Only)                         │
│   Selected Config: None                    │
│   Is Placing: false                        │
│   Current Cell: (0, 0, 0)                  │
└────────────────────────────────────────────┘
```

---

### STEP 3: Scegli Sistema Preview

Hai **2 OPZIONI**:

---

#### **OPZIONE A: PreviewSystem** (Specifico Edifici) ⭐ CONSIGLIATO PER ORA

**Quando usarlo:**
- Solo per edifici
- Sistema già testato e funzionante
- Già integrato con BuildingPlacer

**Configurazione:**

1. Aggiungi componente:
```
Add Component → Script2.BuildingSystem → PreviewSystem
```

2. **Inspector - PreviewSystem:**
```
┌─ Preview System ───────────────────────────┐
│ Preview Materials                          │
│   Valid Preview Material:                  │
│     ⚪ None (Material)                      │ ← Opzionale (usa default)
│                                            │
│   Invalid Preview Material:                │
│     ⚪ None (Material)                      │ ← Opzionale (usa default)
│                                            │
│ Preview Settings                           │
│   Preview Y Offset: 0.05                   │ ← Offset anti z-fighting
│   Valid Color: RGB(0, 255, 0, 128)         │ ← Verde semitrasparente
│   Invalid Color: RGB(255, 0, 0, 128)       │ ← Rosso semitrasparente
└────────────────────────────────────────────┘
```

3. **Collega a BuildingPlacer:**
   - Seleziona "BuildingSystem"
   - In **BuildingPlacer** → campo "Preview System"
   - Trascina **PreviewSystem** (stesso GameObject)

---

#### **OPZIONE B: GenericPreviewSystem** (Generico) 🆕 FUTURO

**Quando usarlo:**
- Vuoi usare preview anche per unità, risorse, ecc.
- Vuoi maggiore flessibilità
- Sistema riutilizzabile per tutto

**Configurazione:**

1. Aggiungi componente:
```
Add Component → Script2.Common → GenericPreviewSystem
```

2. **Inspector - GenericPreviewSystem:**
```
┌─ Generic Preview System ───────────────────┐
│ Preview Settings                           │
│   Y Offset: 0.05                           │ ← Offset anti z-fighting
│   Valid Color: RGB(0, 255, 0, 128)         │ ← Verde
│   Invalid Color: RGB(255, 0, 0, 128)       │ ← Rosso
│   Neutral Color: RGB(255, 255, 255, 128)   │ ← Bianco (neutro)
│                                            │
│ Advanced                                   │
│   Preview Layer: "Default"                 │ ← Layer preview
│   Disable Colliders: ✅                     │ ← Disabilita collider auto
└────────────────────────────────────────────┘
```

3. **⚠️ NOTA**: Per ora BuildingPlacer usa PreviewSystem specifico
   - Per usare GenericPreviewSystem serve adattare BuildingPlacer
   - Consiglio: **usa OPZIONE A per ora**

---

### STEP 4: KeyboardPlacementInput (Input Temporaneo)

```
Add Component → Script2.BuildingSystem → KeyboardPlacementInput
```

**Inspector - KeyboardPlacementInput:**
```
┌─ Keyboard Placement Input ─────────────────┐
│ Placer:                                    │
│   ⚪ None (BuildingPlacer)                  │ ← Auto-trovato
│                                            │
│ Test Building Config:                      │
│   ⚪ None (BuildingConfigSO) ⚠️ IMPORTANTE  │ ← DA CONFIGURARE in Step 6
└────────────────────────────────────────────┘
```

---

### STEP 5: Verifica GameObject Esistenti

Assicurati che esistano in scena:

#### A) GridManager
```
Hierarchy → Trova "GridManager" GameObject
```

**Inspector - GridManager:**
```
┌─ Grid Manager ─────────────────────────────┐
│ Tile Manager:                              │
│   🟢 TileManager (Component)               │ ← DEVE essere assegnato
│                                            │
│ Zone Manager:                              │
│   🟢 ZoneManager (Component)               │ ← DEVE essere assegnato
│                                            │
│ Resource Spawner:                          │
│   ⚪ ResourceSpawner (Component)            │ ← Opzionale
└────────────────────────────────────────────┘
```

**⚠️ SE NON ESISTE**: Crealo manualmente con TileManager e ZoneManager

---

#### B) GameEconomyManager
```
Hierarchy → Trova "GameEconomyManager" GameObject
```

**Inspector - GameEconomyManager:**
- Nessuna configurazione richiesta ✅
- Singleton auto-inizializzato

**⚠️ SE NON ESISTE**: 
1. Create Empty → Rinomina "GameEconomyManager"
2. Add Component → Script2.Economy → GameEconomyManager

---

### STEP 6: Crea BuildingConfigSO di Test

**IMPORTANTE**: Serve almeno 1 BuildingConfigSO per testare!

#### A) Crea ScriptableObject

1. **Project Window** → Click destro
2. `Create → Script2 → Building → Config`
3. Rinomina: **"TestHouse"** (o altro nome)

#### B) Configura BuildingConfigSO

Seleziona "TestHouse" in Project:

```
┌─ Test House (BuildingConfigSO) ────────────┐
│ Prefab e Visuale                           │
│   Prefab:                                  │
│     🟢 HousePrefab                          │ ← DEVE avere SpriteRenderer!
│                                            │
│   Sorting Layer: "OnTiles"                 │ ← Layer rendering
│   Base Sorting Order: 0                    │ ← Offset sorting
│                                            │
│ Dimensioni sulla griglia (celle)           │
│   Width: 1                                 │ ← Celle in X
│   Height: 1                                │ ← Celle in Y
│                                            │
│ Costo risorse                              │
│   Costs:                                   │
│     Size: 2                                │
│     ├─ Element 0                           │
│     │    Type: Wood                        │
│     │    Amount: 10                        │
│     └─ Element 1                           │
│          Type: Stone                       │
│          Amount: 5                         │
│                                            │
│ Anteprima                                  │
│   Valid Color: RGB(0, 255, 0, 128)         │ ← Verde
│   Invalid Color: RGB(255, 0, 0, 128)       │ ← Rosso
│   Ghost Alpha: 0.6                         │ ← Trasparenza
└────────────────────────────────────────────┘
```

#### C) Crea Prefab Edificio (se non hai)

**Requisito**: Il prefab DEVE avere almeno un **SpriteRenderer** o **MeshRenderer**

**Esempio Prefab House:**
```
HousePrefab (Prefab)
├─ Sprite (GameObject)
│  └─ SpriteRenderer
│     ├─ Sprite: house_sprite
│     ├─ Sorting Layer: "OnTiles"
│     └─ Order in Layer: 0
│
└─ Collider2D (opzionale)
   └─ BoxCollider2D
```

---

### STEP 7: Collega BuildingConfigSO a Input

1. Seleziona GameObject **"BuildingSystem"**
2. In **KeyboardPlacementInput** → campo "Test Building Config"
3. Trascina **TestHouse** (ScriptableObject) dal Project

```
┌─ Keyboard Placement Input ─────────────────┐
│ Test Building Config:                      │
│   🟢 TestHouse (BuildingConfigSO)          │ ← CONFIGURATO ✅
└────────────────────────────────────────────┘
```

---

### STEP 8: Configura Sorting Layers

**Unity Editor** → `Edit → Project Settings → Tags and Layers`

#### A) Aggiungi Sorting Layers

```
Sorting Layers:
├─ Default
├─ Tiles          ← AGGIUNGI QUESTO
└─ OnTiles        ← AGGIUNGI QUESTO (per edifici)
```

#### B) Ordine Consigliato

```
Order:
0. Default
1. Tiles        (tile griglia)
2. OnTiles      (edifici sopra tile)
```

---

## 🎮 TEST FUNZIONAMENTO

### Test Rapido

1. **Play Mode** ▶️
2. **Console** dovrebbe mostrare:
   ```
   [GridManager] Initialized
   [GameEconomyManager] Initialized. Current Resources:
   - Wood: 0
   - Stone: 0
   ...
   ```

3. **Premi tasto "1"** → Dovrebbe:
   - ✅ Apparire preview edificio (ghost)
   - ✅ Ghost segue cursore mouse
   - ✅ Tile mostrano preview verde/rossa

4. **Muovi mouse**:
   - ✅ Su tile SBLOCCATO → Preview VERDE
   - ✅ Su tile BLOCCATO → Preview ROSSA
   - ✅ Ghost fluido senza glitch

5. **Premi "1" di nuovo** → Dovrebbe:
   - ✅ Piazzare edificio (se verde)
   - ⚠️ Mostrare errore se risorse insufficienti

6. **Premi ESC** → Dovrebbe:
   - ✅ Annullare placement
   - ✅ Nascondere ghost

---

## 🐛 TROUBLESHOOTING

### Problema 1: "Instance è null"

**Errore**: `NullReferenceException: GameEconomyManager.Instance`

**Causa**: Script Execution Order non configurato

**Soluzione**:
```
Edit → Project Settings → Script Execution Order:
- GameEconomyManager: -100
- GridManager: -50
- BuildingManager: 0
```

---

### Problema 2: Preview non appare

**Errore**: Premo "1" ma nessun ghost

**Cause possibili**:
1. ❌ Test Building Config non assegnato
2. ❌ Prefab non ha SpriteRenderer
3. ❌ PreviewSystem non collegato a BuildingPlacer

**Soluzione**:
1. Verifica **KeyboardPlacementInput** → "Test Building Config" assegnato
2. Verifica **Prefab** ha SpriteRenderer o MeshRenderer
3. Verifica **BuildingPlacer** → "Preview System" collegato

---

### Problema 3: Colori ancora viola/strani

**Errore**: Preview non verde/rossa ma viola/altro

**Causa**: Tile usa materiale condiviso vecchio

**Soluzione**:
1. Assicurati che il fix **Tile.cs** sia applicato
2. Riavvia Unity per ricaricare materiali
3. Verifica colori in PreviewSystem Inspector

---

### Problema 4: "BuildingFactory non trovato"

**Errore**: `BuildingFactory non trovato!`

**Causa**: BuildingFactory non sullo stesso GameObject

**Soluzione**:
1. Seleziona "BuildingSystem"
2. Verifica che **BuildingFactory** sia presente come componente
3. Se manca, aggiungi: `Add Component → BuildingFactory`

---

### Problema 5: Glitch durante movimento

**Errore**: Preview sfarfalla/flickera

**Causa**: Fix cache non applicato correttamente

**Soluzione**:
1. Verifica che **BuildingPlacer** sia la versione aggiornata (con `_lastCell`)
2. Verifica che **GridManager.SetCellsPreview** sia la versione ottimizzata
3. Controlla Console per altri errori

---

## 📊 CHECKLIST FINALE

### Pre-Play
- [ ] BuildingSystem GameObject creato
- [ ] BuildingManager componente aggiunto
- [ ] BuildingFactory componente aggiunto
- [ ] BuildingPlacer componente aggiunto
- [ ] PreviewSystem componente aggiunto E collegato
- [ ] KeyboardPlacementInput componente aggiunto
- [ ] GridManager esiste in scena con TileManager/ZoneManager
- [ ] GameEconomyManager esiste in scena
- [ ] BuildingConfigSO creato con prefab valido
- [ ] BuildingConfigSO assegnato a KeyboardPlacementInput
- [ ] Sorting Layers "Tiles" e "OnTiles" creati
- [ ] Script Execution Order configurato

### In-Play
- [ ] Console mostra inizializzazione corretta
- [ ] Tasto "1" fa apparire preview
- [ ] Preview segue mouse fluidamente
- [ ] Preview verde su tile validi
- [ ] Preview rossa su tile invalidi
- [ ] Zero flickering/glitch
- [ ] Tasto "1" conferma placement (se risorse OK)
- [ ] ESC annulla placement
- [ ] Nessun errore in Console

---

## 🎯 CONFIGURAZIONE FINALE ESEMPIO

```
BuildingSystem (GameObject)
├─ BuildingManager
│  └─ Root: [vuoto] ✅
│  └─ Factory: [auto-trovato] ✅
│
├─ BuildingFactory ✅
│
├─ BuildingPlacer
│  └─ Manager: [auto-trovato] ✅
│  └─ Camera: [auto-trovato] ✅
│  └─ Preview System: 🟢 PreviewSystem ⚠️ IMPORTANTE
│
├─ PreviewSystem
│  └─ Y Offset: 0.05 ✅
│  └─ Valid Color: Verde ✅
│  └─ Invalid Color: Rosso ✅
│
└─ KeyboardPlacementInput
   └─ Placer: [auto-trovato] ✅
   └─ Test Building Config: 🟢 TestHouse ⚠️ IMPORTANTE
```

---

## 🎉 SUCCESSO!

Se tutti i test passano, il tuo BuildingSystem è configurato correttamente!

**Prossimi passi:**
1. Crea più BuildingConfigSO (case, negozi, etc.)
2. Aggiungi UI per selezione edifici
3. Implementa salvataggio edifici
4. Espandi con nuove feature

---

**Guida creata**: 16 Dicembre 2025  
**Versione BuildingSystem**: 3.0 (Post-Consolidamento)  
**Status**: ✅ Production Ready

🚀 **Buon building!**

