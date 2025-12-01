# Istruzioni Dettagliate per Git Copilot: Social Empire - Sviluppo Unity

## 1. Panoramica del Progetto
Social Empire è un videogioco basato su Unity che implementa una rigorosa architettura a design pattern per garantire scalabilità e manutenibilità. Il progetto si concentra su meccaniche di gameplay sociale, gestione delle risorse e sistemi di costruzione strategica.

## 2. Stack Tecnologico
- Linguaggio Primario: C# (Unity)
- Motore di Gioco: Unity
- Architettura: Component-based con enfasi sui Design Pattern.

## 3. Direttive di Stile e Convenzioni di Codice
- Utilizzare `[SerializeField]` per campi privati che necessitano di visibilità nell'Inspector.
- Preferire sempre la composizione all'ereditarietà (Composition Over Inheritance).
- Mantenere le classi `MonoBehaviour` focalizzate e lightweight, aderendo al Single Responsibility Principle (SRP).
- Utilizzare le `region` per organizzare il codice all'interno delle classi (es. Fields, Properties, Unity Lifecycle, Public Methods).
- Adottare le ultime funzionalità e best practice di C# (es. `record` types, espressioni switch).
- Evitare l'annidamento profondo; utilizzare gli `early return` per ridurre la complessità.
- Evitare di usare il metodo `Update` inutilmente; preferire l'uso di eventi, delegati o Coroutine quando appropriato.

## 4. Pattern di Design Implementati
L'assistente deve conoscere e applicare questi pattern nel contesto di Unity:
- Singleton: Per i game manager e i sistemi core.
- Observer (Eventi): Per la comunicazione event-driven tra sistemi.
- Factory: Per la creazione e l'instanziazione di oggetti.
- Strategy: Per comportamenti intercambiabili (es. AI, calcolo strategico).
- Command: Per l'accodamento di azioni e funzionalità di undo/redo.
- Decorator: Per estendere dinamicamente le funzionalità degli oggetti.
- State: Per gestire stati di gioco e stati dei personaggi/edifici.
- Flyweight: Per ottimizzare l'uso della memoria con dati condivisi.
- Chain of Responsibility: Per la gestione dell'input o la propagazione di eventi.
- Builder: Per la costruzione passo-passo di oggetti di gioco complessi.

## 5. Linee Guida Specifiche Unity
- Utilizzare ScriptableObjects per un design data-driven e la configurazione del gioco.
- Implementare l'Object Pooling per oggetti frequentemente instanziati.
- Evitare operazioni pesanti nei loop di `Update()`/`FixedUpdate()`.
- Sfruttare Coroutines o la programmazione `async/await` (con il dispatcher appropriato) per operazioni I/O o calcoli a tempo.
- Utilizzare `MonoBehaviour.CompareTag()` invece del confronto diretto di stringhe per i tag.
- Assicurare che il codice sia compatibile con i diversi cicli di vita di Unity (`Awake`, `Start`, `OnDestroy`, ecc.).

## 6. FOCUS PRIMARIO: Refactoring e Coerenza del Codice

Questa sezione definisce l'approccio alla manutenibilità e all'aggiornamento interconnesso.

- Priorità di Refactoring: Concentrarsi su violazioni del Single Responsibility Principle (SRP), classi `MonoBehaviour` sovradimensionate, e miglioramento della struttura dei pattern implementati.
- Goal del Refactoring: Aumentare la leggibilità, l'estensibilità e l'aderenza ai principi SOLID.
- Direttiva Interconnessioni (Controlli a Catena): Quando si modifica la firma (nome, parametri, tipo di ritorno) di un membro pubblico, Copilot DEVE SUGGERIRE la modifica in TUTTI i punti correlati (Controlli a Catena), inclusi:
    - Tutte le classi chiamanti e le loro istanze (es. un cambio su un metodo Singleton).
    - Le interfacce che definiscono il membro.
    - Le classi derivate o le sottoclassi che lo implementano.
    - Qualsiasi riferimento di Unity (es. eventi nell'Inspector) non gestibile dal codice.
- Sostituzione di Pattern: Suggerire transizioni verso un'architettura più testabile (es. Dependency Injection) se i pattern esistenti (es. Singleton) causano accoppiamento eccessivo.

## 7. FOCUS SECONDARIO: Performance, Analisi degli Errori e Debugging

Questa sezione fornisce le istruzioni operative per l'identificazione e la correzione di bug e colli di bottiglia.

- Analisi delle Performance (Frequenti Bottleneck):
    - Evitare Allocazioni Costose: Identificare e suggerire la rimozione di allocazioni di memoria (es. `new`, stringhe, collezioni) all'interno dei cicli `Update()` per ridurre il Garbage Collector.
    - Caching: Assicurare che `GetComponent<T>()` e altre chiamate costose di accesso ai componenti siano chiamate solo una volta in `Awake()` o `Start()` e che il risultato sia *cachingizzato*.
    - Operazioni Asincrone: Analizzare metodi che potrebbero bloccare il thread principale (operazioni I/O, calcoli complessi) e suggerire l'uso di Coroutine o l'offload su thread separati.
    - Logging: Limitare il logging eccessivo in build di produzione (`#if UNITY_EDITOR`).
- Errori e Robustezza del Codice:
    - Controlli Null: Verificare l'aggiunta di controlli di nullità (es. `?.` o `is null`) per riferimenti critici (componenti, ScriptableObjects) prima dell'accesso.
    - Gestione degli Eventi (Observer Pattern): Garantire che tutti i gestori di eventi (delegati) siano correttamente *disiscritti* (rimossi) in `OnDestroy()` per prevenire Memory Leaks.
    - Prevenzione Bug: Identificare potenziali *race condition* in sistemi multi-thread o multi-coroutine.

## 8. Aspettative di Risposta (Output Style)
L'obiettivo è fornire assistenza in modo conciso e direttivo.

- Modalità Indicativa (Istruzione Cruciale): Non scrivere l'intero blocco di codice rifattorizzato o corretto. L'assistente deve indicare chiaramente la riga, il metodo o la classe da modificare e fornire solo gli *estratti* essenziali della modifica (es. la nuova firma del metodo o la linea di codice da sostituire).
- Tracciamento Modifiche: Per modifiche interconnesse che coinvolgono più file, l'assistente deve elencare i nomi dei file e delle classi che necessitano di aggiornamento per completare la modifica a catena.
- Contesto: Tutte le raccomandazioni devono considerare l'architettura component-based, i design pattern implementati e le performance ottimizzate per un gioco Unity.

## 9. Focus Aree
- Implementazione delle meccaniche di gioco sociale e strategico.
- Integrazione e corretta applicazione dei Design Pattern.
- Ottimizzazione delle Performance e riduzione delle allocazioni di memoria.
- Manutenibilità e scalabilità del codice.
- Aderenza alle Unity best practices.