using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Base.Mod;

public class Game : BaseObject<Game>, IGame
{
    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);
    private readonly IModConfig _modConfig;
    private readonly WeakReference<IRedirectorController> _redirector;

    private MonsterCache _cache = new();

    private uint _currentWeek = uint.MaxValue;
    private IHook<FrameStart>? _hook;

    public Game(ModContext context)
    {
        _modConfig = context.ModConfig;
        context.ModLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        _redirector = context.ModLoader.GetController<IRedirectorController>();
        if (hooks == null)
        {
            Logger.Error("Could not get hook controller.");
            return;
        }

        hooks.AddHook<FrameStart>(FrameStartImpl).ContinueWith(result => _hook = result.Result.Activate());
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x379444)]
    public uint Week
    {
        get => Read<uint>();
        set => Write(value);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x3795A2)]
    public List<MonsterGenus> UnlockedMonsters => ReadArray<byte>(44).ToArray()
        .Select((m, i) => m != 0 ? (MonsterGenus)i : MonsterGenus.Garbage)
        .Where(m => m != MonsterGenus.Garbage).ToList();

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x97A0C)]
    public IMonster Monster { get; set; } = new Monster(Get() + BaseObject<Monster>.BaseOffset());

    public Utils.OneTimeEvent<bool> OnMonsterBreedsLoaded { get; set; } = new();
    public event IGame.WeekChange? OnWeekChange;

    public event IGame.MonsterChange? OnMonsterChanged;

#pragma warning disable 0067
    public event IGame.GameSceneChange? OnGameSceneChanged;
#pragma warning restore 0067


    private int FrameStartImpl()
    {
        UpdateMonster();
        CheckForSceneChange();
        CheckForWeekChange();
        if (_hook == null)
        {
            Logger.Error("Function hook for frame start is null.");
            return 0;
        }

        return _hook.OriginalFunction.Invoke();
    }

    private void CheckForWeekChange()
    {
        if (Week == _currentWeek) return;

        OnWeekChange?.Invoke(new WeekChange(_currentWeek, Week));
        _currentWeek = Week;
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
            {
                var val = prop.GetValue(_cache);
                flags |= val?.Equals(prop.GetValue(newCache)) ?? false ? 0 : value;
            }

        // _logger.WriteLine($"[MRDX.Base.Mod] Monster changed: {flags}");
        if (Monster is not Monster mon)
        {
            Logger.Error("Monster in UpdateMonster is not a Monster.");
            return;
        }

        OnMonsterChanged?.Invoke(new StandardMonsterChanged
        {
            Offset = mon.BaseAddress,
            Previous = _cache,
            Current = newCache,
            IsChangeFromMod = false,
            FieldsChangedFlags = flags
        });

        _cache = newCache;
    }

    public async Task SaveMonsterTechData(string redirectPath)
    {
        if (IExtractDataBin.ExtractedPath == null)
        {
            Logger.Warn("Extraction isn't complete before saving attack data.");
            return;
        }

        _redirector.TryGetTarget(out var redirector);
        if (redirector == null)
        {
            Logger.Error("Failed to get redirection controller.");
            return;
        }

        redirector.Disable();

        const int HeaderSize = 4 * 24;
        const int NameSize = 34;
        const int AtkNameDataOffset = HeaderSize * (int)MonsterGenus.Count;

        const int byteCountForAtkName = 34;
        const int byteCountForHeader = 4;
        var atkData =
            new byte[HeaderSize * (int)MonsterGenus.Count + NameSize * (int)MonsterGenus.Count * 24];
        for (var genus = 0; genus < (int)MonsterGenus.Count; genus++)
        {
            var mon = IMonster.AllMonsters[genus];
            var (_, display, name) = mon;
            var headerOffset = HeaderSize * genus;
            var atkNameOffset = NameSize * genus + HeaderSize * (int)MonsterGenus.Count;

            // Write the attack name and header to the temp array so we can write it out later
            var monsterTechs = MonsterBreed.AllBreeds
                .Find(b => b.Main == b.Sub && b.Main == mon.Id)!.TechList;

            // Build a header for the attacks 
            for (var i = 0; i < 24; ++i)
            {
                var tech = monsterTechs.Find(t => t.Slot.HasFlag((TechSlots)(1 << i)));
                if (tech == null)
                {
                    NO_OFFSET.CopyTo(atkData, headerOffset);
                    headerOffset += byteCountForHeader;
                    continue;
                }

                BitConverter.GetBytes(atkNameOffset).CopyTo(atkData, headerOffset);
                tech.Name.AsMr2().AsBytes().CopyTo(atkData, atkNameOffset);
                atkNameOffset += byteCountForAtkName;
                headerOffset += byteCountForHeader;
            }

            // Write the attack data out to a file to redirect.
            var atkfilename = $"{name[..2]}_{name[..2]}_wz.bin";
            var srcpath = Path.Combine(IExtractDataBin.ExtractedPath, "mf2", "data", "mon", name, atkfilename);
            var dstpath = Path.Combine(redirectPath, atkfilename);

            Logger.Debug($"Monster {display} creating file for attacks {dstpath}");
            var atkfile = IMonsterTechnique.SerializeAttackFileData(monsterTechs);
            await File.WriteAllBytesAsync(dstpath, atkfile);
            Logger.Debug($"Redirecting {srcpath} to {dstpath}");
            redirector.AddRedirect(srcpath, dstpath);

            Logger.Debug($"Monster {display} updating battle data {dstpath}");
            var flkfilename = $"{name[..2]}_{name[..2]}_b.flk";
            var flksrcpath = Path.Combine(IExtractDataBin.ExtractedPath, "mf2", "data", "mon", "btl_con", flkfilename);
            var flkdstpath = Path.Combine(redirectPath, flkfilename);
            var flk = await File.ReadAllBytesAsync(flksrcpath);
            atkfile.CopyTo(flk, 0);
            await File.WriteAllBytesAsync(flkdstpath, flk);
            Logger.Debug($"Redirecting {flksrcpath} to {flkdstpath}");
            redirector.AddRedirect(flksrcpath, flkdstpath);
        }

        // Now write the attack name data back to the exe
        // TODO actually get the attack name table working
        // _memory.Write((nuint)(Base.Mod.Base.ExeBaseAddress + ATK_HEADER_OFFSET.ToUInt32()), atkData);
        Logger.Debug("Finished Saving atk data");

        redirector.Enable();
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

public record struct WeekChange(uint OldWeek, uint NewWeek) : IWeekChange
{
}