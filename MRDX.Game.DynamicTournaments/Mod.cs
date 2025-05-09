using System.Diagnostics;
using System.Drawing;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public class Mod : ModBase // <= Do not Remove.
{
    private readonly IGame? _game;

    private readonly LearningTesting? _lt;

    private readonly WeakReference<IRedirectorController>? _redirector;
    private string _saveDataFolder;

    private uint _gameCurrentWeek;

    private string? _gamePath;

    private TournamentData? _tournamentData;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _saveDataFolder = Path.Combine(_modLoader.GetDirectoryForModId(_modConfig.ModId), "SaveData");
        Directory.CreateDirectory(_saveDataFolder);

        var iHooks = _modLoader.GetController<IHooks>();
        _redirector = _modLoader.GetController<IRedirectorController>();

        var startupScanner = _modLoader.GetController<IStartupScanner>();
        //var memscanner = _modLoader.GetController<IScanner>();
        //_startupScanner = startupScanner;

        // _gameAddress = (nuint)Base.Mod.Base.ExeBaseAddress;
        //
        // _addressUnlockedmonsters = _gameAddress + 0x3795A2;
        // _addressTournamentmonsters = _gameAddress + 0x548D10;
        // _addressCurrentweek = _gameAddress + 0x379444;
        //548CD0

        //Debugger.Launch();
        Logger.SetLogLevel( _configuration.LogLevel );

        if (iHooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get hook controller.", Color.Red);
            return;
        }

        if (startupScanner != null)
        {
            startupScanner.TryGetTarget(out var scanner);
            AlterCode_CheckShrineUnlockRequirements(scanner);
            AlterCode_TournamentLifespanIndex(scanner);
        }

        var maybeSaveFile = _modLoader.GetController<ISaveFile>();
        if (maybeSaveFile != null && maybeSaveFile.TryGetTarget(out var saveFile))
        {
            saveFile.OnSave += SaveTournamentData;
            saveFile.OnLoad += LoadTournamentData;
        }

        var maybeGame = _modLoader.GetController<IGame>();
        if (maybeGame != null && maybeGame.TryGetTarget(out _game))
        {
            _game.OnWeekChange += WeekChangeCallback;
            _game.OnMonsterBreedsLoaded.Subscribe(MonsterBreedsLoaded);
        }

        
        Logger.Trace("Setting up learning testing callback");

        var maybeHooks = _modLoader.GetController<IHooks>();
        if (maybeHooks != null && maybeHooks.TryGetTarget(out var hooks))
            _lt = new LearningTesting(hooks)
            {
                _tournamentStatBonus = _configuration.StatGrowth > 0
                    ? _configuration.StatGrowth + 1
                    : 0
            };


    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    /// <summary>
    ///     Processes data that requires the extraction of the binary first.
    ///     This will be called after the MonsterBreed.AllBreeds list is populated
    /// </summary>
    private void MonsterBreedsLoaded(bool unused)
    {
        Logger.Info("Beginning launch of Dynamic Tournaments");
        // It will be extracted so this will not be null at this point
        _gamePath = IExtractDataBin.ExtractedPath!;
        Logger.Trace("Making new tournament data");
        _tournamentData = new TournamentData(_gamePath, _configuration);


        if (_redirector != null && _redirector.TryGetTarget(out var redirect))
        {
            // Temporarily disable the redirector so we can get the basic monster data
            redirect.Disable();
            _tournamentData.SetupTournamentParticipantsFromTaikai();
            redirect.Enable();

            var tournamentMonsterFile = _gamePath + @"\mf2\data\taikai\taikai_en.flk";
            var enemyFileRedirected = $@"{_saveDataFolder}\taikai_en.flk";
            redirect.AddRedirect(tournamentMonsterFile, enemyFileRedirected);
        }
        else
        {
            Logger.Error("Could not find redirector! Can't setup enemy monster data.");
        }
    }


    private void SaveTournamentData(ISaveFileEntry savefile)
    {
        if ( _configuration.Autosaves || !savefile.IsAutoSave ) {
            _saveDataFolder = Path.GetDirectoryName( savefile.Filename );
            //Directory.CreateDirectory( _saveDataFolder );
            var file = Path.Combine( _saveDataFolder, $"dtp_monsters_{savefile.Slot}.bin" );

            using var fs = new FileStream( file, FileMode.Create );
            foreach ( var monster in _tournamentData!.Monsters )
                fs.Write( monster.ToSaveFile() );

            Logger.Info( $"{file} successfully written." );
        }
    }


    private void LoadTournamentData(ISaveFileEntry savefile)
    {
        if (_tournamentData == null)
        {
            Logger.Warn("Attempted to load an invalid tournament data before we finished loading.");
            return;
        }

        _saveDataFolder = Path.GetDirectoryName( savefile.Filename );
        var file = Path.Combine(_saveDataFolder, $"dtp_monsters_{savefile.Slot}.bin");

        if (!File.Exists(file)) return;

        try
        {
            List<byte[]> monstersRaw = [];

            

            using var fs = new FileStream(file, FileMode.Open);
            var remaining = fs.Length / 100;
            while (remaining > 0)
            {
                var rawdtpmonster = new byte[ 100 ];
                remaining--;

                fs.ReadExactly( rawdtpmonster, 0, 100);
                monstersRaw.Add( rawdtpmonster );
            }

            _tournamentData.LoadSavedTournamentData(monstersRaw);
        }
        catch (Exception e)
        {
            // Failed to load the extra monsters from the savefile, so load them from the default enemy file
            Logger.Info($"Failed to load savefile ${file} due to the following error (this may be harmless?): ${e.Message}");
            _tournamentData.SetupTournamentParticipantsFromTaikai();
        }
    }


    private void WeekChangeCallback(IWeekChange week)
    {
        if (_tournamentData == null)
            // Can't change weeks before we've finished loading!
            return;

        _gameCurrentWeek = week.NewWeek;
        // Unfortunately the ordering of these function calls matters so we have to do this shuffling depending on if the game week progressed.
        var unlockedmonsters = GetUnlockedMonsters();
        AdvanceWeekUpdateTournamentMonsters(unlockedmonsters);
        UpdateTournamentMonsterFile();
    }

    private List<MonsterGenus> GetUnlockedMonsters()
    {
        List<MonsterGenus> monsters;
        switch (_configuration.EnemyTournamentBreed)
        {
            default:
            case Config.TournamentBreeds.PlayerOnly:
                monsters = _game!.UnlockedMonsters;
                break;
            case Config.TournamentBreeds.Realistic:
                monsters = GetUnlockedMonsters_Realistic();
                break;
            case Config.TournamentBreeds.PlayerOnlyRealistic:
            {
                // This uses some special logic for Unique Monsters.
                // I do not respect the player unlock flag here (which is always true).
                // Otherwise, it's just if either case is true they will show up.
                var skip = Enumerable.Range((int)MonsterGenus.XX, 6)
                    .Select(i => (MonsterGenus)i);

                var set = new HashSet<MonsterGenus>();
                set.UnionWith(GetUnlockedMonsters_Realistic());
                set.UnionWith(_game!.UnlockedMonsters.Except(skip));
                monsters = set.ToList();
                break;
            }
            case Config.TournamentBreeds.WildWest:
            {
                monsters = Enumerable.Range(0, (int)MonsterGenus.YZ + 1).Select(i => (MonsterGenus)i).ToList();
                break;
            }
        }

        return monsters;
    }

    private void UpdateTournamentMonsterFile()
    {
        if (_tournamentData == null || _tournamentData.Monsters.Count < 118)
        {
            Logger.Warn(
                "Cannot update tournament monsters file because we haven't finished generating all monsters yet.");
            return;
        }

        _tournamentData.WriteTournamentParticipantsToTaikai(_saveDataFolder);
    }

    private void AdvanceWeekUpdateTournamentMonsters(List<MonsterGenus> unlockedmonsters)
    {
        Logger.Debug("Advancing to week " + _gameCurrentWeek, Color.Blue);
        _tournamentData.AdvanceWeek(_gameCurrentWeek, unlockedmonsters);
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
        // TODO move this whole function to the base mod
        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        var exeBaseAddress = module.BaseAddress.ToInt64();
        scanner.AddMainModuleScan("55 8B EC 53 8B 5D 08 8A C3 24 03 02 C0 56 57 8B F9 BE 01 00 00 00 B1 07", result =>
        {
            var addr = (nuint)(exeBaseAddress + result.Offset);
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
        // TODO move this whole function to the base mod
        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        var exeBaseAddress = module.BaseAddress.ToInt64();
        scanner.AddMainModuleScan("55 8B EC 81 EC D0 00 00 00 A1 ?? ?? ?? ?? 33 C5 89 45 ?? A1 ?? ?? ?? ?? 53",
            result =>
            {
                var addr = (nuint)(exeBaseAddress + result.Offset);
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

    private List<MonsterGenus> GetUnlockedMonsters_Realistic()
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
        return unlocks.ToArray()
            .Select((m, i) => m != 0 ? (MonsterGenus)i : MonsterGenus.Garbage)
            .Where(m => m != MonsterGenus.Garbage).ToList();
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