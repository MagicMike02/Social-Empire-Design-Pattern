# Social Empire - Unity Design Pattern Project

A **Unity 6** (6000.5.2f1 LTS) project demonstrating **design patterns** for scalable and maintainable game development. Focuses on social gameplay mechanics, resource management, and strategic building systems.

---

## 📌 **Project Overview**
- **Engine**: Unity 6 (URP 17.5.0)
- **Language**: C# 9.0 (`netstandard2.1`)
- **Architecture**: Component-based with **SOLID** principles and **Design Patterns** (Singleton, Observer, Factory, Strategy, Command, State, etc.).
- **DI Container**: [VContainer](https://github.com/hadashikick/vcontainer) (1.19.0)
- **Testing**: Unity Test Framework (EditMode + PlayMode)

---

## 🗂️ **Structure**
```
Assets/
├── Script/
│   ├── Core/               # Game lifecycle, DI, and utilities
│   ├── BuildingSystem/     # Building placement and management
│   ├── GridSystem/         # Grid-based world logic
│   ├── ResourceSystem/     # Resource collection and economy
│   ├── EconomySystem/      # Currency and trading
│   ├── InputSystem/        # Input handling (New Input System)
│   ├── CameraSystem/       # Camera controls
│   ├── PathfindingSystem/  # A* pathfinding
│   ├── UI/                 # User interface (uGUI)
│   └── Common/             # Shared utilities and extensions
```

---

## 🛠️ **Setup**
1. **Unity Version**: Open in **Unity 6000.5.2f1** (LTS).
2. **Dependencies**: Install via **OpenUPM** or Unity Package Manager:
   - `com.hadashikick.vcontainer` (VContainer)
   - `com.unity.inputsystem` (Input System)
   - `com.unity.test-framework` (Testing)
   - `com.unity.nuget.newtonsoft-json` (JSON serialization)

3. **Polyfill**: Ensure `IsExternalInit.cs` exists in `Assets/Script/Core/` for C# 9.0 support.

---

## 🚀 **Key Features**
- **Data-Driven Design**: ScriptableObjects for configurations.
- **Object Pooling**: Optimized instantiation for frequent objects.
- **Event-Driven**: Observer pattern for decoupled systems.
- **Performance**: Caching, async operations, and GC allocation minimization.

---

## 📜 **Conventions**
- **Coding**: Follow [C# 9.0](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9) best practices.
- **Unity**: Use `SerializeField` for Inspector-exposed fields, `CompareTag` for tag checks.
- **Patterns**: Prefer **Composition Over Inheritance** and **Single Responsibility Principle (SRP)**.
- **Testing**: Unit tests in `SocialEmpire.Tests.EditMode` (logic) and `SocialEmpire.Tests.PlayMode` (runtime).

---

## 🔍 **Design Patterns Implemented**
| Pattern          | Usage Example                          |
|------------------|----------------------------------------|
| Singleton        | Game managers (e.g., `GameManager`)    |
| Observer         | Event-driven communication             |
| Factory          | Object instantiation (e.g., buildings) |
| Strategy         | AI behavior switching                  |
| Command          | Action queuing (undo/redo)             |
| State            | Game/character state management        |
| Flyweight        | Shared data for memory optimization    |
| Decorator        | Dynamic functionality extension        |
| Chain of Responsibility | Input/event propagation       |

---

## 📝 **Notes**
- **No Assembly Definitions**: All code resides in `Assembly-CSharp`.
- **DI Root**: `GameLifetimeScope.cs` (VContainer setup).
- **Performance**: Avoid allocations in `Update()`; cache `GetComponent` calls.

---

## 📄 **License**
MIT License. See [LICENSE](LICENSE) for details.
