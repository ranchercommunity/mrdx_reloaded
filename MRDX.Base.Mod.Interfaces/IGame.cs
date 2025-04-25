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

    /// <summary>
    ///     Event that is fired when the week changes
    /// </summary>
    public delegate void WeekChange(IWeekChange change);

    public IMonster Monster { get; set; }

    public Utils.OneTimeEvent<bool> OnMonsterBreedsLoaded { get; }

    public event WeekChange OnWeekChange;

    public event MonsterChange OnMonsterChanged;

    /// <summary>
    ///     This event allows you to receive a copy of the inputs before they are sent to the game.
    /// </summary>
    public event GameSceneChange OnGameSceneChanged;
}

public interface IWeekChange
{
    uint OldWeek { get; }
    uint NewWeek { get; }
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

public interface ISaveFileEntry
{
    string Filename { get; }
    string Slot { get; }
    bool IsAutoSave { get; }
}

public interface ISaveFile
{
    public delegate void Load(ISaveFileEntry file);

    public delegate void Save(ISaveFileEntry file);

    public event Load OnLoad;

    public event Save OnSave;

    // IMonster CurrentMonster { get; set; }
    // IList<IMonster> Freezer { get; init; }
}