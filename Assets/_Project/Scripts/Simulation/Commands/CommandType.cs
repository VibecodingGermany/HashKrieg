namespace Nova.Simulation
{
    public enum CommandType : byte
    {
        None = 0,
        Move = 1,
        Stop = 2,
        AttackTarget = 3,
        AttackMove = 4,
        BuildStructure = 5,
        ProduceUnit = 6,
        ResearchTech = 7,
        CancelProduction = 8,
        UseAbility = 9
    }

    public enum CommandIssuer : byte
    {
        Human = 0,
        Peer = 1,
        AI = 2
    }
}
