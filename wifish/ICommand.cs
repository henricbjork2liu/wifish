namespace wifish
{
    public interface ICommand
    {
        string Description { get; }

        bool AreRequirementsFulfilled();
        
        void Execute();
    }
}
