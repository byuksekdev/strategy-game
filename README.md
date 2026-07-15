# Strategy Game Demo

A 2D grid-based real-time strategy game built with Unity, featuring building placement, unit production, pathfinding-driven movement, and a melee combat system.

---

## Gameplay Overview

- **Place buildings** from the production menu onto the grid. The placement preview turns red on invalid areas and green on valid ones.
- **Select a building** to view its details in the information panel. Selecting a Barracks also lists its producible units.
- **Produce units** directly from the information panel — no production time.
- **Select units** with left-click, then **right-click a position** to move them along the shortest path, or **right-click an enemy** to attack.

---

## Features

### Building System
- Four building types: **Barracks** (100 HP), **Power Plant** (50 HP), **Watch Tower**, and **Panteon Bust**
- Ghost preview with green/red grid overlay during placement
- Buildings occupy grid cells and block movement; cells are freed on destruction

### Unit System
- Three soldier types produced from Barracks, each with 10 HP and different attack damage:
  - **Soldier 1** — 10 damage
  - **Soldier 2** — 5 damage
  - **Soldier 3** — 2 damage
- Each Barracks has a designated spawn point; soldiers emerge from it on production
- Units and buildings are destroyed when HP reaches 0

### Pathfinding & Combat
- **A\* pathfinding** with 8-directional movement and diagonal corner-cutting prevention
- Path is recalculated dynamically if a cell becomes blocked mid-movement
- Right-clicking a unit or building with a selected soldier initiates a melee attack

### UI
- **Production Menu** — infinite scroll list with object pooling
- **Information Panel** — shows name, icon, HP, and dimensions of the selected entity; includes a unit list for Barracks
- **Health Bars** — world-space bars on all entities, updated via local events
- **Responsive layout** — works across different screen resolutions and aspect ratios

---

## Architecture

The project follows SOLID principles and uses an event-driven, interface-based architecture to keep systems decoupled.

### Design Patterns Used

| Pattern | Where |
|---|---|
| **Composition Root / DI** | `GameBootstrapper` wires all dependencies at startup |
| **Factory** | `IBuildingFactory` / `BuildingFactory` for spawning buildings |
| **Event Bus** | Type-safe pub/sub (`EventBus` + `GameEvents`) for cross-system communication |
| **Object Pooling** | LeanPool for buildings, units, UI items, and placement ghosts |
| **MVC** | UI controllers and views are separated; models publish events |
| **Registry** | `UnitRegistry` maps grid cells to units for O(1) lookup |
| **ScriptableObject Data** | `EntityData`, `BuildingData`, `UnitData` as data-only assets |
| **Template Method** | `BuildingBase.OnInitialized()` and per-soldier damage overrides |

### Interface Segregation (Grid)
The grid system is split into read and write contracts:
- `IGridProvider` — read-only cell queries
- `IGridOccupancyManager` — occupancy writes
- `IGridService` — both (used by factory)

### Key Systems

**`GameBootstrapper`** — The only place that knows about concrete types. Injects dependencies into all systems at startup.

**`GridManager`** — Owns grid state. Listens for `BuildingDestroyedEvent` to free cells; buildings never write to the grid directly.

**`SelectionController`** — Handles all input. Manages selection, movement commands, attack commands, and path preview rendering.

**`BuildingPlacementController`** — Manages placement mode; delegates creation to `BuildingFactory`.

**`UnitBase`** — Drives movement (A* + cell-by-cell lerp) and melee attack coroutines with registry consistency on cancel.

**`UnitSpawnSystem`** — Handles unit lifecycle: BFS nearest free cell, LeanPool spawn, registry registration and cleanup.

---

## Project Structure

```
Assets/_StrategyGame/
├── Art/                    # Sprites and materials
├── Data/                   # ScriptableObject assets (buildings, units, config)
├── Prefabs/                # Buildings, units, UI, grid
├── Scenes/
└── Scripts/
    ├── Core/               # Bootstrap, EventBus, interfaces, SelectionController
    ├── Grid/               # GridManager, GridCell, GridConfig, visualizers
    ├── Pathfinding/        # AStarPathfinder
    ├── Buildings/          # BuildingBase, BuildingFactory, PlacementController
    ├── Units/              # UnitBase, UnitRegistry, UnitSpawnSystem, soldier types
    ├── Data/               # ScriptableObject class definitions
    └── UI/                 # ProductionMenu, InformationPanel, InfiniteScrollView, HealthBar
```

---

## Technical Notes

- **Draw calls kept under 20** via GPU instancing, sprite atlases, and procedural mesh-based grid rendering (single draw call for the entire grid)
- **Object pooling** applied to UI scroll items, buildings, units, and placement ghosts to avoid runtime allocations
- **Static A\* scratch buffers** minimize GC pressure during pathfinding
- **ScriptableObject data layer** keeps entity configuration out of MonoBehaviours and easy to extend
- **EventBus subscriptions** are cleared on domain reload to prevent stale delegates

---

## Platform

Unity 2021 LTS — 2D — Windows
