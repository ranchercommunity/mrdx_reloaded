using System.Collections.Generic;
using System.Threading.Tasks;

namespace MRDX.Base.Mod.Interfaces;

public enum GameScene
{
    LoadingScreen,
    Ranch,
    Training,
    Erranty,
    Town
}

public interface IGame
{
    /// <summary>
    ///     Event that is fired when the game scene change is detected.
    /// </summary>
    /// <param name="newScene">What scene we just transitioned to</param>
    public delegate void GameSceneChange(GameScene newScene);

    /// <summary>
    ///     Event that is fired when any of the fields of the current monster is changed
    ///     The passed in values are readonly copies of the data. If you need to edit the monster
    ///     just use the regular Monster field.
    /// </summary>
    public delegate void MonsterChange(IMonsterChange change);

    public IMonster Monster { get; set; }

    public event MonsterChange OnMonsterChanged;

    /// <summary>
    ///     This event allows you to receive a copy of the inputs before they are sent to the game.
    /// </summary>
    public event GameSceneChange OnGameSceneChanged;

    public Task<Dictionary<string, IList<IMonsterTechnique>>> LoadMonsterAttackData();
    public Task SaveMonsterAttackData(Dictionary<string, List<IMonsterTechnique>> monsters);
}

public interface ISaveFile
{
    IMonster CurrentMonster { get; set; }
    IList<IMonster> Freezer { get; init; }
}

public interface IMonsterChange
{
    long Offset { get; }
    IMonster Previous { get; }
    IMonster Current { get; }

    bool IsChangeFromMod { get; }

    StatFlags FieldsChangedFlags { get; }
}

public interface IMonsterModChange : IMonsterChange
{
}

public interface IMonsterCompleteChange : IMonsterChange
{
}