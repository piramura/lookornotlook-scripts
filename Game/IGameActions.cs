using Piramura.LookOrNotLook.Item;

namespace Piramura.LookOrNotLook.Game
{
    public interface IGameActions
    {
        void Collect(ItemDefinition def);
        void Penalty(ItemDefinition def);
    }
}
