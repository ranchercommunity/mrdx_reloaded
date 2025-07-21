using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using System.Xml.Schema;
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

// MonsterIDFromBreeds - Takes in a Main/Sub and returns the Monster ID
[ HookDef( BaseGame.Mr2, Region.Us, "51 56 8B F1 8B 0D ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_GetMonsterBreedName ( nuint mainID, nuint subID );




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

    public static nuint address_monster_mm_variant { get { return address_monster + 0x164; } }
    public static nuint address_monster_mm_scaling { get { return address_monster + 0x165; } }
    public static nuint address_monster_mm_truesub { get { return address_monster + 0x166; } }
    public static nuint address_monster_mm_trueguts { get { return address_monster + 0x167; } }

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
    public bool monsterReplaceEnabled = false;
    public bool retriggerReplacement = false;

    private IHook<H_EarlyShrine> _hook_earlyShrine;
    private IHook<H_WriteSDATAMemory> _hook_writeSDATAMemory;
    private IHook<H_MysteryStatUpdate> _hook_statUpdate;

    public bool shrineReplacementActive = false;
    private MMBreed _shrineReplacementMonster;
    private byte _shrineColorVariant;
    private readonly IMonster _monsterCurrent;

    private Dictionary<int, MMBreed> _songIDMapping = new Dictionary<int, MMBreed>();


    private IHook<H_GetMonsterBreedName> _hook_monsterBreedNames;

    private CombinationHandler handlerCombination;
    private FreezerHandler handlerFreezer;
    private ScalingHandler handlerScaling;

    /* Scaling Variables */

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

        _iGame.OnMonsterBreedsLoaded.Subscribe( InitializeNewMonsters );

        _monsterCurrent = _iGame.Monster;

        _iHooks.AddHook<H_MonsterID>( SetupHookMonsterID ).ContinueWith( result => _hook_monsterID = result.Result );
        _iHooks.AddHook<H_LoadEnemyMonsterData>( SetupHookLoadEMData ).ContinueWith( result => _hook_loadEMData = result.Result );
        _iHooks.AddHook<H_BattleStarting>( SetupBattleStarting ).ContinueWith( result => _hook_battleStarting = result.Result );

        _iHooks.AddHook<H_EarlyShrine>( SetupEarlyShrine ).ContinueWith( result => _hook_earlyShrine = result.Result );
        _iHooks.AddHook<H_WriteSDATAMemory>(SetupOverwriteSDATA).ContinueWith( result => _hook_writeSDATAMemory = result.Result );
        _iHooks.AddHook<H_MysteryStatUpdate>( SetupMysteryStat ).ContinueWith( result => _hook_statUpdate = result.Result );

        _iHooks.AddHook<H_GetMonsterBreedName>( SetupGetMonsterBreedName ).ContinueWith( result => _hook_monsterBreedNames = result.Result );

        handlerFreezer = new FreezerHandler( this, _iHooks, _monsterCurrent );
        handlerCombination = new CombinationHandler( this, _iHooks, _monsterCurrent );
        handlerScaling = new ScalingHandler( this, _iHooks, _monsterCurrent );

        //_iHooks.AddHook<ParseTextWithCommandCodes>( SetupParseTextCommmandCodes ).ContinueWith(result => _hook_parseTextWithCommandCodes = result.Result.Activate());



        
        WeakReference<IRedirectorController> _redirectorx = _modLoader.GetController<IRedirectorController>();
        _redirectorx.TryGetTarget( out var redirect );
        if ( redirect == null ) { _logger.WriteLine( $"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red ); return; }
        else { redirect.Loading += ProcessReloadedFileLoad; }

        var exeBaseAddress = module.BaseAddress.ToInt64();
        address_game = (nuint) exeBaseAddress;

        Logger.SetLogLevel( Logger.LogLevel.Info );
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
   
    private byte GetPlayerMonsterVariantData() {
        Memory.Instance.Read( address_monster_mm_variant, out byte variantID );
        return variantID;
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
            Logger.Trace( $"Wrote : {newBreed._monsterVariants[ 0 ].Name} to {nuint.Add( address_game, 0x3492A6 )}", Color.OrangeRed );
        }

        // Rewrite Pixie's Data
        else if ( mainBreedID == 0 && subBreedID == 0 ) {
            byte[] pixieData = { 0xb5, 0x0f, 0xb5, 0x22, 0xb5, 0x31, 0xb5, 0x22, 0xb5, 0x1e, 0xff };
            Memory.Instance.Write( nuint.Add( address_game, 0x3492A5 + 27 ), pixieData ); // Monster Species Pages
            Memory.Instance.Write( nuint.Add( address_game, 0x354E45 + 27 ), pixieData ); // Combination References                                              
        }

        int ret = _hook_monsterBreedNames!.OriginalFunction( mainBreedID, subBreedID );
        ret = ( ret == -1 ? 0 : ret );
        Logger.Debug( $"Name Overwritten: {mainBreedID} | {subBreedID} | {ret}", Color.OrangeRed );

        return ret;  
    }

    

    private void RedirectorSetupDataPath ( string? extractedPath ) {
        _dataPath = extractedPath;
    }

    /// <summary>
    ///  This function should eventually read from a file or some equivalent.
    /// </summary>
    private void InitializeNewMonsters ( bool unused ) {

        // Reads in a slightly modified version of SDATA
        var moreMonstersDataList = new Dictionary<int, string[]>();
        var mdata = File.ReadAllLines( Path.Combine( _modPath + @"\NewMonsterData\", "MDATA_MONSTER.csv" ) );
        foreach ( var r in mdata ) {
            var row = r.Split( "," );

            int songID = int.Parse(row[ 0 ]);
            string name = row[ 1 ];
            MonsterGenus newMain = (MonsterGenus) int.Parse( row[ 2 ] );
            MonsterGenus newSub = (MonsterGenus) int.Parse( row[ 3 ] );

            MonsterGenus baseMain = (MonsterGenus) int.Parse( row[ 4 ] );
            MonsterGenus baseSub = (MonsterGenus) int.Parse( row[ 5 ] );

            ushort lifespan = ushort.Parse( row[ 6 ] );
            short nature = short.Parse( row[ 7 ] );
            LifeType growthPattern = (LifeType) int.Parse( row[ 8 ] );

            ushort slif = ushort.Parse( row[ 9 ] );
            ushort spow = ushort.Parse( row[ 10 ] );
            ushort sint = ushort.Parse( row[ 11 ] );
            ushort sski = ushort.Parse( row[ 12 ] );
            ushort sspd = ushort.Parse( row[ 13 ] );
            ushort sdef = ushort.Parse( row[ 14 ] );

            byte glif = byte.Parse( row[ 15 ] );
            byte gpow = byte.Parse( row[ 16 ] );
            byte gint = byte.Parse( row[ 17 ] );
            byte gski = byte.Parse( row[ 18 ] );
            byte gspd = byte.Parse( row[ 19 ] );
            byte gdef = byte.Parse( row[ 20 ] );

            byte arenaspeed = byte.Parse( row[ 21 ] );
            byte gutsrate = byte.Parse( row[ 22 ] );
            int battlespecials = int.Parse( row[ 23 ] );
            string techniques = row[ 24 ];

            ushort trainbonuses = ushort.Parse( row[ 25 ] );

            MMBreed? breed = MMBreed.GetBreed( newMain, newSub );
            if ( MMBreed.GetBreed(newMain, newSub) == null ) {
                breed = new MMBreed( newMain, newSub, baseMain, baseSub );
                breed.NewBaseBreed( name, lifespan, nature, growthPattern,
                    slif, spow, sint, sski, sspd, sdef,
                    glif, gpow, gint, gski, gspd, gdef,
                    arenaspeed, gutsrate, battlespecials, techniques, trainbonuses );
                _songIDMapping.Add( songID, breed );
                Logger.Info( $"New Monster Combination Found: {newMain}, {newSub} for songID {songID}." );
            }
        }
    }

    private int SetupHookMonsterID ( uint breedIdMain, uint breedIdSub ) {

        int variantID = 0;

        Logger.Info( $"Getting Monster ID: {breedIdMain} : {breedIdSub}", Color.Aqua );

        if ( shrineReplacementActive ) {
            breedIdMain = (uint) _shrineReplacementMonster._genusNewMain;
            breedIdSub = (uint) _shrineReplacementMonster._genusNewSub;
            variantID = _shrineColorVariant;
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
            }
        }

        if ( !_monsterInsideBattleStartup || _monsterInsideBattleRedirects == 1 ) {
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

        Logger.Info( $"Running Redirect Script: {breedIdMain}/{breedIdSub}", Color.Lime );

        foreach ( MMBreed breed in MMBreed.NewBreeds ) {
            if ( breed.MatchNewBreed(breedMain, breedSub ) ) {
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + ".tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + ".tex" );
                _redirector.AddRedirect( _dataPath + @"\mf2\data\mon\" + breed.FilepathBase() + "_bt.tex",
                    _modPath + @"\ManualRedirector\Resources\data\mf2\data\mon\" + breed.FilepathNew( variant ) + "_bt.tex" );
                return;
            }

            if ( breed.MatchBaseBreed(breedMain, breedSub) ) {
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

        foreach ( var songMap in _songIDMapping ) {
            if ( songID == songMap.Key ) {
                shrineReplacementActive = true;
                _shrineReplacementMonster = songMap.Value;
                _shrineColorVariant = (byte) Random.Shared.Next( _shrineReplacementMonster._variantCount == 0 ? 0 : _shrineReplacementMonster._variantCount + 1 );
            }
        }

        if ( _configuration.MonsterSizesGenetics == Config.ScalingGenetics.WildWest ) {
            handlerScaling.temporaryScaling = (byte) ( ( Random.Shared.Next( 1, 201 ) + Random.Shared.Next( 1, 201 ) ) / 2 );
        } else {
            handlerScaling.temporaryScaling = (byte) ( ( Random.Shared.Next( 1, 201 ) + Random.Shared.Next( 1, 201 ) + 
                Random.Shared.Next( 1, 201 ) + Random.Shared.Next( 1, 201 ) ) / 4 );
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
            // TODO - Choose Random Variant or something akin to that. _shrineReplacementMonster._monsterVariants[ 0 ];
            var variant = MonsterBreed.GetBreed( _shrineReplacementMonster._genusNewMain, _shrineReplacementMonster._genusNewSub );
            WriteMonsterData( variant );

            Memory.Instance.Write( address_monster_mm_variant, _shrineColorVariant );

            shrineReplacementActive = false;
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

        Memory.Instance.WriteRaw( nuint.Add( address_monster, 0x1D0 ), BitConverter.GetBytes(breed.BattleSpecialsRaw) );

        _monsterCurrent.TrainBoost = breed.TrainBoost;

        Memory.Instance.WriteRaw( nuint.Add( address_monster, 0x192 ), breed.TechniquesRaw );
    }


    private void ProcessReloadedFileLoad ( string filename ) {
        //_logger.WriteLineAsync( $"Any file check {_monsterInsideBattleStartup}, {_monsterInsideBattleRedirects}, {_monsterInsideBattleMain}, {_monsterInsideBattleSub}", Color.Orange );
        if ( _monsterInsideBattleStartup ) {
            //_logger.WriteLineAsync( $"Inside File Checking for Monsters", Color.Orange );
            if ( _monsterInsideBattleRedirects == 1 ) {
                RedirectFromID( _monsterInsideBattleMain, _monsterInsideBattleSub ); 
                // Load the tournament monsters? I think?
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
    public Config _configuration;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    #endregion
}