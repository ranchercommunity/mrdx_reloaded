using System.Diagnostics;
using System.Drawing;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.InfiniteHallOfFame.Configuration;
using MRDX.Qol.InfiniteHallOfFame.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using static MRDX.Qol.InfiniteHallOfFame.Mod;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Reloaded.Memory.Streams;

namespace MRDX.Qol.InfiniteHallOfFame;

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC F4 08 00 00" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_HallOfFameButtonTextWrite ( nuint self, int unk1 );


public class Mod : ModBase // <= Do not Remove.
{
    private readonly IHooks _iHooks;
    private readonly string? _modPath;

    private readonly IRedirectorController _redirector;
    private string? _dataPath;

    private nuint _address_game;
    private nuint _address_hof_unlocked { get { return _address_game + 0x379718; } }
    private nuint _address_hof_1 { get { return _address_game + 0x37971C; } }
    private nuint _address_hof_2 { get { return _address_hof_1 + 96; } }
    private nuint _address_hof_3 { get { return _address_hof_2 + 96; } }


    public static byte[] emptyHoFSlotTemplate =
        [ 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
        0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
        0x00,0x00,0x00,0x00,0xFF,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00];
        

    private readonly WeakReference<IGameClient>? _gameClient;
    private readonly WeakReference<IController>? _controller;
    private IHook<H_HallOfFameButtonTextWrite> _hook_hallOfFameButtonTextWrite;
    private IHook<UpdateGenericState> _hook_updateGenericState;

    private ushort _hofGroupMax = 0;
    private ushort _hofGroupCurrent = 0;
    
    private List<ABDHoFGroup> _hofGroups = new List<ABDHoFGroup>();

    private bool _HoFControlsEnabled = false;
    private bool _heldLeft = false;
    private bool _heldRight = false;

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

        _gameClient = _modLoader.GetController<IGameClient>();
        _controller = _modLoader.GetController<IController>();

        if (extract == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get extract data bin controller.", Color.Red);
            return;
        }

        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        _address_game = (nuint)module.BaseAddress.ToInt64();

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

        var maybeSaveFile = _modLoader.GetController<ISaveFile>();
        if ( maybeSaveFile != null && maybeSaveFile.TryGetTarget( out var saveFile ) ) {
            saveFile.OnSave += SaveHoFData;
            saveFile.OnLoad += LoadHoFData;
        } else {
            _logger.WriteLine( $"[{_modConfig.ModId}] Could not get Save File controller.", Color.Red );
            return;
        }

            _controller.TryGetTarget( out var controller );
        if ( controller == null ) {
            _logger.WriteLine( $"[{_modConfig.ModId}] Could not get controller controller.", Color.Red );
            return;
        }

        controller.OnInputChanged += HoFSwapLR;

        _iHooks.AddHook<H_HallOfFameButtonTextWrite>( HFHallOfFameButtonTextWrite )
            .ContinueWith( result => _hook_hallOfFameButtonTextWrite = result.Result );

