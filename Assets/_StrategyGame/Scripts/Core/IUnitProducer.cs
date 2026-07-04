using StrategyGame.Data;

namespace StrategyGame.Core
{
    // Implemented by buildings that can produce units (e.g. Barracks).
    public interface IUnitProducer
    {
        UnitData[] ProducibleUnits { get; }
        bool CanProduceUnits { get; }
    }
}
