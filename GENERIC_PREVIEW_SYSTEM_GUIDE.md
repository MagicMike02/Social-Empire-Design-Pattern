# 🎯 GENERICPREVIEWSYSTEM - SISTEMA RIUTILIZZABILE

## ✅ RISPOSTA ALLA TUA DOMANDA

**PreviewSystem attuale**: ❌ NON è generico (accoppiato a BuildingSystem)  
**GenericPreviewSystem nuovo**: ✅ **COMPLETAMENTE GENERICO E RIUTILIZZABILE**

---

## 📊 CONFRONTO

| Aspetto | PreviewSystem (OLD) | GenericPreviewSystem (NEW) |
|---------|---------------------|----------------------------|
| **Namespace** | BuildingSystem ❌ | Common ✅ |
| **Tipo supportati** | Solo MeshRenderer ❌ | MeshRenderer + SpriteRenderer ✅ |
| **Coordinate** | Vector3Int (griglia) ❌ | Vector3 (world generico) ✅ |
| **Stati** | Valid/Invalid ❌ | Valid/Invalid/Neutral ✅ |
| **Colori custom** | No ❌ | Sì ✅ |
| **Rotazione/Scala** | No ❌ | Sì ✅ |
| **Riutilizzabile** | Solo edifici ❌ | Qualsiasi GameObject ✅ |

---

## 🎮 ESEMPI DI UTILIZZO

### 1. EDIFICI (Caso attuale)

```csharp
using Script2.Common;

public class BuildingPlacer : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _previewSystem;
    
    void Update()
    {
        if (isPlacing)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            bool isValid = CanPlaceBuilding(worldPos);
            
            _previewSystem.ShowPreview(buildingPrefab, worldPos, isValid);
        }
    }
    
    void Confirm()
    {
        _previewSystem.HidePreview();
    }
}
```

---

### 2. UNITÀ (Strategia RTS)

```csharp
using Script2.Common;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _unitPreview;
    
    void ShowUnitPreview(GameObject unitPrefab, Vector3 spawnPoint)
    {
        // Preview neutro (nessuna validazione)
        _unitPreview.ShowPreview(unitPrefab, spawnPoint, null);
        
        // Ruota verso direzione
        _unitPreview.SetRotation(Quaternion.LookRotation(direction));
    }
    
    void ValidateSpawnPoint(Vector3 point)
    {
        bool canSpawn = CheckTerrainWalkable(point) && !HasEnemies(point);
        _unitPreview.SetValidationState(canSpawn);
    }
}
```

---

### 3. RISORSE (Sistema Raccolta)

```csharp
using Script2.Common;

public class ResourcePlacer : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _resourcePreview;
    
    void PlaceResourceNode(ResourceType type)
    {
        Vector3 pos = GetMousePosition();
        GameObject prefab = GetResourcePrefab(type);
        
        bool validTerrain = IsValidTerrainForResource(type, pos);
        _resourcePreview.ShowPreview(prefab, pos, validTerrain);
    }
    
    void OnConfirm()
    {
        if (_resourcePreview.HasActivePreview)
        {
            // Spawn risorsa reale
            SpawnResource();
            _resourcePreview.HidePreview();
        }
    }
}
```

---

### 4. DECORAZIONI (Level Editor)

```csharp
using Script2.Common;

public class DecorationTool : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _decorPreview;
    
    void Update()
    {
        if (editMode)
        {
            Vector3 pos = SnapToGrid(mousePosition);
            
            // Preview neutro (sempre valido)
            _decorPreview.ShowPreview(currentDecoration, pos, null);
            
            // Scala dinamica
            float scale = Input.GetAxis("Mouse ScrollWheel");
            _decorPreview.SetScale(Vector3.one * scale);
            
            // Rotazione
            if (Input.GetKey(KeyCode.R))
            {
                _decorPreview.SetRotation(Quaternion.Euler(0, rotationAngle, 0));
            }
        }
    }
}
```

---

### 5. TORRE DIFESA (Tower Defense)

```csharp
using Script2.Common;

public class TowerPlacer : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _towerPreview;
    
    void ShowTowerRange()
    {
        Vector3 pos = GetMousePosition();
        bool canPlace = IsValidTowerSpot(pos) && HasEnoughMoney();
        
        _towerPreview.ShowPreview(towerPrefab, pos, canPlace);
        
        // Mostra area di attacco
        if (canPlace)
        {
            ShowRangeIndicator(pos, towerRange);
        }
    }
}
```

---

### 6. TILE PAINTER (Tilemap Editor)

```csharp
using Script2.Common;

public class TilePainter : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _tilePreview;
    
    void PaintTile()
    {
        Vector3Int gridPos = GetGridPosition();
        Vector3 worldPos = tilemap.CellToWorld(gridPos);
        
        // Preview tile 2D (SpriteRenderer)
        _tilePreview.ShowPreview(tilePrefab, worldPos, null);
        
        // Configura per 2D
        _tilePreview.SetYOffset(0.01f); // Minimo offset
    }
}
```

---

### 7. WAYPOINT SYSTEM (Pathfinding)

```csharp
using Script2.Common;

public class WaypointEditor : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _waypointPreview;
    
    void PlaceWaypoint()
    {
        Vector3 pos = GetGroundPosition();
        
        // Preview con colore custom
        _waypointPreview.ShowPreview(waypointPrefab, pos, null);
        _waypointPreview.SetCustomColor(Color.cyan);
        
        // Verifica connessione al path
        bool connectedToPath = HasNearbyWaypoints(pos);
        if (!connectedToPath)
        {
            _waypointPreview.SetCustomColor(Color.yellow); // Warning
        }
    }
}
```

---

### 8. PARTICLE EFFECT PLACER (VFX Editor)

