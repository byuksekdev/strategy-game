namespace StrategyGame.Data
{
    // Every object that can appear in a production menu (a data asset or a runtime entity)
    // implements this interface. Using IProducible instead of a concrete type lets the
    // production menu, scroll view, and event bus remain open to extension (OCP) and
    // free of concrete-type dependencies (DIP).
    //
    // Implementors:
    //   EntityData (and its subclasses BuildingData, UnitData) — data assets listed in the menu.
    //   BuildingBase — runtime building that exposes its backing data for polymorphic reads.
    public interface IProducible
    {
        EntityData Data { get; }
    }
}
