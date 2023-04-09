using System.Collections.Generic;

namespace MRDX.Base.Mod.Interfaces;

public interface IGame
{
    ISaveFile Loaded { get; set; }
    IList<ISaveFile> SaveFiles { get; init; }
}

public interface ISaveFile
{
    IMonster CurrentMonster { get; set; }
    IList<IMonster> Freezer { get; init; }
}