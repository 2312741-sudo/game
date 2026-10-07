namespace TramChanh.Interaction
{
    public interface IHeldItemAction
    {
        InteractionQuery QueryUse(InteractionContext context);
        void ExecuteUse(InteractionContext context);
    }
}
