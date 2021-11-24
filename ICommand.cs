namespace wifish
{
    internal interface ICommand
    {
        string Description { get; }

        bool AreRequirementsFulfilled();

        void Execute();
    }
}
