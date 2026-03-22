using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.MoreMonsters.Configuration;
using MRDX.Game.MoreMonsters.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
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

// MonsterIDFromBreeds - Takes in a Main/Sub and returns the Monster ID
[ HookDef( BaseGame.Mr2, Region.Us, "51 56 8B F1 8B 0D ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_GetMonsterBreedName ( nuint mainID, nuint subID );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 24 A1 ?? ?? ?? ?? 33 C5 89 45 ?? 56" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_FileSaveLoad ( nuint self, nuint unk1, nuint unk2 );


[HookDef( BaseGame.Mr2, Region.Us, "51 53 56 57 FF 72 ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_FileSave ( nuint self, nuint unk1);

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 53 8B 5D ?? 8A C3" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate nuint H_ShrineMonsterUnlockedChecker ( nuint self, int unk1, int unk2 );


// This function appears to be called only when the ranch is being loaded. We care because this is when we're going to read
// the errantry file to get the correct step data.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 10 53 8B DA" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_LoadingRanch ( int unk1, int unk2, nuint unk3, nuint unk4 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 83 EC 24 A1 ?? ?? ?? ?? 33 C4 89 44 24 ?? 53" ) ]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_WormCocoonStart ( nuint unk1, nuint unk2, nuint unk3ptr );


public class Mod : ModBase // <= Do not Remove.
{
    private readonly IHooks _iHooks;
    private readonly string? _modPath;
    private readonly IGame _iGame;

    private readonly IRedirectorController _redirector;

    private string? _dataPath;

    public static nuint address_game;
    public static nuint address_monster { get { return address_game + 0x37667C; } }
    public static nuint address_freezer { get { return address_game + 0x3768BC; } }
    public static nuint address_monster_vertex_scaling { get { return address_game + 0x581520; } }

    // Offsets are exact for monster values. For Freezer Data, add +2.
    public static short memory_mm_version = 1; // Versioning starts at 1, with 0.5.0
    public static nuint offset_mm_version { get { return 0x159; } }
    public static nuint offset_mm_wormsub { get { return 0x162; } }
    public static nuint offset_mm_truemain { get { return 0x163; } }
    public static nuint offset_mm_alternate { get { return 0x164; } }
    public static nuint offset_mm_scaling { get { return 0x165; } }
    public static nuint offset_mm_truesub { get { return 0x166; } }
    public static nuint offset_mm_trueguts { get { return 0x167; } }

    public static nuint address_monster_mm_version { get { return address_monster + offset_mm_version; } }
    public static nuint address_monster_mm_wormsub { get { return address_monster + offset_mm_wormsub; } }
    public static nuint address_monster_mm_truemain {  get { return address_monster + offset_mm_truemain; } }
    public static nuint address_monster_mm_alternate { get { return address_monster + offset_mm_alternate; } }
    public static nuint address_monster_mm_scaling { get { return address_monster + offset_mm_scaling; } }
    public static nuint address_monster_mm_truesub { get { return address_monster + offset_mm_truesub; } }
    public static nuint address_monster_mm_trueguts { get { return address_monster + offset_mm_trueguts; } }

    internal CombinationHandler HandlerCombination { get => handlerCombination; set => handlerCombination =  value ; }
    internal FreezerHandler HandlerFreezer { get => handlerFreezer; set => handlerFreezer =  value ; }
    internal ScalingHandler HandlerScaling { get => handlerScaling; set => handlerScaling =  value ; }

    public static int unusedMonsterOffset = 0x1C; // 28 Bytes are safe at a minimum prior to the monster's name.
    

    private IHook<H_MonsterID> _hook_monsterID;
    private IHook<H_LoadEnemyMonsterData> _hook_loadEMData;
    private IHook<H_BattleStarting> _hook_battleStarting;
    private bool _monsterInsideBattleStartup = false;
    private bool _monsterInsideEnemySetup = false;
    private uint _monsterInsideBattleRedirects = 0;
    private uint _monsterInsideBattleMain = 0;
    private uint _monsterInsideBattleSub = 0;
    public uint _monsterInsideAlternate = 0;
    public bool monsterReplaceEnabled = false;
    public bool retriggerReplacement = false;



    private IHook<H_EarlyShrine> _hook_earlyShrine;
    private IHook<H_WriteSDATAMemory> _hook_writeSDATAMemory;
    private IHook<H_MysteryStatUpdate> _hook_statUpdate;
    private IHook<UpdateGenericState> _hook_updateGenericState;
    private IHook<H_FileSaveLoad> _hook_fileSaveLoad;
    private IHook<H_FileSave> _hook_fileSave;
    private IHook<H_LoadingRanch> _hook_loadingRanch;

    private IHook<H_WormCocoonStart> _hook_wormCocoonStart;

    private IHook<H_ShrineMonsterUnlockedChecker> _hook_shrineMonsterUnlockedChecker;

    private int _loadedFileCorrectFreezer = 0;

    public bool shrineReplacementActive = false;
    private MMBreed _shrineReplacementMonster;
    private byte _shrineMonsterAlternate;
    private readonly IMonster _monsterCurrent;

    private Dictionary<int, (MMBreed, byte)> _songIDMapping = new Dictionary<int, (MMBreed, byte)>();

    private IHook<H_GetMonsterBreedName> _hook_monsterBreedNames;

    public CombinationHandler handlerCombination;
    public FreezerHandler handlerFreezer;
    public ScalingHandler handlerScaling;
    public VSHandler handlerVS;

    private int _randomCD = 1272522;

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
        _modLoader.GetController<IGame>().TryGetTarget(out _iGame);
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

        if (_iGame == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get iGame controller.", Color.Red);
            return;
        }

        var maybeSaveFile = _modLoader.GetController<ISaveFile>();
        if ( maybeSaveFile != null && maybeSaveFile.TryGetTarget( out var saveFile ) ) {
            saveFile.OnSave += PostSaveFreezerDataCorrections;
            saveFile.OnLoad += LoadUpdateFreezerDataCorrections;
        }

        var startupScanner = _modLoader.GetController<IStartupScanner>();
        if ( startupScanner != null && startupScanner.TryGetTarget( out var scanner ) ) {
            AlterCode_RanchWanderingSoundPointer( scanner );
        } else { Logger.Error("Startup Scanner failed to initialize. Monster souns may be impacted.", Color.Red ); }

            _iGame.OnMonsterBreedsLoaded.Subscribe( InitializeNewMonsters );

        _monsterCurrent = _iGame.Monster;

        _iHooks.AddHook<H_MonsterID>( SetupHookMonsterID ).ContinueWith( result => _hook_monsterID = result.Result );
        _iHooks.AddHook<H_LoadEnemyMonsterData>( SetupHookLoadEMData ).ContinueWith( result => _hook_loadEMData = result.Result );
        _iHooks.AddHook<H_BattleStarting>( SetupBattleStarting ).ContinueWith( result => _hook_battleStarting = result.Result );

        _iHooks.AddHook<H_EarlyShrine>( SetupEarlyShrine ).ContinueWith( result => _hook_earlyShrine = result.Result );
        _iHooks.AddHook<H_WriteSDATAMemory>(SetupOverwriteSDATA).ContinueWith( result => _hook_writeSDATAMemory = result.Result );
        _iHooks.AddHook<H_MysteryStatUpdate>( SetupMysteryStat ).ContinueWith( result => _hook_statUpdate = result.Result );

        _iHooks.AddHook<H_GetMonsterBreedName>( SetupGetMonsterBreedName ).ContinueWith( result => _hook_monsterBreedNames = result.Result );

        _iHooks.AddHook<UpdateGenericState>( CheckUpdateLoadedFreezer ).ContinueWith( result => _hook_updateGenericState = result.Result );
        _iHooks.AddHook<H_FileSaveLoad>( FileSaveLoad ).ContinueWith( result => _hook_fileSaveLoad = result.Result );
        _iHooks.AddHook<H_FileSave>( FileSave ).ContinueWith( result => _hook_fileSave = result.Result );

        _iHooks.AddHook<H_LoadingRanch>( HFLoadingRanch ).ContinueWith( result => _hook_loadingRanch = result.Result );

        //_iHooks.AddHook<H_WormCocoonStart>( HF_WormCocoonStart ).ContinueWith( result => _hook_wormCocoonStart = result.Result );

        _iHooks.AddHook<H_ShrineMonsterUnlockedChecker>( CheckShrineMonsterUnlocked ).ContinueWith( result => _hook_shrineMonsterUnlockedChecker = result.Result );

        handlerFreezer = new FreezerHandler( this, _iHooks, _monsterCurrent );
        handlerCombination = new CombinationHandler( this, _iHooks, _monsterCurrent );
        handlerScaling = new ScalingHandler( this, _iHooks, _monsterCurrent );
        handlerVS = new VSHandler( this, _iHooks );

        //_iHooks.AddHook<ParseTextWithCommandCodes>( SetupParseTextCommmandCodes ).ContinueWith(result => _hook_parseTextWithCommandCodes = result.Result.Activate());

        WeakReference<IRedirectorController> _redirectorx = _modLoader.GetController<IRedirectorController>();
        _redirectorx.TryGetTarget( out var redirect );
        if ( redirect == null ) { _logger.WriteLine( $"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red ); return; }
        else { redirect.Loading += ProcessReloadedFileLoad; }

        var exeBaseAddress = module.BaseAddress.ToInt64();
        address_game = (nuint) exeBaseAddress;



        Logger.SetLogLevel( _configuration.LogLevel );
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
   
    private void FileSaveLoad(nuint self, nuint unk1, nuint unk2 ) {
        _hook_fileSaveLoad!.OriginalFunction( self, unk1, unk2 );
    }


    /// <summary>
    /// Reads the variant location data for regular players. If 255 is read, convert it to 0. 
    /// Note: I am honestly not sure why some monsters have 255 in this data slot. I'm suspicious there's some edge case
    /// where I'll have to redo a lot of work. Hopefully I'm just being paranoid.
    /// </summary>
    /// <returns></returns>
    private byte GetPlayerMonsterVariantData() {
        Memory.Instance.Read( address_monster_mm_alternate, out byte variantID );
        return (variantID != (byte) 255) ? variantID : (byte) 0;
    }


    /// <summary>
    /// Called when monster names are being looked for. The return value is the ID of the monster, which then is used to pull the name
    /// data from the table. Writing over the -1 index in the combination table is safe, but those values hold pointer addresses
    /// for the main table so we have to instead overwrite Pixie's name data and return an ID of 0 if it's for a new monster.
    /// </summary>
    /// <param name="mainBreedID"></param>
    /// <param name="subBreedID"></param>
    /// <returns>monsterBreedID (Card ID)</returns>
    private int SetupGetMonsterBreedName(nuint mainBreedID, nuint subBreedID ) {
        Logger.Trace( $"NamesStart: {mainBreedID} | {subBreedID}", Color.OrangeRed );
        var newBreed = MMBreed.GetBreed( (MonsterGenus) mainBreedID, (MonsterGenus) subBreedID );

        // Write New Monster Data
        if ( newBreed != null ) {
            var bn = newBreed._monsterVariants[ 0 ].NameRaw;

            Memory.Instance.Write( nuint.Add( address_game, 0x3492A5 + 27 ), bn ); // Monster Species Pages
            Memory.Instance.Write( nuint.Add( address_game, 0x354E45 + 27), bn ); // Combination References
            Memory.Instance.Write( nuint.Add( address_game, 0x357ED0 ), bn ); // HoF References
            Logger.Trace( $"Wrote : {newBreed._monsterVariants[ 0 ].Name} to {nuint.Add( address_game, 0x3492A6 )}", Color.OrangeRed );
        }

        // Rewrite Pixie's Data
        else if ( mainBreedID == 0 && subBreedID == 0 ) {
            byte[] pixieData = { 0xb5, 0x0f, 0xb5, 0x22, 0xb5, 0x31, 0xb5, 0x22, 0xb5, 0x1e, 0xff };
            Memory.Instance.Write( nuint.Add( address_game, 0x3492A5 + 27 ), pixieData ); // Monster Species Pages
            Memory.Instance.Write( nuint.Add( address_game, 0x354E45 + 27 ), pixieData ); // Combination References
            Memory.Instance.Write( nuint.Add( address_game, 0x357ED0 ), pixieData ); // HoF References
        }

        int ret = _hook_monsterBreedNames!.OriginalFunction( mainBreedID, subBreedID );
        ret = ( ret == -1 ? 0 : ret );
        Logger.Debug( $"Name Overwritten: {mainBreedID} | {subBreedID} | {ret}", Color.OrangeRed );

        return ret;  
    }

    

    private void RedirectorSetupDataPath ( string? extractedPath ) {
        _dataPath = extractedPath;
        _redirector.AddRedirect( _dataPath + @"\data.bin", _modPath + @"invalidfile.bin" );
        Logger.Info( "Redirecting " + _dataPath + @"\data.bin to " + _modPath + @"invalidfile.bin", Color.Beige );
    }

    /// <summary>
    /// Reads in monster data from the applicable files.
    /// </summary>
    private void InitializeNewMonsters ( bool unused ) {
        InitializeNewMonsters( Path.Combine( _modPath + @"\NewMonsterData\", "MDATA_MONSTER.csv" ) );

        if ( _configuration.BonusMonsterSpecies ) {
            InitializeNewMonsters( Path.Combine( _modPath + @"\NewMonsterData\", "MDATA_MONSTER_BONUS.csv" ) );
        }
    }

    private void InitializeNewMonsters(string filename ) {

        // Reads in a slightly modified version of SDATA
        
        var mdata = File.ReadAllLines( filename );
        foreach ( var r in mdata ) {
            var row = r.Split( "\t" );

            int songID = int.Parse(row[ 0 ]);
            byte alternate = byte.Parse( row[ 1 ] );

            string name = row[ 2 ];
            MonsterGenus newMain = (MonsterGenus) int.Parse( row[ 3 ] );
            MonsterGenus newSub = (MonsterGenus) int.Parse( row[ 4 ] );

            MonsterGenus baseMain = (MonsterGenus) int.Parse( row[ 5 ] );
            MonsterGenus baseSub = (MonsterGenus) int.Parse( row[ 6 ] );

            ushort lifespan = ushort.Parse( row[ 7 ] );
            short nature = short.Parse( row[ 8 ] );
            LifeType growthPattern = (LifeType) int.Parse( row[ 9 ] );

            ushort slif = ushort.Parse( row[ 10 ] );
            ushort spow = ushort.Parse( row[ 11 ] );
            ushort sint = ushort.Parse( row[ 12 ] );
            ushort sski = ushort.Parse( row[ 13 ] );
            ushort sspd = ushort.Parse( row[ 14 ] );
            ushort sdef = ushort.Parse( row[ 15 ] );

            byte glif = byte.Parse( row[ 16 ] );
            byte gpow = byte.Parse( row[ 17 ] );
            byte gint = byte.Parse( row[ 18 ] );
            byte gski = byte.Parse( row[ 19 ] );
            byte gspd = byte.Parse( row[ 20 ] );
            byte gdef = byte.Parse( row[ 21 ] );

            byte arenaspeed = byte.Parse( row[ 22 ] );
            byte gutsrate = byte.Parse( row[ 23 ] );
            int battlespecials = int.Parse( row[ 24 ] );
            string techniques = row[ 25 ];

            ushort trainbonuses = ushort.Parse( row[ 26 ] );

            MMBreed? breed = MMBreed.GetBreed( newMain, newSub );
            if ( breed == null ) {
                breed = new MMBreed( newMain, newSub, baseMain, baseSub, alternate );
                breed.NewBaseBreed( name, lifespan, nature, growthPattern,
                    slif, spow, sint, sski, sspd, sdef,
                    glif, gpow, gint, gski, gspd, gdef,
                    arenaspeed, gutsrate, battlespecials, techniques, trainbonuses );
                _songIDMapping.Add( songID, (breed, alternate) );
                Logger.Info( $"New Monster Combination Found: {newMain}, {newSub} for songID {songID}." );
            }

            else {
                breed._alternateCount = Math.Max( breed._alternateCount, alternate );
                _songIDMapping.Add( songID, (breed, alternate) );
            }
        }
    }

    private int SetupHookMonsterID ( uint breedIdMain, uint breedIdSub ) {

        int variantID = 0;

        Logger.Info( $"Getting Monster ID: {breedIdMain} : {breedIdSub}", Color.Aqua );

        if ( shrineReplacementActive ) {
            breedIdMain = (uint) _shrineReplacementMonster._genusNewMain;
            breedIdSub = (uint) _shrineReplacementMonster._genusNewSub;
            variantID = _shrineMonsterAlternate;
        }

        else if ( handlerVS.GetVsModeOverrideAlternate() ) {
            variantID = (int) _monsterInsideAlternate;
        }

        else {
            variantID = GetPlayerMonsterVariantData();
        }

        MonsterGenus breedMain = (MonsterGenus) breedIdMain;
        MonsterGenus breedSub = (MonsterGenus) breedIdSub;


        if ( _monsterInsideBattleStartup ) { 
            _monsterInsideBattleRedirects++; 
            if ( _monsterInsideBattleRedirects == 2 ) {
                _monsterInsideBattleMain = breedIdMain;
                _monsterInsideBattleSub = breedIdSub;
                //_monsterInsideAlternate = GetOpponentMonsterVariantData(); TODO : This should be from DT. VS Handler sets it up internally.
                //Logger.Info( "InsideBattleStartup, Redirects == 2", Color.Green );
            }
        }

        if ( !_monsterInsideBattleStartup || _monsterInsideBattleRedirects == 1 ) {
            //Logger.Info( $"Not Inside or Redirects = 1 {_monsterInsideBattleStartup} | {_monsterInsideBattleRedirects}", Color.Green );
            RedirectFromID( breedIdMain, breedIdSub, variantID );
        }

        foreach ( MMBreed breed in MMBreed.NewBreeds ) {
            if ( breed.MatchNewBreed(breedMain, breedSub) ) {
                return _hook_monsterID!.OriginalFunction( (uint) breed._genusBaseMain, (uint) breed._genusBaseSub );
            }
        }

        return _hook_monsterID!.OriginalFunction( breedIdMain, breedIdSub );

    }

    /// <summary>
    /// Sets up the redirects required for a monster on the fly, disabling or enabling as needed.
    /// </summary>
    /// <param name="breedIdMain"></param>
    /// <param name="breedIdSub"></param>
    /// <param name="variant"></param>
    private void RedirectFromID ( uint breedIdMain, uint breedIdSub, int variant = 0 ) {
        MonsterGenus breedMain = (MonsterGenus) breedIdMain;
        MonsterGenus breedSub = (MonsterGenus) breedIdSub;

        Logger.Info( $"Running Redirect Script: {breedIdMain}/{breedIdSub}/A{variant}", Color.Lime );

        foreach ( MMBreed breed in MMBreed.NewBreeds ) {
            if ( breed.MatchNewBreed(breedMain, breedSub ) ) {
                Logger.Info( $"Redirect Script Found MM for {breedIdMain}/{breedIdSub}", Color.Lime );
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + ".tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + ".tex" );
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + "_bt.tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + "_bt.tex" );

                Logger.Trace( $"Redirect Script MM for {_dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + ".tex"} " +
                    $"to {_modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + ".tex"}" );
                Logger.Trace( $"Redirect Script MM for {_dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + "_bt.tex"} " +
                    $"to {_modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + "_bt.tex"}" );
                return;
            }

            if ( breed.MatchBaseBreed(breedMain, breedSub) ) {
                Logger.Info( $"Redirect Script reverting to standard monster for {breedIdMain}/{breedIdSub}.", Color.Lime );
                _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + ".tex");
                _redirector.RemoveRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + "_bt.tex" );
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
        Logger.Debug( $"Loading EM Data: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = true;

        _hook_loadEMData!.OriginalFunction( self, p2, p3, p4 );
        Logger.Debug( $"EM Data Call Done: {self} : {p2} : {p3} : {p4}", Color.GreenYellow );
        _monsterInsideEnemySetup = false;
    }

    /// <summary>
    /// This function is called at the beginning of battle preparation. We can use it to know exactly when multiple monster models will be loaded and to start scanning filereads appropriately.
    /// </summary>
    /// <param name="self"></param>
    private void SetupBattleStarting ( nuint self ) {
        _monsterInsideBattleStartup = true;
        _monsterInsideBattleRedirects = 0;
        Logger.Debug( $"BATTLE STARTING!!!!!!!!!!!!!!!!!!!!!", Color.Red );

        _hook_battleStarting!.OriginalFunction( self );
        Logger.Debug( $"BATTLE STARTING OVER !!!!!!!!!!!!!!", Color.Red );
        _monsterInsideBattleStartup = false;
    }


    /// <summary>
    /// This function is called when the player has confirmed which Song they are going
    /// to generate from the shrine. At this point, we can see if the monster needs
    /// to be replaced and enable the mapping.
    /// We also determine which color variant to use.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="p2"></param>
    private void SetupEarlyShrine ( nuint self, nuint p2 ) {
        
        Logger.Debug( $"ESHRINE: {self} {p2}", Color.Yellow );
        _hook_earlyShrine!.OriginalFunction( self, p2 );
        Memory.Instance.Read( nuint.Add( self, 0xcc ), out int songID );
        Logger.Debug( $"ESHRINE Post Setup: {self} {p2} {songID}", Color.Yellow );

        while ( songID == _randomCD ) {
            songID = _songIDMapping.ElementAt( Random.Shared.Next( _songIDMapping.Count ) ).Key;
        }

        if ( _songIDMapping.ContainsKey(songID) ) {
            var songMap = _songIDMapping[songID];
            shrineReplacementActive = true;
            _shrineReplacementMonster = songMap.Item1;
            _shrineMonsterAlternate = songMap.Item2;
        }

        if ( _configuration.MonsterSizesGenetics == Config.ScalingGenetics.WildWest ) {
            handlerScaling.temporaryScaling = (byte) ( ( Random.Shared.Next( 1, 201 ) + Random.Shared.Next( 1, 201 ) ) / 2 );
        } else {
            handlerScaling.temporaryScaling = (byte) ( ( Random.Shared.Next( 1, 201 ) + Random.Shared.Next( 1, 201 ) + 
                Random.Shared.Next( 1, 201 ) ) / 3 );
        }
    }

    /// <summary>
    /// This function is called right before performing the shrine animation. Once
    /// complete, the monster is created. Here we overwrite where the monster breed
    /// sub is written to so that the correct model/texture is used.
    /// </summary>
    /// <param name="self"></param>
    private void SetupOverwriteSDATA ( nuint self ) {

        _hook_writeSDATAMemory!.OriginalFunction( self );
        if ( shrineReplacementActive ) {
            Memory.Instance.Read( nuint.Add(self, 0x44), out nuint breedLoc );
            Memory.Instance.Write( breedLoc + 0x48, (byte) _shrineReplacementMonster._genusNewMain );
            Memory.Instance.Write( breedLoc + 0x49, (byte) _shrineReplacementMonster._genusNewSub );

        }

        if ( _configuration.MonsterSizesEnabled ) {
            Memory.Instance.Write( address_monster_mm_scaling, handlerScaling.temporaryScaling );
            handlerScaling.temporaryScaling = 0;
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

    /// <summary>
    /// This function is called after all shrine stat setup code is called.
    /// We then replace all of the stats with the chosen variant.
    /// Also sets up additional information such as color variants and scaling.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    private int SetupMysteryStat ( nuint self ) {

        Logger.Info( "Updating Monster Stats to new base variant stats." );
        var ret = _hook_statUpdate!.OriginalFunction( self );

        if ( shrineReplacementActive ) {
            var variant = MonsterBreed.GetBreed( _shrineReplacementMonster._genusNewMain, _shrineReplacementMonster._genusNewSub );
            WriteMonsterData( variant );

            Memory.Instance.Write( address_monster_mm_alternate, _shrineMonsterAlternate );

            shrineReplacementActive = false;
        }

        if ( _monsterCurrent.GenusMain == MonsterGenus.Worm ) { // Handle Worm Info
            Memory.Instance.WriteRaw( Mod.address_monster_mm_wormsub, [ (byte) ( _monsterCurrent.GenusSub + 1 ) ] );
        }

        return ret;
    }

    public void WriteMonsterData(MonsterBreed breed) {
        _monsterCurrent.GenusMain = breed.GenusMain;
        _monsterCurrent.GenusSub = breed.GenusSub;

        _monsterCurrent.Lifespan = breed.Lifespan;
        _monsterCurrent.InitalLifespan = breed.Lifespan;

        _monsterCurrent.NatureRaw = (short) breed.NatureBase;
        _monsterCurrent.NatureBase = breed.NatureBase;

        _monsterCurrent.LifeType = breed.LifeType;

        _monsterCurrent.Life = breed.Life;
        _monsterCurrent.Power = breed.Power;
        _monsterCurrent.Intelligence = breed.Intelligence;
        _monsterCurrent.Skill = breed.Skill;
        _monsterCurrent.Speed = breed.Speed;
        _monsterCurrent.Defense = breed.Defense;

        _monsterCurrent.GrowthRateLife = breed.GrowthRateLife;
        _monsterCurrent.GrowthRatePower = breed.GrowthRatePower;
        _monsterCurrent.GrowthRateIntelligence = breed.GrowthRateIntelligence;
        _monsterCurrent.GrowthRateSkill = breed.GrowthRateSkill;
        _monsterCurrent.GrowthRateSpeed = breed.GrowthRateSpeed;
        _monsterCurrent.GrowthRateDefense = breed.GrowthRateDefense;

        _monsterCurrent.ArenaSpeed = breed.ArenaSpeed;
        _monsterCurrent.GutsRate = breed.GutsRate;

        // TODO - Incorproate into base mod.
        // Battle Specials - Not incorporated into the base mod yet.
        Memory.Instance.WriteRaw( nuint.Add( address_monster, 0x1D0 ), BitConverter.GetBytes(breed.BattleSpecialsRaw) );

        _monsterCurrent.TrainBoost = breed.TrainBoost;

        // Fix Techniques - This is not incorproated into the base mod yet.
        Memory.Instance.WriteRaw( nuint.Add( address_monster, 0x192 ), IMonsterTechnique.SerializeTechsLearnedMemory( breed.TechsKnown ) );

        // Fix Technique Slot Chosen - The game does not like custom monsters and will assign slots for invalid techniques, bricking an entire range.
        byte[] slotChosen = [ 0xFF, 0xFF, 0xFF, 0xFF ];
        foreach ( IMonsterTechnique technique in breed.TechsKnown ) {
            slotChosen[(int) technique.Range] = technique.SlotPosition < slotChosen[ (int) technique.Range ] ? technique.SlotPosition : slotChosen[ (int) technique.Range ];
        }
        for ( int i = 0; i < 4; i++ ) { if ( slotChosen[i] == 0xFF ) { slotChosen[ i ] = 24; } }
        Memory.Instance.WriteRaw( nuint.Add( address_monster, 0x1C8 ), slotChosen );

        // Fix Like Slot - The monster likes are occasionally set to illegal items (or empty). Just reset it every time.
        Item[] itemList = [ Item.Potato, Item.Milk, Item.Fish, Item.Meat, Item.Tablet, Item.CupJelly, Item.Play, Item.Battle, Item.Rest ];
        Utils.Shuffle( Random.Shared, itemList );
        _monsterCurrent.ItemLike = _monsterCurrent.ItemDislike != itemList[ 0 ] ? itemList[ 0 ] : itemList[ 1 ];
        
    }


    private void ProcessReloadedFileLoad ( string filename ) {

        FileLoadCheckBattleRedirects(filename);
        FileLoadCorrectFreezerEntries( filename );

    }

    private void FileLoadCheckBattleRedirects(string filename) {
        if ( _monsterInsideBattleStartup ) {
            Logger.Trace( $"Inside File Checking for Monsters:  {_monsterInsideBattleStartup}, {_monsterInsideBattleRedirects}, {_monsterInsideBattleMain}, {_monsterInsideBattleSub}", Color.Orange );
            if ( _monsterInsideBattleRedirects == 1 ) {
                RedirectFromID( _monsterInsideBattleMain, _monsterInsideBattleSub, (int) _monsterInsideAlternate );
                // Load the tournament monsters? I think?
            }

            if ( filename.Contains( "_bt.tex" ) && filename.Contains( "mf2\\data\\mon" ) ) {
                _monsterInsideBattleRedirects--;
                Logger.Trace( $"Inside File Checking Decrementing redirects {_monsterInsideBattleRedirects}", Color.Orange );
            }
        }
    }


    /// <summary>
    /// This function is called whenever Reloaded detects a file load. We are looking for 
    /// park.tex. This is our indication that the game has 'properly' been loaded so we do not
    /// upload garbage data to VS mode. This reads the monster's mm guts rate and subs and replaces the
    /// freezer data if set to the appropriate values. Values of 0 are ignored.
    /// Subspecies are written as +1 to the actual species (Pixie = 1, Henger = 6, etc).
    /// </summary>
    /// <param name="filename"></param>
    private void FileLoadCorrectFreezerEntries(string filename) {

        if ( filename.Contains("park.tex") && _loadedFileCorrectFreezer == 1 ) {
            _loadedFileCorrectFreezer = 2;
        }
    }


    private void CheckUpdateLoadedFreezer ( nint parent ) {
        _hook_updateGenericState!.OriginalFunction( parent );

        Logger.Trace( "Generic State Update", Color.Beige );
        if ( _loadedFileCorrectFreezer == 2 ) {
            PostSaveLoadFreezerDataCorrections();
            Logger.Info("Updated Freezer with MM Data Post-Load", Color.Aqua );
            _loadedFileCorrectFreezer = 0;
        }
        
    }

    /// <summary>
    /// This function is called immediately prior to saving the game.
    /// It alters the state of all monsters in the freezer to have main breed subs and guts rates.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="unk1"></param>
    private void FileSave ( nuint self, nuint unk1 ) {

        // TODO : I need to fix the 0x8 and 0x4 additions at the end of the addresses.

        for ( var i = 0; i < 20; i++ ) {
            var startPos = address_freezer + (nuint) ( 524 * i );
            var mainPosActual = startPos + 0x4 + 0x4;
            var subPosActual = startPos + 0x8 + 0x4;

            Memory.Instance.Read( mainPosActual, out byte main );
            Memory.Instance.Read( subPosActual, out byte sub );

            if ( main == 0x2e ) { continue; } // Skip Empty Slots

            var verPosMM = startPos + offset_mm_version + 0x8;

            Memory.Instance.Read( verPosMM, out ushort versionMM );
            
            // VERSION 0.5.0 SAVE FILE UPDATE
            // Updates all monsters in the freezer to Version 0.5.0 standards that are not already following it.
            if ( versionMM < 1 ) {
                VersionUpdateFreezerMonster_V0x5x0_MM1( (byte) i );
            }
        }

        Logger.Info( "File Saved with More Monster Data", Color.Aqua );
        _hook_fileSave!.OriginalFunction( self, unk1 );
    }

    private void VersionUpdateFreezerMonster_V0x5x0_MM1 ( byte freezerSlot ) {
        
        var startPos = address_freezer + (nuint) ( 524 * freezerSlot );
        var mainPosActual = startPos + 0x4 + 0x4;
        var subPosActual = startPos + 0x8 + 0x4;
        var gutsPosActual = startPos + 0x1D3 + 0x4;

        Memory.Instance.Read( mainPosActual, out byte mainA );
        Memory.Instance.Read( subPosActual, out byte subA );
        Memory.Instance.Read( gutsPosActual, out byte gutsA );

        var verPosMM = startPos + offset_mm_version + 0x8;
        var mainPosMM = startPos + offset_mm_truemain + 0x8;
        var subPosMM = startPos + offset_mm_truesub + 0x8;
        var gutsPosMM = startPos + offset_mm_trueguts + 0x8;
        var wormPosMM = startPos + offset_mm_wormsub + 0x8;

        byte mmMain = (byte) ( mainA + 1 );
        byte mmSub = (byte) ( subA + 1 );

        // Update an MM Monster to the proper Version, Main, Sub, and GR
        if ( MMBreed.GetBreed( (MonsterGenus) mainA, (MonsterGenus) subA ) != null ) {
            Memory.Instance.Write<short>( verPosMM, ref memory_mm_version );
            Memory.Instance.Write<Byte>( mainPosMM, ref mmMain );
        }

        // Update standard breed monstesr to the proper version, Main, Sub, and GR
        else {
            Memory.Instance.Write<short>( verPosMM, ref memory_mm_version );
            Memory.Instance.Write<Byte>( mainPosMM, ref mmMain );
            Memory.Instance.Write<Byte>( subPosMM, ref mmSub );
            Memory.Instance.Write<Byte>( gutsPosMM, ref gutsA );
        }

        // Handle Worm
        if ( (MonsterGenus) mainA == MonsterGenus.Worm ) {
            Memory.Instance.Write<byte>( wormPosMM, ref mmSub );
        }

        Logger.Info( $"Monster in Freezer Slot {freezerSlot} updated to Version 0.5.0 Standards (v1)." );
    }

    /// <summary>
    /// This function is called after confirming a song ID from the shrine. 
    /// It checks whether the monster unlock flags are set for a specific monster, and returns 1 if unlocked, 0 otherwise.
    /// We need to hook this function to disable shrine replacement (monster replacement basically) if the shrine fails to
    /// generate a monster. This was breaking a ton of stuff down the line otherwise.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="unk1"></param>
    /// <param name="unk2"></param>
    /// <returns></returns>
    private nuint CheckShrineMonsterUnlocked ( nuint self, int unk1, int unk2 ) {
        Logger.Trace( $"Checking if the monster is unlocked at the shrine. {unk1} {unk2}", Color.Yellow );
        nuint ret = _hook_shrineMonsterUnlockedChecker!.OriginalFunction( self, unk1, unk2 );

        if ( ret == 0 ) { shrineReplacementActive = false; }
        return ret;
    }

    private void PostSaveFreezerDataCorrections ( ISaveFileEntry savefile ) {
        PostSaveLoadFreezerDataCorrections();
    }

    /// <summary>
    /// This function is called after a save is performed. 
    /// This call used to undo the MM value swaps to resume normal gameplay, but as its handled prior to sending data off to VS mode
    /// this is not necessary any longer unless the monster is on an old version of MM.
    /// </summary>
    private void PostSaveLoadFreezerDataCorrections() {
        // TODO : I need to fix the 0x8 and 0x4 additions at the end of the addresses.
        
        for ( var i = 0; i < 20; i++ ) {
            var startPos = address_freezer + (nuint) ( 524 * i );
            var verPosMM = startPos + offset_mm_version + 0x8;

            Memory.Instance.Read( verPosMM, out short mmVersion );

            // Monster MM Data before Version 0.5.0, fix Sub and Guts
            if ( mmVersion < 1 ) {
                var subPosMM = address_freezer + (nuint) ( 524 * i ) + offset_mm_truesub + 0x8;
                var gutsPosMM = address_freezer + (nuint) ( 524 * i ) + offset_mm_trueguts + 0x8;

                var subPosActual = address_freezer + (nuint) ( 524 * i ) + 0x8 + 0x4;
                var gutsPosActual = address_freezer + (nuint) ( 524 * i ) + 0x1D3 + 0x4;

                Memory.Instance.Read( subPosMM, out byte sub );
                Memory.Instance.Read( gutsPosMM, out byte guts );

                // Subs are saved as Sub+1 so we can tell empty (0) from Pixie.
                if ( sub > 0 ) {
                    sub -= 1;
                    Memory.Instance.Write<Byte>( subPosActual, ref sub );
                }

                if ( guts > 0 ) {
                    Memory.Instance.Write<Byte>( gutsPosActual, ref guts );
                }
            }
        }
        
        Logger.Debug( "Freezer Data Corrected Post Save/Load - No Action Taken", Color.Aqua );
    }

    /// <summary>
    /// This function is called after a load is performed.
    /// If we are currently are in VS Mode, then update the freezer data in memory.
    /// </summary>
    /// <param name="savefile"></param>
    private void LoadUpdateFreezerDataCorrections ( ISaveFileEntry savefile ) {
        _loadedFileCorrectFreezer = 1;
        if ( handlerVS._vsModeEntered ) {
            handlerVS.UpdateLoadedFileFreezerInformation();
        }
    }



    private void HFLoadingRanch ( int unk1, int unk2, nuint unk3, nuint unk4 ) {
        _hook_loadingRanch!.OriginalFunction( unk1, unk2, unk3, unk4 );

        Memory.Instance.SafeRead( address_monster, out byte monsterMain );
        var breedShort = IMonster.AllMonsters[ monsterMain ].ShortName;
        var errantryFile = _dataPath + @$"\mf2\data\mon\{breedShort}\{breedShort[ ..2 ]}_{breedShort[ ..2 ]}_i.isd";

        var data = File.ReadAllBytes( errantryFile );
        long soundPos = data[ 0x18 ] + ( data[0x19] << 8 ) + ( data[0x1A] << 16) + (data[0x1B] << 24);

        var soundLen = data.Length - soundPos;

        byte[] rawSoundData = new byte[ soundLen ];
        for ( var i = soundPos; i < data.Length; i++ ) {
            rawSoundData[ i - soundPos ] = data[ i ];
        }
        Memory.Instance.WriteRaw( address_game + 0x571470, rawSoundData );
    }

    /// <summary>
    /// This function refuses to hook properly so we're doing it another way for now. Leaving it in as this is the PROPER way to do things.
    /// THis should be writing the Worm's Sub ON Cocoon, instead it crashes the game. Cool!
    /// </summary>
    /// <param name="self"></param>
    /// <param name="unk1"></param>
    /// <param name="unk2"></param>
    /// <param name="unk3ptr"></param>
    public void HF_WormCocoonStart ( nuint unk1, nuint unk2, nuint unk3ptr ) {
        
        Logger.Debug( $"Worm Cocoon Starting with {unk1}, {unk2}, {unk3ptr}.", Color.WhiteSmoke );
        byte wormSub = (byte) ( (byte) _monsterCurrent.GenusSub + 1);
        Memory.Instance.Write<byte>( address_monster_mm_wormsub, ref wormSub );

        _hook_wormCocoonStart!.OriginalFunction( unk1, unk2, unk3ptr );
        Logger.Debug( $"Worm Sub : {wormSub} written to MM Data.", Color.WhiteSmoke );


    }
    /// <summary>
    ///     This function replaces the location where the game is looking for sound data when wandering around on the ranch.
    ///     Errantry files can only be of length 0x7EF.
    ///     Here, we overwrite the location of where this data should be located to a manual location in memory
    ///     the mod writes the sound data to.
    ///     MOV EDI [EBX + 0x100] becomes NOOP, MOV EDI [MF2.EXE + 0x571470]
    ///     The choice of MF2.exe + 0x571470 is arbitrary. It appears to be an unused and unallocated block of null values.
    /// </summary>
    /// <param name="scanner"></param>
    private void AlterCode_RanchWanderingSoundPointer ( IStartupScanner scanner ) {
        _logger.WriteLine( "Ranch Wandering Pointer Scanning" );
        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        var exeBaseAddress = module.BaseAddress.ToInt64();
        scanner.AddMainModuleScan( "55 8B EC 83 EC 0C 83 3D ?? ?? ?? ?? 00", result => {
            var addr = (nuint) ( exeBaseAddress + result.Offset );
            Memory.Instance.SafeWrite( addr + 0x59, (byte) 0x90 );
            Memory.Instance.SafeWrite( addr + 0x59 + 0x1, 0xBF );
            Memory.Instance.SafeWrite( addr + 0x59 + 0x2, (int) ( exeBaseAddress + 0x571470 ) );
            _logger.WriteLine( $"{addr + 0x59} found, updating to {exeBaseAddress}/{exeBaseAddress + 0x571470}" );
        } );
    }

    // Card Information For Later
    // b5 13 b5 21 b5 1e b5 2b b5 1e
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
    public Config _configuration;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    #endregion
}