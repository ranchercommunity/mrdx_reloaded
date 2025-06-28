using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.MoreMonsters.Configuration;
using MRDX.Game.MoreMonsters.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;

namespace MRDX.Game.MoreMonsters;


[ HookDef( BaseGame.Mr2, Region.Us, "53 56 57 8B F9 8B DA 8B 0D ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_MonsterID ( uint p1, uint p2 );

[HookDef( BaseGame.Mr2, Region.Us, "53 8B DC 83 EC 08 83 E4 F8 83 C4 04 55 8B 6B ?? 89 6C 24 ?? 8B EC 83 EC 10" )]
[Function( CallingConventions.Fastcall)]
public delegate void H_LoadEnemyMonsterData ( nuint self, uint p2, int p3, int p4 );

[HookDef( BaseGame.Mr2, Region.Us, "8A 15 ?? ?? ?? ?? 56 8B F1 57" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_BattleStarting ( nuint self );

/*[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 53 56 8B 35 ?? ?? ?? ?? 57" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_PreShrineCreation ( nint self, int p1, int p2, int p3, int p4 );*/

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 8B 81 ?? ?? ?? ??")]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_MysteryShrine ( nuint self, nuint p2 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 28 0F B6 41 ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_WriteSDATAMemory ( nuint self );

[ HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 50 A1 ?? ?? ?? ?? 33 C5 89 45 ?? 53 56 57 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 8B F9" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_EarlyShrine ( nuint self, nuint p2 );

[HookDef( BaseGame.Mr2, Region.Us, "56 8B F1 8B 46 ?? 8B 08 83 F9 2E" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_ReadSDATA ( nuint self, int p1 );

[HookDef( BaseGame.Mr2, Region.Us, "56 8B F1 57 8A 86 ?? ?? ?? ??")]
[Function( CallingConventions.Fastcall )]
public delegate int H_MysteryStatUpdate ( nuint self );

public class Mod : ModBase // <= Do not Remove.
{
    private readonly IHooks _iHooks;
    private readonly string? _modPath;

    private readonly IRedirectorController _redirector;

    private string? _dataPath;

    public byte _itemGiveHookCount;
    public bool _itemGivenSuccess;

    public bool _itemHandleMagicBananas;
    public byte _itemIdGiven = 0;
    public byte _itemOriginalIdGiven;

    public bool _snapshotUpdate = true;


    private uint _monsterLastId = 999999 ;
    private IHook<H_MonsterID> _hook_monsterID;
    private IHook<H_LoadEnemyMonsterData> _hook_loadEMData;
    private IHook<H_BattleStarting> _hook_battleStarting;
    private bool _monsterInsideBattleStartup = false;
    private bool _monsterInsideEnemySetup = false;
    private uint _monsterInsideBattleRedirects = 0;
    private uint _monsterInsideBattleMain = 0;
    private uint _monsterInsideBattleSub = 0;
    public bool monsterReplaceEnabled = false;
    public bool retriggerReplacement = false;

    //private IHook<H_PreShrineCreation> _hook_preShrineCreation;
    //private IHook<H_MysteryShrine> _hook_mysteryShrine;
    private IHook<H_EarlyShrine> _hook_earlyShrine;
    private IHook<H_WriteSDATAMemory> _hook_writeSDATAMemory;
    //private IHook<H_ReadSDATA> _hook_readSDATA;
    private IHook<H_MysteryStatUpdate> _hook_statUpdate;

    public bool shrineReplacementActive = false;
    private MMBreed _shrineReplacementMonster;
    private readonly IMonster _monsterCurrent;

    private List<MMBreed> _monsterBreeds = new List<MMBreed>();
    private Dictionary<int, MMBreed> _songIDMapping = new Dictionary<int, MMBreed>();


    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _modPath = _modLoader.GetDirectoryForModId(_modConfig.ModId);

        _modLoader.GetController<IRedirectorController>().TryGetTarget(out _redirector);
        _modLoader.GetController<IHooks>().TryGetTarget(out _iHooks);
        _modLoader.GetController<IGame>().TryGetTarget(out var iGame);
        _modLoader.GetController<IExtractDataBin>().TryGetTarget(out var extract);

        if (extract == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get extract data bin controller.", Color.Red);
            return;
        }
        extract.ExtractComplete.Subscribe( RedirectorSetupDataPath );

        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;

        if (_redirector == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get redirector controller.", Color.Red);
            return;
        }

        if (_iHooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get hook controller.", Color.Red);
            return;
        }

        if (iGame == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get iGame controller.", Color.Red);
            return;
        }

        _monsterCurrent = iGame.Monster;

        _iHooks.AddHook<H_MonsterID>( SetupHookMonsterID ).ContinueWith( result => _hook_monsterID = result.Result );
        _iHooks.AddHook<H_LoadEnemyMonsterData>( SetupHookLoadEMData ).ContinueWith( result => _hook_loadEMData = result.Result );
        _iHooks.AddHook<H_BattleStarting>( SetupBattleStarting ).ContinueWith( result => _hook_battleStarting = result.Result );


        _iHooks.AddHook<H_EarlyShrine>( SetupEarlyShrine ).ContinueWith( result => _hook_earlyShrine = result.Result );
        _iHooks.AddHook<H_WriteSDATAMemory>(SetupOverwriteSDATA).ContinueWith( result => _hook_writeSDATAMemory = result.Result );
        _iHooks.AddHook<H_MysteryStatUpdate>( SetupMysteryStat ).ContinueWith( result => _hook_statUpdate = result.Result );


        WeakReference<IRedirectorController> _redirectorx = _modLoader.GetController<IRedirectorController>();
        _redirectorx.TryGetTarget( out var redirect );
        if ( redirect == null ) { _logger.WriteLine( $"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red ); return; }
        else { redirect.Loading += ProcessReloadedFileLoad; }

        InitializeNewMonsters();
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private void RedirectorSetupDataPath ( string? extractedPath ) {
        _dataPath = extractedPath;
    }

    /// <summary>
    ///  This function should eventually read from a file or some equivalent.
    /// </summary>
    private void InitializeNewMonsters () {
        // Zuum-Henger
        MMBreed breed = new MMBreed( MonsterGenus.Zuum, MonsterGenus.Henger, MonsterGenus.Zuum, MonsterGenus.Arrowhead );
        breed.NewVariant( 330, 45, LifeType.Normal, 
            120, 130, 90, 150, 120, 110, 
            2, 2, 2, 3, 2, 1, 
            2, 14, -1, -1, 12 );
        _songIDMapping.Add( 1262719, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Zuum, MonsterGenus.Undine, MonsterGenus.Zuum, MonsterGenus.Dragon );
        breed.NewVariant( 330, 50, LifeType.Sustainable, 
            100, 85, 100, 150, 115, 70, 
            2, 1, 2, 4, 2, 1, 
            3, 11, 3, 10001, 6 );
        _songIDMapping.Add( 1262724, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Zilla, MonsterGenus.Dragon, MonsterGenus.Zilla, MonsterGenus.Zilla );
        breed.NewVariant( 320, -45, LifeType.Precocious, 
            210, 180, 130, 100, 70, 130,
            4, 4, 3, 1, 0, 3, 0, 
            19, 3, 1001, 1024 );
        _songIDMapping.Add( 1262729, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Jell, MonsterGenus.Gaboo, MonsterGenus.Jell, MonsterGenus.Jell );
        breed.NewVariant( 350, 15, LifeType.Sustainable,
            145, 125, 60, 95, 115, 110,
            3, 3, 1, 2, 1, 3,
            2, 14, 3, 10001, 32 );
        _songIDMapping.Add( 1262737, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Undine, MonsterGenus.Dragon, MonsterGenus.Undine, MonsterGenus.Undine );
        breed.NewVariant( 280, -35, LifeType.Precocious,
            90, 130, 145, 135, 100, 70,
            2, 3, 3, 3, 2, 2,
            2, 13, 3, 10000001, 2 );
        _songIDMapping.Add( 1262738, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Ghost, MonsterGenus.Joker, MonsterGenus.Ghost, MonsterGenus.Ghost );
        breed.NewVariant( 280, -70, LifeType.Sustainable,
            105, 110, 175, 175, 130, 70,
            1, 2, 4, 4, 2, 0,
            3, 9, 3, 11, 16);
        _songIDMapping.Add( 1262740, breed );
        _monsterBreeds.Add( breed );

        breed = new MMBreed( MonsterGenus.Monol, MonsterGenus.Mock, MonsterGenus.Monol, MonsterGenus.Monol );
        breed.NewVariant( 280, -70, LifeType.Sustainable,
            105, 110, 175, 175, 130, 70,
            1, 2, 4, 4, 2, 0,
            3, 9, 3, 11, 16 );
        _songIDMapping.Add( 1262743, breed );
        _monsterBreeds.Add( breed );

        // TODO : Monster Moves and Battle Specials seem to be non-functioning?

        /*Songs to use
         * 1262745	1262749	1262752	1262762	1262766	1262768	1262770	1262783	1262789	989884
         * 989885 989886 989887 989888 989889  989890 989891 989892 989893 989894 
         * 989895 989896 989897 989898 989899 989900*/
    }

    private int SetupHookMonsterID ( uint breedIdMain, uint breedIdSub ) {

        _logger.WriteLineAsync( $"Getting Monster ID: {breedIdMain} : {breedIdSub} : C{_monsterLastId}", Color.Aqua );

        if ( shrineReplacementActive ) {
            breedIdMain = (uint) _shrineReplacementMonster._genusNewMain;
            breedIdSub = (uint) _shrineReplacementMonster._genusNewSub;
        }

        MonsterGenus breedMain = (MonsterGenus) breedIdMain;
        MonsterGenus breedSub = (MonsterGenus) breedIdSub;


        if ( _monsterInsideBattleStartup ) { 
            _monsterInsideBattleRedirects++; 
            if ( _monsterInsideBattleRedirects == 2 ) {
                _monsterInsideBattleMain = breedIdMain;
                _monsterInsideBattleSub = breedIdSub;
            }
        }

        if ( !_monsterInsideBattleStartup || _monsterInsideBattleRedirects == 1 ) {
            RedirectFromID( breedIdMain, breedIdSub );
        }

        foreach ( MMBreed breed in _monsterBreeds ) {
            if ( breed.MatchNewBreed(breedMain, breedSub) ) {
                return _hook_monsterID!.OriginalFunction( (uint) breed._genusBaseMain, (uint) breed._genusBaseSub );
            }
        }
        return _hook_monsterID!.OriginalFunction( breedIdMain, breedIdSub );

    }

    private void RedirectFromID ( uint breedIdMain, uint breedIdSub ) {
        MonsterGenus breedMain = (MonsterGenus) breedIdMain;
        MonsterGenus breedSub = (MonsterGenus) breedIdSub;

        _logger.WriteLineAsync( $"Running Redirect Script: {breedIdMain}/{breedIdSub}", Color.Lime );

        foreach ( MMBreed breed in _monsterBreeds ) {
            if ( breed.MatchNewBreed(breedMain, breedSub ) ) {
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed._filepathBase + ".tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed._filepathNew + ".tex" );
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed._filepathBase + "_bt.tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed._filepathNew + "_bt.tex" );
                return;
            }

            if ( breed.MatchBaseBreed(breedMain, breedSub) ) {
                _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\" + breed._filepathBase + ".tex");
                _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\" + breed._filepathBase + "_bt.tex" );
                return;
            }
        }
    }

    /// <summary>
    /// This function is called when enemy monster data is being loaded and we need to swap redirects.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="p4"></param>
    private void SetupHookLoadEMData ( nuint self, uint p2, int p3, int p4 ) {
        //_logger.WriteLineAsync( $"Loading EM Data: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = true;

        _hook_loadEMData!.OriginalFunction( self, p2, p3, p4 );
        //_logger.WriteLineAsync( $"EM Data Call Done: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = false;
    }

    /// <summary>
    /// This function is called at the beginning of battle preparation. We can use it to know exactly when multiple monster models will be loaded and to start scanning filereads appropriately.
    /// </summary>
    /// <param name="self"></param>
    private void SetupBattleStarting ( nuint self ) {
        _monsterInsideBattleStartup = true;
        _monsterInsideBattleRedirects = 0;
        //_logger.WriteLineAsync( $"BATTLE STARTING!!!!!!!!!!!!!!!!!!!!!", Color.Red );

        _hook_battleStarting!.OriginalFunction( self );
        //_logger.WriteLineAsync( $"BATTLE STARTING OVER !!!!!!!!!!!!!!", Color.Red );
        _monsterInsideBattleStartup = false;
    }


    private void SetupEarlyShrine ( nuint self, nuint p2 ) {
        
        _logger.WriteLineAsync( $"ESHRINE: {self} {p2}", Color.Yellow );
        _hook_earlyShrine!.OriginalFunction( self, p2 );
        Memory.Instance.Read( nuint.Add( self, 0xcc ), out int songID );
        _logger.WriteLineAsync( $"ESHRINE: {self} {p2} {songID}", Color.Yellow );

        foreach ( var songMap in _songIDMapping ) {
            if ( songID == songMap.Key ) {
                shrineReplacementActive = true;
                _shrineReplacementMonster = songMap.Value;
            }
        }
    }

    private void SetupOverwriteSDATA ( nuint self ) {

        _hook_writeSDATAMemory!.OriginalFunction( self );
        if ( shrineReplacementActive ) {
            Memory.Instance.Read( nuint.Add(self, 0x44), out nuint breedLoc );
            Memory.Instance.Write( breedLoc + 0x48, (byte) _shrineReplacementMonster._genusNewMain );
            Memory.Instance.Write( breedLoc + 0x49, (byte) _shrineReplacementMonster._genusNewSub );
        }
    }

    /*private int SetupReadSData ( nuint self, int p1 ) {
        
        var ret = _hook_readSDATA!.OriginalFunction( self, p1 );
        _logger.WriteLine( $"RSDAT: {self} {p1} {ret}", Color.Azure );

        _monsterCurrent.GenusMain = MonsterGenus.Zuum;
        _monsterCurrent.GenusSub = MonsterGenus.Henger;
        _monsterCurrent.Life = 999;

        return ret;
    }*/

    private int SetupMysteryStat ( nuint self) {

        var ret = _hook_statUpdate!.OriginalFunction( self );

        if ( shrineReplacementActive ) {
            // TODO - Choose Random Variant or something akin to that.
            var variant = _shrineReplacementMonster._monsterVariants[ 0 ];
            _monsterCurrent.GenusMain = variant.GenusMain;
            _monsterCurrent.GenusSub = variant.GenusSub;

            _monsterCurrent.Lifespan = variant.Lifespan;
            _monsterCurrent.InitalLifespan = variant.InitalLifespan;

            _monsterCurrent.NatureRaw = variant.NatureRaw;
            _monsterCurrent.NatureBase = variant.NatureBase;

            _monsterCurrent.LifeType = variant.LifeType;

            _monsterCurrent.Life = variant.Life;
            _monsterCurrent.Power = variant.Power;
            _monsterCurrent.Intelligence = variant.Intelligence;
            _monsterCurrent.Skill = variant.Skill;
            _monsterCurrent.Speed = variant.Speed;
            _monsterCurrent.Defense = variant.Defense;

            _monsterCurrent.GrowthRateLife = variant.GrowthRateLife;
            _monsterCurrent.GrowthRatePower = variant.GrowthRatePower;
            _monsterCurrent.GrowthRateIntelligence = variant.GrowthRateIntelligence;
            _monsterCurrent.GrowthRateSkill = variant.GrowthRateSkill;
            _monsterCurrent.GrowthRateSpeed = variant.GrowthRateSpeed;
            _monsterCurrent.GrowthRateDefense = variant.GrowthRateDefense;

            _monsterCurrent.ArenaSpeed = variant.ArenaSpeed;
            _monsterCurrent.GutsRate = variant.GutsRate;

            _monsterCurrent.TrainBoost = variant.TrainBoost;

            // Battle Specials and Moves not supported yet.

            shrineReplacementActive = false;
        }
        return ret;
    }

    private void ProcessReloadedFileLoad ( string filename ) {
        //_logger.WriteLineAsync( $"Any file check {_monsterInsideBattleStartup}, {_monsterInsideBattleRedirects}, {_monsterInsideBattleMain}, {_monsterInsideBattleSub}", Color.Orange );
        if ( _monsterInsideBattleStartup ) {
            //_logger.WriteLineAsync( $"Inside File Checking for Monsters", Color.Orange );
            if ( _monsterInsideBattleRedirects == 1 ) {
                RedirectFromID( _monsterInsideBattleMain, _monsterInsideBattleSub );
            }

            if ( filename.Contains( "_bt.tex" ) && filename.Contains( "mf2\\data\\mon" ) ) {

                _monsterInsideBattleRedirects--;
                //_logger.WriteLineAsync( $"Inside File Checking Decrementing redirects {_monsterInsideBattleRedirects}", Color.Orange );
            }
        }
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Configuration.Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    #region Reloaded Template

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
}