using System.Collections.Generic;

namespace Script
{
    public interface ICommandFactory
    {
        // Metodo per creare un comando in base al tipo e ai dati forniti.
        ICommand CreateCommand(string commandType, Dictionary<string, object> commandData);
    }
}