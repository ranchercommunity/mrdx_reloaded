using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class Game : BaseObject<Game>, IGame
{
    public ISaveFile Loaded { get; set; }
    public IList<ISaveFile> SaveFiles { get; init; } = new List<ISaveFile>();
}