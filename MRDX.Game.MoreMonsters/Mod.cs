using System.Diagnostics;
using System.Drawing;
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


[HookDef( BaseGame.Mr2, Region.Us, "53 56 57 8B F9 8B DA 8B 0D ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_MonsterID ( uint p1, uint p2 );

[HookDef( BaseGame.Mr2, Region.Us, "53 8B DC 83 EC 08 83 E4 F8 83 C4 04 55 8B 6B ?? 89 6C 24 ?? 8B EC 83 EC 10" )]
[Function( CallingConventions.Fastcall)]
public delegate void H_LoadEnemyMonsterData ( nuint self, uint p2, int p3, int p4 );

[HookDef( BaseGame.Mr2, Region.Us, "8A 15 ?? ?? ?? ?? 56 8B F1 57" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_BattleStarting ( nuint self );


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

        _iHooks.AddHook<H_MonsterID>( SetupHookMonsterID ).ContinueWith( result => _hook_monsterID = result.Result );
        _iHooks.AddHook<H_LoadEnemyMonsterData>( SetupHookLoadEMData ).ContinueWith( result => _hook_loadEMData = result.Result );
        _iHooks.AddHook<H_BattleStarting>( SetupBattleStarting ).ContinueWith( result => _hook_battleStarting = result.Result );


        WeakReference<IRedirectorController> _redirectorx = _modLoader.GetController<IRedirectorController>();
        _redirectorx.TryGetTarget( out var redirect );
        if ( redirect == null ) { _logger.WriteLine( $"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red ); return; }
        else { redirect.Loading += ProcessReloadedFileLoad; }
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

    private int SetupHookMonsterID ( uint breedIdMain, uint breedIdSub ) {

        _logger.WriteLineAsync( $"$Getting Monster ID: {breedIdMain} : {breedIdSub} : C{_monsterLastId}", Color.Aqua );

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

        if ( breedIdMain == 8 && breedIdSub == 5 ) {
            return _hook_monsterID!.OriginalFunction( breedIdMain, 10 );
        }

        else if ( breedIdMain == 8 && breedIdSub == 29 ) {
            return _hook_monsterID!.OriginalFunction( breedIdMain, 1 );
        }

        else if ( breedIdMain == 17 && breedIdSub == 1 ) { // Zilla - Dragon
            return _hook_monsterID!.OriginalFunction( breedIdMain, 17 );
        }

        else if ( breedIdMain == 28 && breedIdSub == 27 ) { // Jell - Gaboo
            return _hook_monsterID!.OriginalFunction( breedIdMain, 28 );
        }

        else if ( breedIdMain == 29 && breedIdSub == 1 ) { //Undine - Dragon
            return _hook_monsterID!.OriginalFunction( breedIdMain, 29 );
        }

        return _hook_monsterID!.OriginalFunction( breedIdMain, breedIdSub );

    }

    private void RedirectFromID ( uint breedIdMain, uint breedIdSub ) {
        _logger.WriteLineAsync( $"Running Redirect Script: {breedIdMain}/{breedIdSub}", Color.Lime );
        if ( breedIdMain == 8 && breedIdSub == 5 ) {
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_km.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\kkro\kk_kf.tex" );
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_km_bt.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\kkro\kk_kf_bt.tex" );
        }

        else if ( breedIdMain == 8 && breedIdSub == 10 ) {
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_km.tex" );
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_km_bt.tex" );
        }


        else if ( breedIdMain == 8 && breedIdSub == 29 ) {
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_kb.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\kkro\kk_ms.tex" );
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_kb_bt.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\kkro\kk_ms_bt.tex" );
        }

        else if ( breedIdMain == 8 && breedIdSub == 1 ) {
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_kb.tex" );
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\kkro\kk_kb_bt.tex" );
        }

        else if ( breedIdMain == 17 && breedIdSub == 1 ) {
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\mggjr\mg_mg.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\mggjr\mg_kb.tex" );
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\mggjr\mg_mg_bt.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\mggjr\mg_kb_bt.tex" );
        }

        else if ( breedIdMain == 17 && breedIdSub == 17 ) {
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\mggjr\mg_mg.tex" );
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\mggjr\mg_mg_bt.tex" );
        }

        else if ( breedIdMain == 28 && breedIdSub == 27 ) {
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\mrpru\mr_mr.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\mrpru\mr_mq.tex" );
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\mrpru\mr_mr_bt.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\mrpru\mr_mq_bt.tex" );
        }

        else if ( breedIdMain == 28 && breedIdSub == 28 ) {
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\mrpru\mr_mr.tex" );
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\mrpru\mr_mr_bt.tex" );
        }

        else if ( breedIdMain == 29 && breedIdSub == 1 ) {
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\msund\ms_ms.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\msund\ms_kb.tex" );
            _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\msund\ms_ms_bt.tex",
                _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\msund\ms_kb_bt.tex" );
        }

        else if ( breedIdMain == 29 && breedIdSub == 29 ) {
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\msund\ms_ms.tex" );
            _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\msund\ms_ms_bt.tex" );
        }
    }

    private void SetupHookLoadEMData ( nuint self, uint p2, int p3, int p4 ) {
        //_logger.WriteLineAsync( $"Loading EM Data: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = true;

        _hook_loadEMData!.OriginalFunction( self, p2, p3, p4 );
        //_logger.WriteLineAsync( $"EM Data Call Done: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = false;
    }

    private void SetupBattleStarting ( nuint self ) {
        _monsterInsideBattleStartup = true;
        _monsterInsideBattleRedirects = 0;
        //_logger.WriteLineAsync( $"BATTLE STARTING!!!!!!!!!!!!!!!!!!!!!", Color.Red );

        _hook_battleStarting!.OriginalFunction( self );
        //_logger.WriteLineAsync( $"BATTLE STARTING OVER !!!!!!!!!!!!!!", Color.Red );
        _monsterInsideBattleStartup = false;
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

    public override void ConfigurationUpdated(Config configuration)
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