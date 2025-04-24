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

    private readonly nuint _address_currentweek;
    private readonly nuint _address_tournamentmonsters;
    private readonly nuint _address_unlockedmonsters;

    private readonly string _gamePath = "";
    private readonly IHooks _iHooks;
    private readonly IScanner _memoryScanner;

    private readonly WeakReference<IRedirectorController> _redirector;

    private readonly SaveFileManager _saveFileManager;

    private readonly List<MonsterGenus> _unlockedmonsters;
    private readonly nuint gameAddress;
    private uint _game_currentWeek;

    private LearningTesting _LT;

    //private IHook<CheckShrineUnlockRequirementHook>? _shrineUnlockHook;
    //private bool monsterUnlockCheckDefaults = false;

    private IStartupScanner _startupScanner;

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

        _saveFileManager = new SaveFileManager(this, _modLoader, _modConfig, _logger, _configuration.Autosaves);

        _redirector = _modLoader.GetController<IRedirectorController>();
        _modLoader.GetController<IExtractDataBin>().TryGetTarget(out var extract);
        _modLoader.GetController<IHooks>().TryGetTarget(out _iHooks);

        var startupScanner = _modLoader.GetController<IStartupScanner>();
        //var memscanner = _modLoader.GetController<IScanner>();
        //_startupScanner = startupScanner;

        gameAddress = (nuint)Base.Mod.Base.ExeBaseAddress;

        _address_unlockedmonsters = gameAddress + 0x3795A2;
        _address_tournamentmonsters = gameAddress + 0x548D10;
        _address_currentweek = gameAddress + 0x379444;
        //548CD0

        _unlockedmonsters = new List<MonsterGenus>();

        if (extract == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get extract data bin controller.", Color.Red);
            return;
        }

        _gamePath = extract.ExtractedPath;

        _redirector.TryGetTarget(out var redirect);
        if (redirect == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red);
            return;
        }

        redirect.Loading += ProcessReloadedFileLoad;


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

        HandleExtraction(extract);

        //Debugger.Launch();
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    /// <summary>
    ///     Attempts to perform a file read. If it fails, we assume that the binary has not been extracted and begin the
    ///     extraction process.
    ///     If it succeeds, then simply process the extracted data.
    /// </summary>
    /// <param name="extract"></param>
    private void HandleExtraction(IExtractDataBin extract)
    {
        try
        {
            var fs = File.OpenRead(_gamePath + "\\mf2\\data\\mon\\kapi\\" + "ka_ka_wz.bin");
            fs.Close();
            ProcessExtractedData();
        }

        catch
        {
            extract.ExtractComplete += path => { ProcessExtractedData(); };
        }
    }

    /// <summary>
    ///     Processes data that requires the extraction of the binary first. Can be called two separate ways.
    /// </summary>
    private void ProcessExtractedData()
    {
        tournamentData = new TournamentData(this, _logger, _configuration);
        SetupMonsterBreeds();
        SetupTournamentParticipantsFromTaikai();

        _LT = new LearningTesting(_iHooks, gameAddress);
        _LT._tournamentStatBonus = _configuration.StatGrowth > 0
            ? _configuration.StatGrowth + 1
            : 0;
    }

    private void ProcessReloadedFileLoad(string filename)
    {
        _saveFileManager.SaveDataMonitor(filename);
    }

    /// <summary>
    ///     Parses data folders looking for monster texture files. This is how we build our valid breed list for generating
    ///     tournament monsters.
    /// </summary>
    private void SetupMonsterBreeds()
    {
        // MonsterBreed.SetupMonsterBreedList(_gamePath);
        // TournamentMonster._configuration = _configuration;
    }

    /// <summary>
    ///     Loads the taikai_en.flk file and generates the TournamentData from it. Is loaded at startup and when a new save
    ///     without save data is loaded.
    /// </summary>
    private void SetupTournamentParticipantsFromTaikai()
    {
        tournamentData.ClearAllData();

        var tournamentMonsterFile = _gamePath + "\\mf2\\data\\taikai\\taikai_en.flk";
        var rawmonster = new byte[60];

        using var fs = new FileStream(tournamentMonsterFile, FileMode.Open);
        fs.Position = 0xA8C + 60; // This relies upon nothing earlier in the file being appended. 
        for (var i = 1; i < 120; i++)
        {
            // 0 = Dummy Monster so skip. 119 in the standard file.
            fs.ReadExactly(rawmonster, 0, 60);
            TournamentMonster tm = new(rawmonster);
            tournamentData.AddExistingMonster(tm, i);

            // var bytes = "";
            // for (var z = 0; z < 60; z++) bytes += rawmonster[z] + ",";
            Logger.Trace("Monster " + i + " Parsed: " + tm, Color.Lime);
        }

        tournamentData._initialized = true;
    }

    private void SetupUpdateHook(nint parent)
    {
        _updateHook!.OriginalFunction(parent);
        var newWeek = false;
        Memory.Instance.Read(_address_currentweek, out uint currentWeek);
        if (_game_currentWeek != currentWeek)
        {
            _game_currentWeek = currentWeek;
            newWeek = true;
        }

        // Unfortunately the ordering of these function calls matters so we have to do this shuffling depending on if the game week progressed.
        if (newWeek) GetUnlockedMonsters(_address_unlockedmonsters);
        LoadGameUpdateTournamentData();
        if (newWeek) AdvanceWeekUpdateTournamentMonsters(_unlockedmonsters);
        UpdateMemoryTournamentData(_address_tournamentmonsters);

        Logger.Trace("Hook Game Update", Color.Red);
    }

    private void GetUnlockedMonsters(nuint unlockAddress)
    {
        var unlocks = new byte[44];
        _unlockedmonsters.Clear();

        if (_configuration._confABD_tournamentBreeds == Config.E_ConfABD_TournamentBreeds.PlayerOnly)
        {
            unlocks = GetUnlockedMonsters_Player(unlockAddress);
        }

        else if (_configuration._confABD_tournamentBreeds == Config.E_ConfABD_TournamentBreeds.Realistic)
        {
            unlocks = GetUnlockedMonsters_Realistic();
        }

        else if (_configuration._confABD_tournamentBreeds == Config.E_ConfABD_TournamentBreeds.PlayerOnlyRealistic)
        {
            unlocks = GetUnlockedMonsters_Realistic();
            var ulp = GetUnlockedMonsters_Player(unlockAddress);

            // This uses some special logic for Unique Monsters. I do not respect the player unlock flag here (which is always true). Otherwise, it's just if either case is true they will show up.
            for (var i = 0; i < 38; i++) unlocks[i] = (byte)(unlocks[i] | ulp[i]);
        }

        else if (_configuration._confABD_tournamentBreeds == Config.E_ConfABD_TournamentBreeds.WildWest)
        {
            for (var i = 0; i < unlocks.Length; i++) unlocks[i] = 0x01;
        }


        for (var i = 0; i < unlocks.Length; i++)
            if (unlocks[i] == 0x01)
            {
                _unlockedmonsters.Add((MonsterGenus)i);
                Logger.Trace("Unlocked Monster Check: " + i, Color.Pink);
            }
    }

    private void LoadGameUpdateTournamentData()
    {
        if (_saveFileManager._saveData_gameLoaded)
        {
            Logger.Info("Game Load Detected", Color.Orange);
            var monsters = _saveFileManager.LoadABDTournamentData();
            if (monsters.Count == 0)
            {
                Logger.Trace("No custom tournament data found. Loading taikai_en.", Color.Orange);
                SetupTournamentParticipantsFromTaikai();
            }
            else
            {
                Logger.Trace("Found Data for " + monsters.Count + " monsters.", Color.Orange);
                tournamentData.ClearAllData();
                foreach (var abdm in monsters) tournamentData.AddExistingMonster(abdm);
            }

            tournamentData._initialized = true;
            tournamentData._firstweek = true;
            Logger.Info("Initialization Complete", Color.Orange);
        }
    }

    /// <summary>  </summary>
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

            var enemyAddresses = gameAddress + taddr + 0xA8C;
            var monsters = tournamentData.GetTournamentMembers(1, 118);
            for (var i = 1; i <= 118; i++)
                Memory.Instance.WriteRaw(enemyAddresses + (nuint)(i * 60), monsters[i - 1].monster.raw_bytes);
        }
    }

    private void AdvanceWeekUpdateTournamentMonsters(List<MonsterGenus> unlockedmonsters)
    {
        Logger.Debug("Advancing to week " + _game_currentWeek, Color.Blue);
        tournamentData.AdvanceWeek(_game_currentWeek, unlockedmonsters);
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
            Memory.Instance.SafeWrite(addr + 0x2f, (ushort)37008);
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
                Memory.Instance.SafeWrite(addr + 0x13E + 0x2, (byte)_configuration._confDTP_tournament_lifespan);
                Memory.Instance.SafeWrite(addr + 0x143 + 0x1, (byte)_configuration._confDTP_tournament_lifespan);
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
        unlocks[(int)MonsterGenus.Dragon] = (byte)(_game_currentWeek >= 48 * 8 ? 1 : 0);
        unlocks[(int)MonsterGenus.Centaur] = (byte)(_game_currentWeek >= 48 * 90 ? 1 : 0);
        unlocks[(int)MonsterGenus.ColorPandora] = 1;
        unlocks[(int)MonsterGenus.Beaclon] = (byte)(_game_currentWeek >= 48 * 40 ? 1 : 0); // FIMBA+
        unlocks[(int)MonsterGenus.Henger] = (byte)(_game_currentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Wracky] = (byte)(_game_currentWeek >= 48 * 60 ? 1 : 0);
        unlocks[(int)MonsterGenus.Golem] = 1; // They're just large.
        unlocks[(int)MonsterGenus.Zuum] = 1;
        unlocks[(int)MonsterGenus.Durahan] = (byte)(_game_currentWeek >= 48 * 50 ? 1 : 0);
        unlocks[(int)MonsterGenus.Arrowhead] = 1;
        unlocks[(int)MonsterGenus.Tiger] = 1;
        unlocks[(int)MonsterGenus.Hopper] = 1;
        unlocks[(int)MonsterGenus.Hare] = 1;
        unlocks[(int)MonsterGenus.Baku] = 1; // They're just large.
        unlocks[(int)MonsterGenus.Gali] = (byte)(_game_currentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Kato] = 1;
        unlocks[(int)MonsterGenus.Zilla] = (byte)(_game_currentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.Bajarl] = (byte)(_game_currentWeek >= 48 * 70 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mew] = (byte)(_game_currentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Phoenix] = (byte)(_game_currentWeek >= 48 * 15 ? 1 : 0);
        unlocks[(int)MonsterGenus.Ghost] = (byte)(_game_currentWeek >= 48 * 2 ? 1 : 0);
        unlocks[(int)MonsterGenus.Metalner] = (byte)(_game_currentWeek >= 48 * 100 ? 1 : 0);
        unlocks[(int)MonsterGenus.Suezo] = 1;
        unlocks[(int)MonsterGenus.Jill] = (byte)(_game_currentWeek >= 48 * 48 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mocchi] = 1;
        unlocks[(int)MonsterGenus.Joker] = (byte)(_game_currentWeek >= 48 * 36 ? 1 : 0);
        unlocks[(int)MonsterGenus.Gaboo] = 1;
        unlocks[(int)MonsterGenus.Jell] = 1;
        unlocks[(int)MonsterGenus.Undine] = (byte)(_game_currentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.Niton] = (byte)(_game_currentWeek >= 48 * 5 ? 1 : 0);
        unlocks[(int)MonsterGenus.Mock] = (byte)(_game_currentWeek >= 48 * 20 ? 1 : 0);
        unlocks[(int)MonsterGenus.Ducken] = (byte)(_game_currentWeek >= 48 * 4 ? 1 : 0);
        unlocks[(int)MonsterGenus.Plant] = 1;
        unlocks[(int)MonsterGenus.Monol] = 1;
        unlocks[(int)MonsterGenus.Ape] = 1;
        unlocks[(int)MonsterGenus.Worm] = (byte)(_game_currentWeek >= 48 * 3 ? 1 : 0); // FIMBA
        unlocks[(int)MonsterGenus.Naga] = 1;
        unlocks[(int)MonsterGenus.Unknown1] = (byte)(_game_currentWeek >= 48 * 40 ? 1 : 0);
        unlocks[(int)MonsterGenus.Unknown2] = (byte)(_game_currentWeek >= 48 * 50 ? 1 : 0);
        unlocks[(int)MonsterGenus.Unknown3] = (byte)(_game_currentWeek >= 48 * 70 ? 1 : 0);
        unlocks[(int)MonsterGenus.Unknown4] = (byte)(_game_currentWeek >= 48 * 90 ? 1 : 0);
        unlocks[(int)MonsterGenus.Unknown5] = (byte)(_game_currentWeek >= 48 * 110 ? 1 : 0);
        unlocks[(int)MonsterGenus.Unknown6] = (byte)(_game_currentWeek >= 48 * 120 ? 1 : 0);
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