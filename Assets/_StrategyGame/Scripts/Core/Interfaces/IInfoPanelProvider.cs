using StrategyGame.Data;

namespace StrategyGame.Core
{
    // Implemented by every selectable entity that should display data in the InformationPanel
    // (currently BuildingBase and UnitBase; future types such as Vehicle or Hero only need
    // to implement this interface — InformationPanelController never needs to change).
    //
    // OCP: InformationPanelController depends solely on this interface, not on concrete types.
    // Adding a new entity type therefore requires zero modifications to the panel controller.
    public interface IInfoPanelProvider
    {
        // Data used to populate the panel header (DisplayName, Icon).
        EntityData EntityData { get; }

        // True when the delete button should be visible (grid-placed buildings).
        // False for units and any future non-deletable entity types.
        bool CanBeDeleted { get; }

        // Non-null when this entity can produce units; the panel will show the production list.
        // Null for units and non-production buildings, which hides the production list.
        IUnitProducer UnitProducer { get; }
    }
}
