using System.Diagnostics;
using System.Drawing;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.InfiniteFreezer.Configuration;
using MRDX.Qol.InfiniteFreezer.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using static MRDX.Qol.InfiniteFreezer.Mod;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Reloaded.Memory.Streams;

namespace MRDX.Qol.InfiniteFreezer;

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 44 A1 ?? ?? ?? ?? 33 C5 89 45 ?? 53 56 57 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 8B C1" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_ParsingFreezerData ( nuint self, int unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "56 57 33 FF 33 F6 33 D2" )]
[Function( CallingConventions.Fastcall )]
public delegate int H_FreezerFullCheck ( nuint self, int unk1 );


public class Mod : ModBase // <= Do not Remove.
{
    private readonly IHooks _iHooks;
    private readonly string? _modPath;

    private readonly IRedirectorController _redirector;
    private nuint _address_currentweek;
    private string? _dataPath;

    private nuint _address_game;
    private nuint _address_freezer { get { return _address_game + 0x3768BC; } }
    private nuint _address_freezer_page { get { return _address_game + 0x376676; } }
    private nuint _address_freezer_help_pointer1 { get { return _address_game + 0x1DF11B0; } }
    private nuint _address_freezer_help_pointer2 { get { return _address_game + 0x1DF11B4; } }
    private nuint _address_freezer_help_pointer3 { get { return _address_game + 0x1DF11B8; } }

    private FreezerMenuOptions _freezerStatus;

    public static byte[] emptyFreezerSlotTemplate =
        [   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
            0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB3, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB3, 0x00, 0xB3, 0x00, 0xB3, 0x00, 0xB3, 0x00, 0xB6, 0x00, 0xB6, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17, 0x03, 0x00, 0x00,
            0xCB, 0x03, 0x00, 0x00, 0xCB, 0x03, 0x00, 0x00, 0x7F, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x72, 0x0B, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x18, 0x18,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00];

    private readonly WeakReference<IGameClient>? _gameClient;
    private readonly WeakReference<IController>? _controller;
    private IHook<H_ParsingFreezerData> _hook_parsingFreezerData;
    private IHook<UpdateGenericState> _hook_updateGenericState;
    private IHook<H_FreezerFullCheck> _hook_freezerFullCheck;

    private ushort _freezerGroupMax = 0;
    private ushort _freezerCurrentGroup = 0;
    

    private List<USFreezerGroup> _freezerGroups = new List<USFreezerGroup>();

    private bool _freezerControlsEnabled = false;
    private bool _heldLeft = false;
    private bool _heldRight = false;

