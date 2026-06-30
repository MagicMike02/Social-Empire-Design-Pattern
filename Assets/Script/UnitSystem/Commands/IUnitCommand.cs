namespace Script.UnitSystem.Commands
{
    /// <summary>
    /// Contratto command per UnitSystem (disaccoppiato dal CommandHistory globale).
    /// </summary>
    public interface IUnitCommand
    {
        bool Execute();
    }
}
