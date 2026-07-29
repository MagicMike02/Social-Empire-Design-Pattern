# Istruzioni Dettagliate per Git Copilot: Social Empire - Sviluppo Unity

## 1. Panoramica del Progetto
Social Empire è un videogioco basato su Unity che implementa una rigorosa architettura a design pattern per garantire scalabilità e manutenibilità. Il progetto si concentra su meccaniche di gameplay sociale, gestione delle risorse e sistemi di costruzione strategica.

## 2. Stack Tecnologico

### Versioni e Target
- **Unity**: 6000.5.2f1 (Unity 6 LTS)
- **C# (LangVersion)**: 9.0
- **Target Framework**: netstandard2.1
- **Pipeline di Rendering**: Universal Render Pipeline (URP) 17.5.0

### Librerie e Pacchetti Principali
| Pacchetto | Versione | Ruolo |
|---|---|---|
| **VContainer** (`jp.hadashikick.vcontainer`) | 1.19.0 | DI/IoC Container — Dependency Injection (da OpenUPM) |
| **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json`) | 3.2.1 | Serializzazione JSON per save system |
| **Unity Input System** | 1.19.0 | Nuovo Input System |
| **Unity Test Framework** | 1.7.0 | Unit testing (EditMode + PlayMode) (futuro indefinito)|
| **Unity UI (uGUI)** | 2.5.0 | UI Toolkit / uGUI |

### Polyfill Necessario
- **`IsExternalInit`** (`Assets/Script/Core/IsExternalInit.cs`): Classe marker vuota in `System.Runtime.CompilerServices`. Necessaria per supportare `record` e `init`-only setters (C# 9) su netstandard2.1. **Non rimuovere.**

### Architettura
- **DI Container**: VContainer — `GameLifetimeScope.cs` in `Core/` è il root del container.
- **Moduli**: BuildingSystem, GridSystem, ResourceSystem, EconomySystem, InputSystem, CameraSystem, PathfindingSystem, UI, Core, Common.
- **Nessun Assembly Definition (.asmdef)**: tutto in `Assembly-CSharp`.

## 3. Direttive di Stile e Convenzioni di Codice
- Utilizzare `[SerializeField]` per campi privati che necessitano di visibilità nell'Inspector.
- Preferire sempre la composizione all'ereditarietà (Composition Over Inheritance).
- Mantenere le classi `MonoBehaviour` focalizzate e lightweight, aderendo al Single Responsibility Principle (SRP).
- Utilizzare le `region` per organizzare il codice all'interno delle classi (es. Fields, Properties, Unity Lifecycle, Public Methods).
- Adottare le ultime funzionalità e best practice di C# (es. `record` types, espressioni switch).
- Evitare l'annidamento profondo; utilizzare gli `early return` per ridurre la complessità.
- Evitare di usare il metodo `Update` inutilmente; preferire l'uso di eventi, delegati o Coroutine quando appropriato.

## 4. Design Pattern Implementati
L'assistente deve conoscere e applicare questi pattern nel contesto di Unity:

| Pattern | Utilizzo nel Progetto |
|---|---|
| **Singleton** | Game manager e sistemi core (via VContainer, nessun singleton statico) |
| **Observer (Eventi)** | Comunicazione event-driven tra sistemi tramite `GlobalEventBus` |
| **Factory** | Creazione e instanziazione oggetti (es. `BuildingFactory`) |
| **Strategy** | Comportamenti intercambiabili (es. AI, `ResourceGenerationStrategy`) |
| **Command** | Accodamento azioni e undo/redo (`ICommand`, `CommandHistory`) |
| **Decorator** | Estensione dinamica funzionalità (es. `CachedPathfindingDecorator`) |
| **State** | Gestione stati di gioco/edifici (es. `IPlacementState`) |
| **Flyweight** | Ottimizzazione memoria con dati condivisi |
| **Chain of Responsibility** | Gestione input e propagazione eventi |
| **Builder** | Costruzione passo-passo di oggetti complessi |

## 5. Linee Guida Specifiche Unity
- Utilizzare ScriptableObjects per un design data-driven e la configurazione del gioco.
- Implementare l'Object Pooling per oggetti frequentemente istanziati.
- Evitare operazioni pesanti nei loop di `Update()`/`FixedUpdate()`.
- Sfruttare Coroutines o `async/await` (con dispatcher appropriato) per operazioni I/O o calcoli a tempo.
- Utilizzare `MonoBehaviour.CompareTag()` invece del confronto diretto di stringhe per i tag.
- Assicurare che il codice sia compatibile con i cicli di vita Unity (`Awake`, `Start`, `OnDestroy`, ecc.).

## 6. Regole Trasversali

### Modifiche a catena
Quando si modifica la firma (nome, parametri, tipo di ritorno) di un membro pubblico, aggiornare TUTTI i punti correlati:
- Classi chiamanti e loro istanze
- Interfacce che definiscono il membro
- Classi derivate o sottoclassi che lo implementano
- Riferimenti Unity Inspector (da segnalare se non gestibili via codice)

### Performance
- **Niente allocazioni in `Update()`**: evitare `new`, stringhe, collezioni nei loop.
- **Cache** `GetComponent<T>()` e chiamate costose in `Awake()`/`Start()`.
- **Operazioni asincrone**: metodi I/O o calcoli pesanti → Coroutine o thread separati.
- **Logging**: limitare in produzione (`#if UNITY_EDITOR`).

### Robustezza
- **Controlli null**: usare `?.` o `is null` per riferimenti critici prima dell'accesso.
- **Eventi**: disiscrivere sempre i delegati in `OnDestroy()` per prevenire memory leak.
- **Race condition**: evitarle in sistemi multi-thread o multi-coroutine.