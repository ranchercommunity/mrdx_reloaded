using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public sealed class Mod : ModBase, IExports // <= Do not Remove.
{
    private static readonly Dictionary<string, ushort[]> PluralNames = new()
    {
        { "Pixies", "Pixie".AsMr2() },
        { "Dragons", "Dragon".AsMr2() },
        { "Centaurs", "Centaur".AsMr2() },
        { "ColorPandoras", "ColorPandora".AsMr2() },
        { "Beaclons", "Beaclon".AsMr2() },
        { "Hengers", "Henger".AsMr2() },
        { "Wrackys", "Wracky".AsMr2() },
        { "Golems", "Golem".AsMr2() },
        { "Zuums", "Zuum".AsMr2() },
        { "Durahans", "Durahan".AsMr2() },
        { "Arrow Heads", "Arrow Head".AsMr2() },
        { "Tigers", "Tiger".AsMr2() },
        { "Hoppers", "Hopper".AsMr2() },
        { "Hares", "Hare".AsMr2() },
        { "Bakus", "Baku".AsMr2() },
        { "Galis", "Gali".AsMr2() },
        { "Katos", "Kato".AsMr2() },
        { "Zillas", "Zilla".AsMr2() },
        { "Bajarls", "Bajarl".AsMr2() },
        { "Mews", "Mew".AsMr2() },
        { "Phoenixes", "Phoenix".AsMr2() },
        { "Ghosts", "Ghost".AsMr2() },
        { "Metalners", "Metalner".AsMr2() },
        { "Suezos", "Suezo".AsMr2() },
        { "Jills", "Jill".AsMr2() },
        { "Mocchis", "Mocchi".AsMr2() },
        { "Jokers", "Joker".AsMr2() },
        { "Gaboos", "Gaboo".AsMr2() },
        { "Jells", "Jell".AsMr2() },
        { "Undines", "Undine".AsMr2() },
        { "Nitons", "Niton".AsMr2() },
        { "Mocks", "Mock".AsMr2() },
        { "Duckens", "Ducken".AsMr2() },
        { "Plants", "Plant".AsMr2() },
        { "Monols", "Monol".AsMr2() },
        { "Apes", "Ape".AsMr2() },
        { "Worms", "Worm".AsMr2() },
        { "Nagas", "Naga".AsMr2() }
    };

    private readonly IGame _game;

    /// <summary>
    ///     Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    ///     Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    private readonly Memory _memory;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    /// <summary>
    ///     Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    private readonly IStartupScanner? _startupScanner;


    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _configuration = context.Configuration;
        Logger.LoggerInternal = _logger;
        ConfigurationUpdated(_configuration);

        // Order is somewhat important here as some other controllers will use the Hooks
        var hooks = new Hooks(context);
        _modLoader.AddOrReplaceController<IHooks>(_owner, hooks);
        _modLoader.AddOrReplaceController<IController>(_owner, new Controller(context));
        _game = new Game(context);
        _modLoader.AddOrReplaceController(_owner, _game);
        _modLoader.AddOrReplaceController<IGameClient>(_owner, new GameClient());
        _modLoader.AddOrReplaceController<ISaveFile>(_owner, new SaveFileManager(_modLoader));

        var maybeExtractor = _modLoader.GetController<IExtractDataBin>();
        if (maybeExtractor != null && maybeExtractor.TryGetTarget(out var extract))
            extract.ExtractComplete.Subscribe(ExtractionComplete);

        var maybeScanner = _modLoader.GetController<IStartupScanner>();
        if (maybeScanner != null && maybeScanner.TryGetTarget(out var scanner)) _startupScanner = scanner;

        _memory = Memory.Instance;

        if (_configuration.FixMonsterBreedPluralization && _startupScanner != null)
            // Sigscanning can only find code not data, so we use it to find a pointer to the plural block and patch them
            // directly instead of hooking the text rendering functions
            _startupScanner.AddMainModuleScan("05 ?? ?? ?? ?? 50 E8 ?? ?? ?? ?? 6B C6 1B",
                result => FixMonsterBreedPluralization(result, 1));

        // Disable these hooks until we fix it later
        // hooks!.AddHook<ParseTextWithCommandCodes>(DrawTextToScreenHook)
        // .ContinueWith(result => _textHook = result.Result.Activate());
        // hooks!.AddHook<DrawBattleNumberToScreen>(DrawBattleNumberToScreenHook)
        //     .ContinueWith(result => _numberHook = result.Result.Activate());
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    // private IHook<DrawBattleNumberToScreen>? _numberHook;

    // private IHook<ParseTextWithCommandCodes>? _textHook;

    public static string? DataPath { get; set; }

    public Type[] GetTypes()
    {
        return [typeof(IController), typeof(IHooks), typeof(IGame), typeof(IGameClient), typeof(ISaveFile)];
    }

    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        Logger.GlobalLogLevel = _configuration.LogLevel;
    }

    private void ExtractionComplete(string? path)
    {
        DataPath = path;
        LoadMonsterBreeds()
            .ContinueWith(t =>
            {
                Logger.Debug("Firing monster breed loaded one off event");
                _game.OnMonsterBreedsLoaded.Fire(true);
            });
    }

    private void FixMonsterBreedPluralization(PatternScanResult result, int addrOffset)
    {
        if (!result.Found)
        {
            Logger.Warn("Unable to find pointer to pluralized text!");
            return;
        }

        var pointer = (nuint)(Base.ExeBaseAddress + result.Offset + addrOffset);
        _memory.Read(pointer, out nuint addr);
        Logger.Info($"Patching Pluralization at {addr:x}");
        // each name is a fixed chunk of 13 letters (ushort) + 1 bytes (0xff)
        for (var i = 0; i < PluralNames.Count; ++i)
        {
            const int len = 13 * 2;
            var straddr = nuint.Add(addr, i * (len + 1));
            _memory.ReadRaw(straddr, out var bytes, len);
            var str = bytes.AsShorts().AsString();
            var unpluralized = PluralNames[str].AsBytes();

            _memory.SafeWrite(straddr, unpluralized);
        }

        Logger.Info($"Patching Pluralization at {addr:x} Complete");
    }

    private static async Task LoadMonsterBreeds()
    {
        Logger.Info("Loading monster breeds");
        var newBreeds = new List<MonsterBreed>
        {
            Capacity = 400
        };
        var atkNameTable = LoadAtkNames();

        var sDataList = new Dictionary<(MonsterGenus, MonsterGenus), string[]>();
        var sdata = await File.ReadAllLinesAsync(Path.Combine(DataPath!, "SDATA_MONSTER.csv"));
        foreach (var t in sdata)
        {
            var row = t.Split(",");

            if (!sDataList.ContainsKey(((MonsterGenus)int.Parse(row[2]), (MonsterGenus)int.Parse(row[3]))))
            {
                sDataList.Add(((MonsterGenus)int.Parse(row[2]), (MonsterGenus)int.Parse(row[3])), row);
            }
        }

        foreach (var info in IMonster.AllMonsters)
        {
            if (info.Name.StartsWith("Unknown")) continue;

            var shortname = info.ShortName[..2];
            var breedFolder = @$"{DataPath}\mf2\data\mon\{info.ShortName}\";
            var textureFiles = Directory.EnumerateFiles(breedFolder, "??_??.tex");
            var techniqueFile = Path.Combine(breedFolder, $"{shortname}_{shortname}_wz.bin");

            // Build a singular tech list. This will be the same for every breed until I do the right now and actually check errantry (no thanks :( )
            Logger.Trace($"loading technique list from {techniqueFile}");
            var data = await File.ReadAllBytesAsync(techniqueFile);

            Logger.Debug($"Creating technique list for {info.Name}");
            var techs = CreateTechs(atkNameTable[info.Id], data);
            Logger.Trace($"tech loading complete for {info.Name}");

            // Enumerate through each species tex file and generate the final breeds.
            foreach (var filename in textureFiles)
            {
                Logger.Trace($"checking texture files by the name of {filename}");
                var mainIdentifier = filename[^9..^7];
                var subIdentifier = filename[^6..^4];
                var breedIdentifier = $"{mainIdentifier}_{subIdentifier}";

                Logger.Trace($"creating new breed {breedIdentifier}");

                // Compares Sub Information to Known Monsters - Have to do some shenanigans to take care of the unknown ??? species in the database.
                var sub = MonsterGenus.Garbage;
                foreach (var mon in IMonster.AllMonsters)
                    if (mon.ShortName[..2].Equals(subIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        sub = mon.Id;
                        break;
                    }

                if (sub == MonsterGenus.Garbage)
                {
                    Logger.Trace($"Can't handle garbage unknown breed {breedIdentifier}");
                    continue;
                }

                if (info.Id == MonsterGenus.Durahan && sub == MonsterGenus.Henger)
                {
                    Logger.Trace($"Durahan / Henger has an invalid texture file. {breedIdentifier}");
                    continue;
                }

                Logger.Trace($"found subinfo: {sub}");
                newBreeds.Add(new MonsterBreed
                {
                    Main = info.Id,
                    Sub = sub,
                    Name = string.Empty,
                    BreedIdentifier = breedIdentifier,
                    TechList = techs,
                    SDATAValues = sDataList[(info.Id, sub)]
                });
            }
        }

        Logger.Info("Finished loading all breeds");
        MonsterBreed.AllBreeds = newBreeds;
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
    // private void DrawTextToScreenHook(nint input, nint output, nint unk2)
    // {
    //     _textHook!.OriginalFunction(input, output, unk2);
    //     var o = Base.ReadString(output);
    //     // _logger.WriteLine($"Parsed Text: {o}");
    // }

    // private void DrawBattleNumberToScreenHook(int number, short xcoord, short ycoord, short unkflag, uint unused5,
    //     uint unused6, IntPtr unkdata)
    // {
    //     _logger.WriteLine($"Number: {number} at ({xcoord}, {ycoord}) flag: {unkflag} ptr: {unkdata.ToInt64():02X}");
    //     _numberHook!.OriginalFunction(number, xcoord, ycoord, unkflag, unused5, unused6, unkdata);
    // }
}