```csharp
using Script2.Common;

public class EffectPlacer : MonoBehaviour
{
    [SerializeField] private GenericPreviewSystem _effectPreview;
    
    void PlaceEffect()
    {
        Vector3 pos = GetMousePosition();
        
        // Preview sempre neutro (effetti decorativi)
        _effectPreview.ShowPreview(effectPrefab, pos, null);
        _effectPreview.SetNeutralState();
        
        // Scala in base a intensità
        float intensity = GetIntensitySlider();
        _effectPreview.SetScale(Vector3.one * intensity);
    }
}
```

---

## 🔧 CONFIGURAZIONE AVANZATA

### Configurazione Personalizzata
```csharp
// Setup per edifici
previewSystem.SetPreviewName("Building_Preview");
previewSystem.SetYOffset(0.1f);
previewSystem.SetValidationColors(
    Color.green,    // Valid
    Color.red,      // Invalid
    Color.white     // Neutral
);

// Setup per unità 2D
previewSystem.SetPreviewName("Unit_Preview");
previewSystem.SetYOffset(0.01f); // Minimo per 2D
previewSystem.SetValidationColors(
    new Color(0, 1, 1, 0.7f),    // Cyan valido
    new Color(1, 0, 1, 0.7f),    // Magenta invalido
    new Color(1, 1, 1, 0.5f)     // Bianco neutro
);

// Setup per risorse
previewSystem.SetPreviewName("Resource_Preview");
previewSystem.SetYOffset(0.05f);
// Usa solo stato neutro
previewSystem.SetNeutralState();
```

---

### Ottimizzazione Anti-Glitch
```csharp
// Update solo se movimento significativo
void Update()
{
    Vector3 newPos = GetMousePosition();
    
    // Aggiorna solo se mosso > 1cm
    bool updated = previewSystem.UpdatePreviewIfMoved(
        newPos, 
        isValid: CanPlace(newPos),
        threshold: 0.01f
    );
    
    if (updated)
    {
        // Esegui logica aggiuntiva solo se necessario
        UpdateUI();
    }
}
```

---

## 📋 API COMPLETA

### Core Methods
```csharp
// Mostra/Aggiorna preview
void ShowPreview(GameObject prefab, Vector3 worldPosition, bool? isValid = null)

// Update ottimizzato (anti-glitch)
bool UpdatePreviewIfMoved(Vector3 worldPosition, bool? isValid = null, float threshold = 0.01f)

// Nascondi
void HidePreview()
```

### State Methods
```csharp
// Stati validazione
void SetValidationState(bool isValid)
void SetNeutralState()
void SetCustomColor(Color color)
```

### Transform Methods
```csharp
void SetRotation(Quaternion rotation)
void SetScale(Vector3 scale)
```

### Configuration Methods
```csharp
void SetYOffset(float offset)
void SetPreviewName(string name)
void SetValidationColors(Color valid, Color invalid, Color? neutral = null)
```

### Properties
```csharp
bool HasActivePreview { get; }
GameObject CurrentPreview { get; }
```

---

## ✅ VANTAGGI

### 1. Completamente Generico
- ✅ Funziona con **qualsiasi GameObject**
- ✅ Supporta **MeshRenderer + SpriteRenderer**
- ✅ Coordinate **world generiche** (non griglia-specifico)

### 2. Riutilizzabile
- ✅ Edifici
- ✅ Unità
- ✅ Risorse
- ✅ Decorazioni
- ✅ Tile
- ✅ Waypoint
- ✅ Effetti VFX
- ✅ Qualsiasi altro GameObject!

### 3. Flessibile
- ✅ Stati: Valid/Invalid/Neutral
- ✅ Colori personalizzati
- ✅ Rotazione e scala
- ✅ Configurazione runtime

### 4. Performante
- ✅ Cache anti-glitch
- ✅ Update condizionali
- ✅ Materiali istanziati corretti
- ✅ Cleanup automatico

---

## 🔄 MIGRAZIONE DA PREVIEWSYSTEM

### Opzione 1: Mantieni Entrambi (Consigliato)
- Usa `PreviewSystem` per edifici (esistente)
- Usa `GenericPreviewSystem` per nuove feature (unità, risorse)

### Opzione 2: Migra Completamente
1. Sostituisci `PreviewSystem` con `GenericPreviewSystem` in BuildingPlacer
2. Adatta le chiamate:
```csharp
// OLD
_previewSystem.UpdatePreviewIfCellChanged(cell, worldPos, isValid);

// NEW
_previewSystem.UpdatePreviewIfMoved(worldPos, isValid, threshold: 0.01f);
```

---

## 📁 STRUTTURA FILE

```
Assets/Script2/
├── Common/
│   └── GenericPreviewSystem.cs ⭐ NUOVO (generico)
│
└── BuildingSystem/
    └── PreviewSystem.cs (specifico edifici)
```

---

## 🎉 CONCLUSIONE

**Sì, ora hai un sistema COMPLETAMENTE GENERICO!**

✅ **GenericPreviewSystem** è riutilizzabile per:
- Edifici
- Unità
- Risorse
- Decorazioni
- Tile
- Waypoint
- Effetti
- **Qualsiasi GameObject!**

**Features principali:**
- ✅ MeshRenderer + SpriteRenderer
- ✅ Colori personalizzabili
- ✅ Rotazione + Scala
- ✅ Stati multipli
- ✅ Cache anti-glitch
- ✅ Cleanup automatico

**Il tuo PreviewSystem originale rimane per compatibilità, ma ora hai anche la versione generica per futuri utilizzi!** 🚀

---

**File creato**: `Assets/Script2/Common/GenericPreviewSystem.cs`  
**Namespace**: `Script2.Common`  
**Status**: ✅ Production-ready

