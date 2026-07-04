using System;

namespace Script.Core.SaveSystem
{
    public interface ISaveable
    {
        GameSaveData Save();
        void Load(GameSaveData data);
    }
}