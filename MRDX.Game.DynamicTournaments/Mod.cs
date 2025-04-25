using System.Diagnostics;
using System.Drawing;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

//using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

//using static MRDX.Base.Mod.Interfaces.TournamentData;
//using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace MRDX.Game.DynamicTournaments;

public class Mod : ModBase // <= Do not Remove.
{
    [HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 53 8B 5D 08 8A C3 24 03 02 C0 56 57 8B F9 BE 01 00 00 00 B1 07")]
    [Function(CallingConventions.Fastcall)]
    public delegate void CheckShrineUnlockRequirementHook(nint parent);

    private readonly nuint _addressCurrentweek;
    private readonly nuint _addressTournamentmonsters;
    private readonly nuint _addressUnlockedmonsters;
    private readonly nuint _gameAddress;
    private readonly IHooks? _iHooks;
    private readonly IScanner _memoryScanner;
    private readonly string _saveDataFolder;

    private readonly ISaveFile? _saveFile;

    private readonly List<MonsterGenus> _unlockedmonsters = [];
    private uint _gameCurrentWeek;

    private string? _gamePath;

    private LearningTesting _LT;

    //private IHook<CheckShrineUnlockRequirementHook>? _shrineUnlockHook;
    //private bool monsterUnlockCheckDefaults = false;

    private IHook<UpdateGenericState>? _updateHook;

    public TournamentData tournamentData;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _saveDataFolder = Path.Combine(_modLoader.GetDirectoryForModId(_modConfig.ModId), "SaveData");

        _modLoader.GetController<IHooks>().TryGetTarget(out _iHooks);
        _modLoader.GetController<ISaveFile>().TryGetTarget(out _saveFile);

        var startupScanner = _modLoader.GetController<IStartupScanner>();
        //var memscanner = _modLoader.GetController<IScanner>();
        //_startupScanner = startupScanner;

        _gameAddress = (nuint)Base.Mod.Base.ExeBaseAddress;

        _addressUnlockedmonsters = _gameAddress + 0x3795A2;
        _addressTournamentmonsters = _gameAddress + 0x548D10;
        _addressCurrentweek = _gameAddress + 0x379444;
        //548CD0

        if (_iHooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get hook controller.", Color.Red);
            return;
        }

        _iHooks.AddHook<UpdateGenericState>(SetupUpdateHook)
            .ContinueWith(result => _updateHook = result.Result.Activate());


        if (startupScanner != null)
        {
            startupScanner.TryGetTarget(out var scanner);
            AlterCode_CheckShrineUnlockRequirements(scanner);
            AlterCode_TournamentLifespanIndex(scanner);
        }

        _modLoader.GetController<IScannerFactory>().TryGetTarget(out var sf);
        _memoryScanner = sf.CreateScanner(Process.GetCurrentProcess(), Process.GetCurrentProcess().MainModule);

        var maybeExtractor = _modLoader.GetController<IExtractDataBin>();
        if (maybeExtractor != null && maybeExtractor.TryGetTarget(out var extract))
            lock (IExtractDataBin.LockMr2)
            {
                if (extract.ExtractedPath != null)
                    _gamePath = extract.ExtractedPath;
                else
                    extract.ExtractComplete += path =>
                    {
                        _gamePath = path;
                        ProcessExtractedData();
                    };
            }

        var maybeSaveFile = _modLoader.GetController<ISaveFile>();
        if (maybeSaveFile != null && maybeSaveFile.TryGetTarget(out _saveFile))
        {
            _saveFile.OnSave += SaveTournamentData;
            _saveFile.OnLoad -= LoadTournamentData;
        }
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    /// <summary>
    ///     Processes data that requires the extraction of the binary first. Can be called two separate ways.
    /// </summary>
    private void ProcessExtractedData()
    {
        tournamentData = new TournamentData(_configuration);
        // SetupMonsterBreeds();

        _LT = new LearningTesting(_iHooks, _gameAddress);
        _LT._tournamentStatBonus = _configuration.StatGrowth > 0
            ? _configuration.StatGrowth + 1
            : 0;
    }


    private void SaveTournamentData(ISaveFileEntry savefile)
    {
        Directory.CreateDirectory(_saveDataFolder);

        var file = Path.Combine(_saveDataFolder, $"dtp_monsters_{savefile.Slot}.bin");

        using var fs = new FileStream(file, FileMode.Create);
        foreach (var monster in tournamentData.Monsters)
            fs.Write(monster.ToSaveFile());

        Logger.Info($"{savefile} successfully written.");
    }


    private void LoadTournamentData(ISaveFileEntry savefile)
    {
        var file = Path.Combine(_saveDataFolder, $"dtp_monsters_{savefile.Slot}.bin");

        if (!File.Exists(file)) return;

        List<TournamentMonster> monsters = [];

        var rawabd = new byte[100];

        using var fs = new FileStream(file, FileMode.Open);
        var remaining = fs.Length / 100;
        while (remaining > 0)
        {
            remaining--;

            fs.ReadExactly(rawabd, 0, 100);
            monsters.Add(new TournamentMonster(rawabd));
        }

        tournamentData.LoadSavedTournamentData(monsters);
    }


    private void SetupUpdateHook(nint parent)
    {
        _updateHook!.OriginalFunction(parent);
        var newWeek = false;
        Memory.Instance.Read(_addressCurrentweek, out uint currentWeek);
        if (_gameCurrentWeek != currentWeek)
        {
            _gameCurrentWeek = currentWeek;
            newWeek = true;
        }

        // Unfortunately the ordering of these function calls matters so we have to do this shuffling depending on if the game week progressed.
        if (newWeek) GetUnlockedMonsters(_addressUnlockedmonsters);
        // LoadGameUpdateTournamentData();
        if (newWeek) AdvanceWeekUpdateTournamentMonsters(_unlockedmonsters);
        UpdateMemoryTournamentData(_addressTournamentmonsters);

        Logger.Trace("Hook Game Update", Color.Red);
    }

    private void GetUnlockedMonsters(nuint unlockAddress)
    {
        var unlocks = new byte[44];
        _unlockedmonsters.Clear();

        switch (_configuration.EnemyTournamentBreed)
        {
            case Config.TournamentBreeds.PlayerOnly:
                unlocks = GetUnlockedMonsters_Player(unlockAddress);
                break;
            case Config.TournamentBreeds.Realistic:
                unlocks = GetUnlockedMonsters_Realistic();
                break;
            case Config.TournamentBreeds.PlayerOnlyRealistic:
            {
                unlocks = GetUnlockedMonsters_Realistic();
                var ulp = GetUnlockedMonsters_Player(unlockAddress);

                // This uses some special logic for Unique Monsters. I do not respect the player unlock flag here (which is always true). Otherwise, it's just if either case is true they will show up.
                for (var i = 0; i < 38; i++) unlocks[i] = (byte)(unlocks[i] | ulp[i]);
                break;
            }
            case Config.TournamentBreeds.WildWest:
            {
                for (var i = 0; i < unlocks.Length; i++) unlocks[i] = 0x01;
                break;
            }
        }


        for (var i = 0; i < unlocks.Length; i++)
            if (unlocks[i] == 0x01)
            {
                _unlockedmonsters.Add((MonsterGenus)i);
                Logger.Trace("Unlocked Monster Check: " + i, Color.Pink);
            }
    }

    private void UpdateMemoryTournamentData(nuint tournamentAddress)
    {
        var checkPattern = true;
        nuint taddr = 0;
        while (checkPattern)
        {
            taddr = (nuint)_memoryScanner.FindPattern("06 00 00 00 00 00 00 00 20 00 00 00 F4 04 00 00", (int)taddr + 1)
                .Offset;

            if (taddr == 0xffffffff)
            {
                checkPattern = false;
                break;
            }

            var enemyAddresses = _gameAddress + taddr + 0xA8C;
            var monsters = tournamentData.GetTournamentMembers(1, 118);
            for (var i = 1; i <= 118; i++)
                Memory.Instance.WriteRaw(enemyAddresses + (nuint)(i * 60), tournamentData.Monsters[i - 1].Serialize());
        }
    }

    private void AdvanceWeekUpdateTournamentMonsters(List<MonsterGenus> unlockedmonsters)
    {
        Logger.Debug("Advancing to week " + _gameCurrentWeek, Color.Blue);
        tournamentData.AdvanceWeek(_gameCurrentWeek, unlockedmonsters);
    }

    /// <summary>
    ///     This function replaces an assembly jump with two nops. The jmp originally checked the id of the monster against
    ///     some bizarre set of mathematics that determined whether the monster was a 'default' species (i.e., unlocked at the
    ///     beginning of the game).
    ///     The game has flags that map to each species unlock requirements. Removing this jmp call results in the game
    ///     checking all species against these flags. For normal gameplay, this does nothing. This 'fix' would only be useful
    ///     in the instance where
    ///     other mods restrict the main breeds available to each player, (i.e., disabling access to Pixie by setting the flag
    ///     to 0 instead of the 1 it defaults to).
    /// </summary>
    /// <param name="scanner"></param>
    private void AlterCode_CheckShrineUnlockRequirements(IStartupScanner scanner)
    {
        scanner.AddMainModuleScan("55 8B EC 53 8B 5D 08 8A C3 24 03 02 C0 56 57 8B F9 BE 01 00 00 00 B1 07", result =>
        {
            var addr = (nuint)(Base.Mod.Base.ExeBaseAddress + result.Offset);
            Memory.Instance.SafeWrite(addr + 0x2f, (ushort)0x9090);
        });
    }

    // +0x13E is where the first compare to 3 happens
    // 03 C2
    // 8D 04 40
    // 0x13E - 83 F8 03
    // 7D 07
    // 0x143 - B8 03000000

    private void AlterCode_TournamentLifespanIndex(IStartupScanner scanner)
    {
        scanner.AddMainModuleScan("55 8B EC 81 EC D0 00 00 00 A1 ?? ?? ?? ?? 33 C5 89 45 ?? A1 ?? ?? ?? ?? 53",
            result =>
            {
                var addr = (nuint)(Base.Mod.Base.ExeBaseAddress + result.Offset);
                Memory.Instance.SafeWrite(addr + 0x13E + 0x2, (byte)_configuration.LifespanReduction);
                Memory.Instance.SafeWrite(addr + 0x143 + 0x1, (byte)_configuration.LifespanReduction);
            });
    }

    #region Reloaded Templating

    /// <summary>
    ///     Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    ///     Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    ///     Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    #endregion

    #region GetUnlockedMonsters

    private byte[] GetUnlockedMonsters_Player(nuint unlockAddress)
    {
        Memory.Instance.ReadRaw(unlockAddress, out var punlock,
            44); // Technically only the first 38 bytes are used, but we need them for the return.
        for (var i = 38; i < 44; i++) punlock[i] = 1;
        return punlock;
    }

    private byte[] GetUnlockedMonsters_Realistic()
    {
        // Most of these are psuedo random. Improvements that could be made are:
        // Phoenix - Unlock when the first Expedition is completed.
        // Joker and Jill - Tie to Phoenix's date.
        // FIMBA - Actually tie the unlock date to the year of the tournament. I'm just using three for now.
        // Beaclon - Tie to the FIMBA Date as well but with a delay.
        // Metalner - I've never even seen this even so perhaps tie it to that?

        var unlocks = new byte[44];
        unlocks[(int)MonsterGenus.Pixie] = 1;
        unlocks[(int)MonsterGenus.Dragon] = (byte)(_gameCurrentWeek >= 48 * 8 ? 1 : 0);
        unlocks[(int)MonsterGenus.Centaur] = (byte)(_gameCurrentWeek >= 48 * 90 ? 1 : 0);
        unlocks[(int)MonsterGenus.ColorPandora] = 1;
        unlocks[(int)MonsterGenus.Beaclon] = (byte)(_gameCurrentWeek >= 48 * 40 ? 1 : 0); // FIMBA+
        unlocks[(int)MonsterGenus.Henger] = (byte)(_gameCurrentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Wracky] = (byte)(_gameCurrentWeek >= 48 * 60 ? 1 : 0);
        unlocks[(int)MonsterGenus.Golem] = 1; // They're just large.
        unlocks[(int)MonsterGenus.Zuum] = 1;
        unlocks[(int)MonsterGenus.Durahan] = (byte)(_gameCurrentWeek >= 48 * 50 ? 1 : 0);
        unlocks[(int)MonsterGenus.Arrowhead] = 1;
        unlocks[(int)MonsterGenus.Tiger] = 1;
        unlocks[(int)MonsterGenus.Hopper] = 1;
        unlocks[(int)MonsterGenus.Hare] = 1;
        unlocks[(int)MonsterGenus.Baku] = 1; // They're just large.
        unlocks[(int)MonsterGenus.Gali] = (byte)(_gameCurrentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Kato] = 1;
        unlocks[(int)MonsterGenus.Zilla] = (byte)(_gameCurrentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.Bajarl] = (byte)(_gameCurrentWeek >= 48 * 70 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mew] = (byte)(_gameCurrentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Phoenix] = (byte)(_gameCurrentWeek >= 48 * 15 ? 1 : 0);
        unlocks[(int)MonsterGenus.Ghost] = (byte)(_gameCurrentWeek >= 48 * 2 ? 1 : 0);
        unlocks[(int)MonsterGenus.Metalner] = (byte)(_gameCurrentWeek >= 48 * 100 ? 1 : 0);
        unlocks[(int)MonsterGenus.Suezo] = 1;
        unlocks[(int)MonsterGenus.Jill] = (byte)(_gameCurrentWeek >= 48 * 48 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mocchi] = 1;
        unlocks[(int)MonsterGenus.Joker] = (byte)(_gameCurrentWeek >= 48 * 36 ? 1 : 0);
        unlocks[(int)MonsterGenus.Gaboo] = 1;
        unlocks[(int)MonsterGenus.Jell] = 1;
        unlocks[(int)MonsterGenus.Undine] = (byte)(_gameCurrentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.Niton] = (byte)(_gameCurrentWeek >= 48 * 5 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mock] = (byte)(_gameCurrentWeek >= 48 * 20 ? 1 : 0);
        unlocks[(int)MonsterGenus.Ducken] = (byte)(_gameCurrentWeek >= 48 * 4 ? 1 : 0);
        unlocks[(int)MonsterGenus.Plant] = 1;
        unlocks[(int)MonsterGenus.Monol] = 1;
        unlocks[(int)MonsterGenus.Ape] = 1;
        unlocks[(int)MonsterGenus.Worm] = (byte)(_gameCurrentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Naga] = 1;
        unlocks[(int)MonsterGenus.XX] = (byte)(_gameCurrentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.XY] = (byte)(_gameCurrentWeek >= 48 * 50 ? 1 : 0);
        unlocks[(int)MonsterGenus.XZ] = (byte)(_gameCurrentWeek >= 48 * 70 ? 1 : 0);
        unlocks[(int)MonsterGenus.YX] = (byte)(_gameCurrentWeek >= 48 * 90 ? 1 : 0);
        unlocks[(int)MonsterGenus.YY] = (byte)(_gameCurrentWeek >= 48 * 110 ? 1 : 0);
        unlocks[(int)MonsterGenus.YZ] = (byte)(_gameCurrentWeek >= 48 * 120 ? 1 : 0);
        return unlocks;
    }

    #endregion


    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    /// <summary>
    ///     Returns true if the suspend functionality is supported, else false.
    /// </summary>
    public override bool CanSuspend()
    {
        return true;
    }


    public override bool CanUnload()
    {
        return true;
    }


    public override void Suspend()
    {
        var redirector = _modLoader.GetController<IRedirectorController>();
        if (redirector != null && redirector.TryGetTarget(out var re)) re.Disable();
    }


    public override void Unload()
    {
        Suspend();
    }


    public override void Resume()
    {
        var redirector = _modLoader.GetController<IRedirectorController>();
        if (redirector != null && redirector.TryGetTarget(out var re)) re.Enable();
    }

    #endregion
}