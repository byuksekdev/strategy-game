using UnityEngine;
using StrategyGame.Data;

namespace StrategyGame.Buildings
{
    // Abstraction that decouples building creation from its consumers.
    // Consumers (e.g. BuildingPlacementController) depend on this interface,
    // not on the concrete BuildingFactory, satisfying SOLID's Dependency Inversion Principle.
    public interface IBuildingFactory
    {
        // Creates and places a building at the given grid origin.
        // Returns the initialized BuildingBase, or null if placement failed.
        BuildingBase Create(BuildingData data, Vector2Int gridOrigin, Transform parent = null);
    }
}
