using StrategyGame.Data;

namespace StrategyGame.Core
{
    // Every object that can be produced in the production menu (building, unit, etc.) implements this interface.
    public interface IProducible
    {
        EntityData Data { get; }
    }
}
