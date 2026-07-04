namespace StrategyGame.Core
{
    // Every object that can be selected on the board (building, unit, etc.) implements this interface.
    public interface ISelectable
    {
        void OnSelected();
        void OnDeselected();
    }
}
