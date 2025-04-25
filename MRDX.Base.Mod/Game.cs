using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Base.Mod;

public class Game : BaseObject<Game>, IGame
{
    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);
    private readonly IModConfig _modConfig;
    private readonly WeakReference<IRedirectorController> _redirector;

    private MonsterCache _cache = new();
    private IHook<FrameStart>? _hook;

    public List<MonsterBreed> MonsterBreeds = [];

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

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x97A0C)]
    public IMonster Monster { get; set; } = new Monster(Get() + BaseObject<Monster>.BaseOffset());

    public event IGame.WeekChange? OnWeekChange;

    public event IGame.MonsterChange? OnMonsterChanged;

#pragma warning disable 0067
    public event IGame.GameSceneChange? OnGameSceneChanged;
#pragma warning restore 0067
    public async Task SaveMonsterTechData(string redirectPath)
    {
        if (Mod.DataPath == null)
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
            var monsterTechs = MonsterBreeds
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
            var srcpath = Path.Combine(Mod.DataPath, "mf2", "data", "mon", name, atkfilename);
            var dstpath = Path.Combine(redirectPath, atkfilename);

            Logger.Debug($"Monster {display} creating file for attacks {dstpath}");
            var atkfile = IMonsterTechnique.SerializeAttackFileData(monsterTechs);
            await File.WriteAllBytesAsync(dstpath, atkfile);
            Logger.Debug($"Redirecting {srcpath} to {dstpath}");
            redirector.AddRedirect(srcpath, dstpath);

            Logger.Debug($"Monster {display} updating battle data {dstpath}");
            var flkfilename = $"{name[..2]}_{name[..2]}_b.flk";
            var flksrcpath = Path.Combine(Mod.DataPath, "mf2", "data", "mon", "btl_con", flkfilename);
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
    }

    public MonsterBreed GetBreed(MonsterGenus main, MonsterGenus sub)
    {
        return MonsterBreeds.Find(m => m.Main == main && m.Sub == sub)!;
    }

    public async Task LoadMonsterBreeds()
    {
        if (Mod.DataPath == null)
        {
            Logger.Warn("Extraction isn't complete before loading attack data.");
            return;
        }

        var newBreeds = new List<MonsterBreed>
        {
            Capacity = 400
        };
        var atkNameTable = LoadAtkNames();
        foreach (var info in IMonster.AllMonsters)
        {
            if (info.Name.StartsWith("Unknown")) continue;

            var shortname = info.ShortName[..2];
            var breedFolder = @$"{Mod.DataPath}\mf2\data\mon\{info.ShortName}\";
            var textureFiles = Directory.EnumerateFiles(breedFolder, "??_??.tex");
            var techniqueFile = Path.Combine(breedFolder, $"{shortname}_{shortname}_wz.bin");

            // Build a singular tech list. This will be the same for every breed until I do the right now and actually check errantry (no thanks :( )
            var data = await File.ReadAllBytesAsync(techniqueFile);

            Logger.Debug($"loading technique list for ${info.Name}");
            var techs = CreateTechs(atkNameTable[info.Id], data);

            // Enumerate through each species tex file and generate the final breeds.
            foreach (var filename in textureFiles)
            {
                var mainIdentifier = filename[^9..2];
                var subIdentifier = filename[^6..2];
                var breedIdentifier = $"{mainIdentifier}_{subIdentifier}";

                // Compares Sub Information to Known Monsters - Have to do some shenanigans to take care of the unknown ??? species in the database.
                var subInfo = IMonster.AllMonsters.First(s =>
                    s.ShortName[..2].Equals(subIdentifier, StringComparison.InvariantCultureIgnoreCase));

                newBreeds.Add(new MonsterBreed
                {
                    Main = info.Id,
                    Sub = subInfo.Id,
                    Name = string.Empty,
                    BreedIdentifier = breedIdentifier,
                    TechList = techs
                });
            }
        }

        MonsterBreeds = newBreeds;
    }


    private int FrameStartImpl()
    {
        UpdateMonster();
        CheckForSceneChange();
        if (_hook == null)
        {
            Logger.Error("Function hook for frame start is null.");
            return 0;
        }

        return _hook.OriginalFunction.Invoke();
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

    private void CheckForSceneChange()
    {
    }

    private static Dictionary<MonsterGenus, string[,]> LoadAtkNames()
    {
        const nuint ATK_HEADER_OFFSET = 0x340870;
        const nuint ATK_NAME_OFFSET = 0x3416B0;
        const int byteCountForAtkName = 34;
        const int byteCountForHeader = 4;
        const int numberOfAttacks = 24;
        const int allAtkNameSize = (int)MonsterGenus.Count * byteCountForAtkName * numberOfAttacks;
        const int headerSize = (int)MonsterGenus.Count * byteCountForHeader * numberOfAttacks;
        var startAddr = (nuint)(Base.ExeBaseAddress + ATK_HEADER_OFFSET.ToUInt32());
        var endAddr = (nuint)(startAddr.ToUInt64() + allAtkNameSize + headerSize);
        Memory.Instance.ReadRaw(startAddr, out var bytes, (int)(endAddr - startAddr));

        var atkList = new Dictionary<MonsterGenus, string[,]>();
        for (var mon = 0; mon < (int)MonsterGenus.Count; mon++)
        {
            var atkSlots = new string[4, 6];
            for (var i = 0; i < atkSlots.GetLength(0); ++i)
            for (var j = 0; j < atkSlots.GetLength(1); ++j)
            {
                var header = (mon * numberOfAttacks + i * 6 + j) * byteCountForHeader;
                var offset = BitConverter.ToInt32(bytes, header);
                var name = bytes[offset..(offset + byteCountForAtkName)];
                atkSlots[i, j] = name.AsShorts().AsString();
            }

            atkList[(MonsterGenus)mon] = atkSlots;
        }

        return atkList;
    }

    private static List<IMonsterTechnique> CreateTechs(string[,] atkNames, Span<byte> rawStats)
    {
        var techs = new List<IMonsterTechnique>
        {
            Capacity = 24
        };
        for (var i = 0; i < (int)TechRange.Count; ++i)
        {
            var tech = (TechRange)i;
            for (var j = 0; j < 6; ++j)
            {
                var slot = (i * 6 + j) * 4;
                Logger.Trace($"new attack tech {tech} slot {slot}");
                var offset = BitConverter.ToInt32(rawStats[slot .. (slot + 4)]);
                if (offset < 0)
                {
                    Logger.Trace($"skipping negative offset {offset}");
                    continue;
                }

                Logger.Trace($"new attack offset {offset}");
                techs.Add(new MonsterTechnique(atkNames[i, j], (TechSlots)(1 << (i * 6 + j)),
                    rawStats[offset .. (offset + 0x20)]));
            }
        }

        return techs;
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