    private bool _helperTextSetup = false;

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
            saveFile.OnSave += SaveFreezerData;
            saveFile.OnLoad += LoadFreezerData;
        } else {
            _logger.WriteLine( $"[{_modConfig.ModId}] Could not get Save File controller.", Color.Red );
            return;
        }

            _controller.TryGetTarget( out var controller );
        if ( controller == null ) {
            _logger.WriteLine( $"[{_modConfig.ModId}] Could not get controller controller.", Color.Red );
            return;
        }

        controller.OnInputChanged += FreezerSwapLR;

        _iHooks.AddHook<H_ParsingFreezerData>( SetupParsingFreezerData )
            .ContinueWith( result => _hook_parsingFreezerData = result.Result );

        _iHooks.AddHook<H_FreezerFullCheck>( CheckFreezerStatus ).ContinueWith( result => _hook_freezerFullCheck = result.Result );
        _iHooks.AddHook<UpdateGenericState>( DisableFreezerControls ).ContinueWith( result => _hook_updateGenericState = result.Result );
    }

    private void SetupParsingFreezerData ( nuint parent, int unk1 ) {
        Memory.Instance.Read( _address_game + 0x369AB4, out byte optionSelected );
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
        }
    }

    private int CheckFreezerStatus ( nuint parent, int unk1 ) {
        int ret = _hook_freezerFullCheck!.OriginalFunction( parent, unk1 );
        Logger.Trace( $"Game is checking if freezer is full: {parent} | {unk1} || {ret}", Color.SkyBlue );

        Memory.Instance.Read( _address_game + 0x369AB4, out byte optionSelected );
        _freezerStatus = (FreezerMenuOptions) optionSelected;

        if ( _freezerStatus == FreezerMenuOptions.Freeze ) { 
            if ( ret == 20 ) {
                WriteFreezerToMod( _freezerCurrentGroup );

                while ( ret == 20 ) {
                    _freezerCurrentGroup++;

                    if ( _freezerCurrentGroup <= _freezerGroupMax ) { // Start cycling through freezers until we find an open space in one.
                        ret = 0;
                        for ( var i = 0; i < 20; i++ ) {
                            var slot = _freezerGroups[ _freezerCurrentGroup ].freezerSlots[ i ];

                            if ( slot[ 0x170 ] != 0xFF ) {
                                ret++;
                            }
                        }
                    }

                    else { // If we're at the end of the list of freezers make a new one and freeze the monster there.
                        _freezerGroupMax++;
                        _freezerGroups.Add( new USFreezerGroup( _freezerGroupMax ) );
                        _freezerCurrentGroup = _freezerGroupMax;
                    }
                }

                
                WriteModToFreezer( _freezerCurrentGroup );

                UpdateFreezerHelperText();

            }
        }

        else if ( _freezerStatus == FreezerMenuOptions.Combine || _freezerStatus == FreezerMenuOptions.Revive ||
            _freezerStatus == FreezerMenuOptions.Delete ) { 
            if ( ret == 0 ) {
                WriteFreezerToMod( _freezerCurrentGroup );

                var originalGroup = _freezerCurrentGroup;
                _freezerCurrentGroup = 0;
                while ( ret == 0 ) {
                    if ( _freezerCurrentGroup <= _freezerGroupMax ) { // Start cycling through freezers until we find one with at least 1 monster.
                        ret = 0;
                        for ( var i = 0; i < 20; i++ ) {
                            var slot = _freezerGroups[ _freezerCurrentGroup ].freezerSlots[ i ];

                            if ( slot[ 0x170 ] != 0xFF ) {
                                ret++;
                            }
                        }
                    }

                    else { // If we're at the and, reset the original group and return.
                        _freezerCurrentGroup = originalGroup;
                        return 0;
                    }

                    if ( ret == 0 ) { _freezerCurrentGroup++; }
                }

                // Write Mod Data to Freezer Memory
                WriteModToFreezer( _freezerCurrentGroup );

                if ( _freezerStatus == FreezerMenuOptions.Combine ) {
                    UpdateFreezerHelperText( "Freezer Locked" );
                } else {
                    UpdateFreezerHelperText();
                }
             

            }
        }

        return ret;
    }

    private void DisableFreezerControls ( nint parent ) {
        _hook_updateGenericState!.OriginalFunction( parent );

        if ( _freezerControlsEnabled ) {
            Logger.Debug( $"Disabling Freezer Controls", Color.SkyBlue );
            _freezerControlsEnabled = false;
        }

        _helperTextSetup = false;

    }
    private void FreezerSwapLR ( IInput input ) {
        if ( !_freezerControlsEnabled ) { return; }
        if ( _gameClient == null || !_gameClient.TryGetTarget( out var game ) ) return;

        var swapLeft = ( input.Buttons & ButtonFlags.DpadLeft ) != 0;
        var swapRight = ( input.Buttons & ButtonFlags.DpadRight ) != 0;

        var subMod = 0;

        if ( swapLeft && !_heldLeft ) {
            if ( _freezerCurrentGroup > 0 ) {
                subMod = -1;
            }
        }

        else if ( swapRight && !_heldRight ) {

            // Expand the current tracked freezer size.
            if ( _freezerGroupMax == _freezerCurrentGroup ) {
                _freezerGroupMax++;

                _freezerGroups.Add( new USFreezerGroup( _freezerGroupMax ) );
            }

            subMod = 1;

        }

        if ( subMod != 0 ) {

            // Write Freezer Memory to Mod Data
            WriteFreezerToMod( _freezerCurrentGroup );

            _freezerCurrentGroup = (byte) ( _freezerCurrentGroup + subMod );

            // Write Mod Data to Freezer Memory
            WriteModToFreezer( _freezerCurrentGroup );

            // Write Freezer Page Help Data - Freezer Numbers
            UpdateFreezerHelperText();

        }

        _heldLeft = swapLeft;
        _heldRight = swapRight;
    }

    /// <summary>
    /// This function is used to replace the in memory freezer data with the mod's freeer group data.
    /// </summary>
    /// <param name="group"></param>
    private void WriteModToFreezer(ushort group) {
        for ( var i = 0; i < 20; i++ ) {
            Memory.Instance.Write( nuint.Add( _address_freezer, ( 524 * i ) ), _freezerGroups[ group ].freezerSlots[ i ] );
        }
    }

    /// <summary>
    /// This function is used to replace a mod's group with the current state of the active in-game freezer.
    /// </summary>
    /// <param name="group"></param>
    private void WriteFreezerToMod(ushort group) {
        var monData = new byte[ 524 ];
        for ( var i = 0; i < 20; i++ ) {
            Memory.Instance.ReadRaw( nuint.Add( _address_freezer, ( 524 * i ) ), out monData, 524 );
            Array.Copy( monData, _freezerGroups[ group ].freezerSlots[ i ], 524 );
        }
    }

    private void UpdateFreezerHelperText( string customMessage = "" ) {
        if ( _helperTextSetup ) { return; }
        var ftext = System.Text.Encoding.Unicode.GetBytes(
            $"Freezer: {( _freezerCurrentGroup + 1 )}/" +
            $"{( _freezerGroupMax + 1 )}               x" );

        var r1Message = new byte[ 17 ];
        for ( var j = 0; j < 17; j++ ) {
            r1Message[ j ] = ftext[ j * 2 ];
        }

        var r3Message = new byte[ 17 ];
        if ( customMessage != "" ) {
            var text = System.Text.Encoding.Unicode.GetBytes( customMessage + "                      " );
            for ( var j = 0; j < 17; j++ ) {
                r3Message[ j ] = text[ j * 2 ];
            }
        }

        Memory.Instance.Read( _address_freezer_help_pointer1, out nuint addr_help1 );

        if ( addr_help1 == 0x00 ) { return; } // It doesn't write these addresses until it's needed.

        Memory.Instance.WriteRaw( addr_help1, r1Message );

        // Write Freezer Page Help Data - Freezer Name
        Memory.Instance.Read( _address_freezer_help_pointer2, out nuint addr_help2 );
        Memory.Instance.WriteRaw( addr_help2, _freezerGroups[ _freezerCurrentGroup ].nameMemoryMapped );

        // Write Freezer Page Help Data - Empty
        Memory.Instance.Read( _address_freezer_help_pointer3, out nuint addr_help3 );
        Memory.Instance.WriteRaw( addr_help3, r3Message );

        _helperTextSetup = true;
    }

    private void SaveFreezerData ( ISaveFileEntry savefile ) {

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
        }
    }


    private void LoadFreezerData ( ISaveFileEntry savefile ) {

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

        Logger.Info( $"Freezer data successfully loaded.", Color.SkyBlue );
    }

    public class USFreezerGroup {
        public string name;

        public byte[] nameMemoryMapped;

        public ushort group;

        public List<byte[]> freezerSlots;

        public bool utilized;

        public USFreezerGroup( ushort groupnum, string gname = null) {
            name = ( gname == null ) ? $"Freezer {groupnum}" : gname;
            group = groupnum;
            freezerSlots = new List<byte[]>();
            utilized = false;

            for ( var i = 0; i < 20; i++ ) {
                byte[] eSlot = new byte[ 524 ];
                Array.Copy( emptyFreezerSlotTemplate, eSlot, 524 );

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