        _iHooks.AddHook<UpdateGenericState>( DisableHoFControls ).ContinueWith( result => _hook_updateGenericState = result.Result );
    }

    private void HFHallOfFameButtonTextWrite( nuint parent, int unk1 ) {
        _hook_hallOfFameButtonTextWrite!.OriginalFunction( parent, unk1 );

        if ( !_HoFControlsEnabled ) {
            Logger.Debug( $"Enabling Hall of Fame Controls: {parent} | {unk1}", Color.Beige );
            _HoFControlsEnabled = true;
        }


    }
    private void SetupParsingFreezerData ( nuint parent, int unk1 ) {
        /*Memory.Instance.Read( _address_game + 0x369AB4, out byte optionSelected );
        _freezerStatus = (FreezerMenuOptions) optionSelected;

        if ( _freezerStatus != FreezerMenuOptions.Combine ) {
            Logger.Debug( $"Enabling Freezer Controls: {parent} | {unk1} | {optionSelected} ", Color.SkyBlue );
            _freezerControlsEnabled = true;
        }

        _hook_parsingFreezerData!.OriginalFunction( parent, unk1 );

        if ( _freezerStatus == FreezerMenuOptions.Combine ) {
            UpdateFreezerHelperText( "Freezer Locked" );
        } else {
            UpdateFreezerHelperText();
        }*/
    }



    private void DisableHoFControls ( nint parent ) {
        _hook_updateGenericState!.OriginalFunction( parent );

        if ( _HoFControlsEnabled ) {
            Logger.Debug( $"Disabling Hall of Fame Controls", Color.Beige );
            _HoFControlsEnabled = false;
        }
    }

    private void HoFSwapLR ( IInput input ) {
        if ( !_HoFControlsEnabled ) { return; }
        if ( _gameClient == null || !_gameClient.TryGetTarget( out var game ) ) return;

        var swapLeft = ( input.Buttons & ButtonFlags.DpadLeft ) != 0;
        var swapRight = ( input.Buttons & ButtonFlags.DpadRight ) != 0;

        var subMod = 0;

        if ( swapLeft && !_heldLeft ) {
            if ( _hofGroupCurrent > 0 ) {
                subMod = -1;
            }
        }

        else if ( swapRight && !_heldRight ) {

            if ( _hofGroupCurrent < _hofGroupMax ) {
                subMod = 1;
            }
        }

        if ( subMod != 0 ) {

            // Write Hall of Fame Memory to Mod Data
            WriteHoFToMod( _hofGroupCurrent );

            _hofGroupCurrent = (byte) ( _hofGroupCurrent + subMod );

            // Write Mod Data to Hall of Fame Memory
            WriteModToHoF( _hofGroupCurrent );
        }

        _heldLeft = swapLeft;
        _heldRight = swapRight;
    }

    /// <summary>
    /// This function is used to replace the in memory freezer data with the mod's freeer group data.
    /// </summary>
    /// <param name="group"></param>
    private void WriteModToHoF(ushort group) {
        for ( var i = 0; i < 20; i++ ) {
            //Memory.Instance.Write( nuint.Add( _address_freezer, ( 524 * i ) ), _freezerGroups[ group ].freezerSlots[ i ] );
        }
    }

    /// <summary>
    /// This function is used to replace a mod's group with the current state of the active in-game freezer.
    /// </summary>
    /// <param name="group"></param>
    private void WriteHoFToMod(ushort group) {
        var monData = new byte[ 524 ];
        for ( var i = 0; i < 20; i++ ) {
            //Memory.Instance.ReadRaw( nuint.Add( _address_freezer, ( 524 * i ) ), out monData, 524 );
            //Array.Copy( monData, _freezerGroups[ group ].freezerSlots[ i ], 524 );
        }
    }


    private void SaveHoFData ( ISaveFileEntry savefile ) {
        /*
        var saveDataFolder = Path.Combine( Path.GetDirectoryName( savefile.Filename ), $"InfiniteFreezer\\SaveFile_{savefile.Slot}\\" );
        Directory.CreateDirectory( saveDataFolder );

        foreach ( USFreezerGroup freezerGroup in _freezerGroups ) {
            var file = Path.Combine( saveDataFolder, $"ifreezer_{freezerGroup.group}.bin" );

            using var fs = new FileStream( file, FileMode.Create );
            fs.Write( freezerGroup.group );
            fs.Write( 0xFFFFFFFF );
            fs.Write( _freezerCurrentGroup );
            fs.Write( 0xFFFFFFFF );
            fs.Write( freezerGroup.nameMemoryMapped );
            fs.Write( 0xFFFFFFFF );
            fs.Write( freezerGroup.utilized ? 1 : 0 );
            fs.Write( new byte[ 487 ] );
            fs.Write( 0xFFFFFFFF );

            for ( var j = 0; j < 20; j++ ) {
                fs.Write( freezerGroup.freezerSlots[ j ] );
                fs.Write( 0xFFFFFFFF );
            }

            fs.Close();

            Logger.Info( $"{file} successfully written.", Color.SkyBlue );
        }*/
    }


    private void LoadHoFData ( ISaveFileEntry savefile ) {
        /*
        var saveDataFolder = Path.GetDirectoryName( savefile.Filename );
        saveDataFolder = Path.Combine( saveDataFolder, $"InfiniteFreezer\\SaveFile_{savefile.Slot}\\" );
        Directory.CreateDirectory( saveDataFolder );

        _freezerCurrentGroup = 0;
        _freezerGroupMax = 0;
        _freezerGroups = new List<USFreezerGroup>();

        var freezerFiles = Directory.EnumerateFiles( saveDataFolder );
        freezerFiles = freezerFiles.Where( file => file.Contains( $"ifreezer_" ) );

        var maxFG = 0;
        foreach ( var fn in freezerFiles ) {
            var fg = fn.Substring( fn.LastIndexOf( "_" ) + 1, ( fn.LastIndexOf( "." ) - fn.LastIndexOf( "_" ) ) - 1 );
            maxFG = Math.Max( ushort.Parse( fg ), maxFG );
        }

        for ( var i = 0; i <= maxFG; i++ ) {
            _freezerGroups.Add( new USFreezerGroup( (ushort) i ) );
        }
        _freezerGroupMax = (ushort) maxFG;

        foreach ( var fn in freezerFiles ) {
            var fg = fn.Substring( fn.LastIndexOf( "_" ) + 1, ( fn.LastIndexOf( "." ) - fn.LastIndexOf( "_" ) ) - 1 );
            var file = Path.Combine( saveDataFolder, $"ifreezer_{fg}.bin" );

            if ( File.Exists( file ) ) {
                try {
                    var fgData = File.ReadAllBytes( file );
                    var freezerGroup = _freezerGroups[ ushort.Parse(fg) ];

                    freezerGroup.group = ushort.Parse( fg );
                    _freezerCurrentGroup = (ushort) (fgData[ 6 ] + ( fgData[ 7 ] << 8) );
                    Array.Copy( fgData, 12, freezerGroup.nameMemoryMapped, 0, 17 );
                    freezerGroup.utilized = fgData[ 33 ] == 1;

                    for ( var i = 0; i < 20; i++ ) {
                        Array.Copy( fgData, 528 + ( i * ( 524 + 4 ) ), freezerGroup.freezerSlots[ i ], 0, 524 );
                    }

                    _freezerGroups[ ushort.Parse( fg ) ] = freezerGroup;
                }
                catch ( Exception e ) {
                    // Failed to load the extra monsters from the savefile, so load them from the default enemy file
                    Logger.Info( $"Failed to load freezer file ${file} due to the following error: ${e.Message}", Color.SkyBlue );
                }
            }
        }

        // Write Freezer Memory to Mod Data
        var monData = new byte[ 524 ];
        for ( var i = 0; i < 20; i++ ) {
            Memory.Instance.ReadRaw( nuint.Add( _address_freezer, ( 524 * i ) ), out monData, 524 );
            Array.Copy( monData, _freezerGroups[ _freezerCurrentGroup ].freezerSlots[ i ], 524 );
        }

        Logger.Info( $"Freezer data successfully loaded.", Color.SkyBlue );*/
    }

    public class ABDHoFGroup {
        public string name;

        public byte[] nameMemoryMapped;

        public ushort group;

        public List<byte[]> freezerSlots;

        public bool utilized;

        public ABDHoFGroup( ushort groupnum, string gname = null) {
            name = ( gname == null ) ? $"Freezer {groupnum}" : gname;
            group = groupnum;
            freezerSlots = new List<byte[]>();
            utilized = false;

            for ( var i = 0; i < 20; i++ ) {
                byte[] eSlot = new byte[ 524 ];
                Array.Copy( emptyHoFSlotTemplate, eSlot, 524 );

                freezerSlots.Add( eSlot );
            }

            var ftext = System.Text.Encoding.Unicode.GetBytes( name );
            nameMemoryMapped = new byte[ 17 ];
            var len = Math.Min( name.Length, 17 );
            for ( var j = 0; j < len; j++ ) {
                nameMemoryMapped[ j ] = ftext[ j * 2 ];
            }
        }
    }

    private enum FreezerMenuOptions { Combine, Freeze, Revive, Delete, Analyze }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

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