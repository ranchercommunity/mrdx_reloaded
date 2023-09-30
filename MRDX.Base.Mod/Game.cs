using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class Game : BaseObject<Game>, IGame
{
    private readonly ILogger _logger;
    private MonsterCache _cache = new();
    private IHook<FrameStart>? _hook;

    public Game(ModContext context)
    {
        _logger = context.Logger;
        context.ModLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks!.AddHook<FrameStart>(FrameStartImpl).ContinueWith(result => _hook = result.Result.Activate());
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x97A0C)]
    public IMonster Monster { get; set; } = new Monster(Get());

    public event IGame.MonsterChange? OnMonsterChanged;

#pragma warning disable 0067
    public event IGame.GameSceneChange? OnGameSceneChanged;
#pragma warning restore 0067

    private int FrameStartImpl()
    {
        UpdateMonster();
        CheckForSceneChange();
        return _hook!.OriginalFunction.Invoke();
    }

    private void UpdateMonster()
    {
        // Read all of the current monsters data and look for differences.
        var newCache = new MonsterCache(Monster);
        if (_cache == newCache) return;

        // Monster stat changed so send a new event
        StatFlags flags = 0;
        foreach (var prop in typeof(IMonster).GetProperties())
            if (StatFlagUtil.LookUp.TryGetValue(prop.Name, out var value))
                flags |= prop.GetValue(_cache)!.Equals(prop.GetValue(newCache)) ? 0 : value;
        _logger.WriteLine($"Monster changed: {flags}");
        OnMonsterChanged?.Invoke(new StandardMonsterChanged
        {
            Offset = (Monster as Monster)!.BaseAddress,
            Previous = _cache,
            Current = newCache,
            IsChangeFromMod = false,
            FieldsChangedFlags = flags
        });

        _cache = newCache;
    }

    private void CheckForSceneChange()
    {
    }
}

public class StandardMonsterChanged : IMonsterChange
{
    public long Offset { get; init; }
    public IMonster Previous { get; init; } = new MonsterCache();
    public IMonster Current { get; init; } = new MonsterCache();
    public bool IsChangeFromMod { get; init; }
    public StatFlags FieldsChangedFlags { get; init; }
}