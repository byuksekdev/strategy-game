using StrategyGame.Data;
using UnityEngine;

namespace StrategyGame.Core
{
    // ── Selection ──────────────────────────────────────────────────────────────
    // Raised when the active selection changes. Selected is null when cleared.
    public readonly struct SelectionChangedEvent
    {
        public readonly ISelectable Selected;
        public SelectionChangedEvent(ISelectable selected) => Selected = selected;
    }

    // ── Building ───────────────────────────────────────────────────────────────
    // Raised when a building is successfully placed on the grid.
    public readonly struct BuildingPlacedEvent
    {
        public readonly GameObject Building;
        public BuildingPlacedEvent(GameObject building) => Building = building;
    }

    // Raised just before a building is despawned (HP == 0 or manually deleted).
    public readonly struct BuildingDestroyedEvent
    {
        public readonly GameObject Building;
        public BuildingDestroyedEvent(GameObject building) => Building = building;
    }

    // ── Unit ───────────────────────────────────────────────────────────────────
    // Raised after a unit is spawned and initialized on the grid.
    public readonly struct UnitSpawnedEvent
    {
        public readonly GameObject Unit;
        public UnitSpawnedEvent(GameObject unit) => Unit = unit;
    }

    // Raised just before a unit is despawned (HP == 0).
    public readonly struct UnitDestroyedEvent
    {
        public readonly GameObject Unit;
        public UnitDestroyedEvent(GameObject unit) => Unit = unit;
    }

    // ── Production ─────────────────────────────────────────────────────────────
    // Raised when the user clicks a building card in the production menu.
    // Triggers both the placement system and the information panel preview.
    public readonly struct BuildingProductionRequestedEvent
    {
        public readonly BuildingData BuildingData;
        public BuildingProductionRequestedEvent(BuildingData data) => BuildingData = data;
    }

    // Raised when the user clicks a unit card in the information panel.
    // Consumed by UnitSpawnSystem.
    public readonly struct UnitProductionRequestedEvent
    {
        public readonly UnitData UnitData;
        public readonly IUnitProducer Producer;

        public UnitProductionRequestedEvent(UnitData unitData, IUnitProducer producer)
        {
            UnitData = unitData;
            Producer = producer;
        }
    }

    // ── Placement ──────────────────────────────────────────────────────────────
    // Raised when placement mode ends (building placed, right-click, or ESC cancel).
    public readonly struct PlacementModeExitedEvent { }
}
