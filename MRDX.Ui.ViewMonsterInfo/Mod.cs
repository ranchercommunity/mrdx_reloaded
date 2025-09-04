using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod.Interfaces;
using MRDX.Ui.ViewMonsterInfo.Configuration;
using MRDX.Ui.ViewMonsterInfo.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;

namespace MRDX.Ui.ViewMonsterInfo;

[StructLayout( LayoutKind.Explicit )]
public struct Box {
    [FieldOffset( 0x0 )]
    public nint Next;

    [FieldOffset( 0x4 )]
    public nint Previous;

    [FieldOffset( 0x8 )]
    public nint Attribute;

    [FieldOffset( 0xC )]
    public short XCopy;

    [FieldOffset( 0xE )]
    public short YCopy;

    [FieldOffset( 0x10 )]
    public short X;

    [FieldOffset( 0x12 )]
    public short Y;

    [FieldOffset( 0x1E )]
    public short Z;

    [FieldOffset( 0x2C )]
    public short XOffset;

    [FieldOffset( 0x2E )]
    public short YOffset;

    [FieldOffset( 0x40 )]
    public int unk1;

    [FieldOffset( 0x44 )]
    public int unk2;

    [FieldOffset( 0x48 )]
    public int unk3;
}


[StructLayout( LayoutKind.Explicit )]
public struct BoxAttribute {
    [FieldOffset( 0x0 )]
    public byte unk1;

    [FieldOffset( 0x10 )]
    public ushort Width;

    [FieldOffset( 0x12 )]
    public ushort Height;

    [FieldOffset( 0x14 )]
    public byte R;

    [FieldOffset( 0x15 )]
    public byte G;

    [FieldOffset( 0x16 )]
    public byte B;

    [FieldOffset( 0x17 )]
    public byte IsSemiTransparent;
}

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 56 57 8B 7D 08 6A 03" )]
[Function( CallingConventions.Cdecl )]
public delegate int DrawLoyalty ( nint unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 53 56 8B 75 08 8D 44 24 14" )]
[Function( CallingConventions.Fastcall )]
public delegate int DrawTextWithPadding ( short x, short y, nint text, short padding );

// Function that is called every tick while you're in the farm.
// Hooking this causes various UI elements in the Farm to not function for some reason.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 53 56 57 A1 ?? ?? ?? ?? 33 C5 50 8D 45 F4 64 A3 ?? ?? ?? ?? 8B 75 08 8B 7D 10 F6 46 0C 03 0F 84" )]
[Function( CallingConventions.Cdecl )]
public delegate void DrawFarmUiElements ( nint unk1, nint unk2, nint unk3 );

[HookDef( BaseGame.Mr2, Region.Us, "80 79 38 01 75 38" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void RemovesSomeUiElements ( nint self );

    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private IHook<DrawLoyalty>? _loyaltyHook;
    private DrawTextWithPadding? _drawText;
    private IHook<RemovesSomeUiElements>? _removeUiHook;

    private nuint _address_game;
    private IMonster monster;

    private List<nint> allAddresses = new List<nint>();
    private List<Box> allBoxes = new List<Box>();

    private nint rootBoxPtr;
    private Box rootBox;

    private bool initialized = false;

    private nint stressTextAddr;
    private nint fatigueTextAddr;
    private nint lifeIndexTextAddr;

    public Mod ( ModContext context ) {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;

        _modLoader.GetController<IHooks>().TryGetTarget( out var hooks );

        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        _address_game = (nuint) module.BaseAddress.ToInt64();

        hooks!.AddHook<RemovesSomeUiElements>( RestoreUiLinkedList ).ContinueWith( result => _removeUiHook = result.Result.Activate() );
        hooks!.AddHook<DrawLoyalty>( DrawLifeIndex ).ContinueWith( result => _loyaltyHook = result.Result.Activate() );
        hooks!.CreateWrapper<DrawTextWithPadding>().ContinueWith( result => _drawText = result.Result );

        WeakReference<IGame> _game = _modLoader.GetController<IGame>();
        _game.TryGetTarget( out var g );
        g!.OnMonsterChanged += MonsterChanged;
    }

    private void MonsterChanged ( IMonsterChange mon ) {
        monster = mon.Current;

        if ( stressTextAddr != 0 ) {
            Marshal.FreeCoTaskMem( stressTextAddr );
            Marshal.FreeCoTaskMem( fatigueTextAddr );
            Marshal.FreeCoTaskMem( lifeIndexTextAddr );
        }

        AllocateText( monster );
    }

    private void AllocateText ( IMonster monster ) {
        byte[] stress = $"Stress:{monster.Stress}".AsMr2().AsBytes();

        stressTextAddr = Marshal.AllocCoTaskMem( stress.Length );
        Marshal.Copy( stress, 0, stressTextAddr, stress.Length );

        byte[] fatigue = $"Fatigue:{monster.Fatigue}".AsMr2().AsBytes();

        fatigueTextAddr = Marshal.AllocCoTaskMem( fatigue.Length );
        Marshal.Copy( fatigue, 0, fatigueTextAddr, fatigue.Length );

        byte[] lifeIndex = getLifeIndexText( monster ).AsMr2().AsBytes();

        lifeIndexTextAddr = Marshal.AllocCoTaskMem( lifeIndex.Length );
        Marshal.Copy( lifeIndex, 0, lifeIndexTextAddr, lifeIndex.Length );
    }

    private int getLifeIndex ( IMonster monster ) {
        return monster.Fatigue + ( monster.Stress * 2 );
    }

    private string getLifeIndexText ( IMonster monster ) {
        int lifeIndex = getLifeIndex( monster );
        int lifespanHit = getLifespanHit( monster );

        return $"LI:{lifeIndex}({lifespanHit}w)";
    }

    private int getLifespanHit ( IMonster monster ) {
        int lifeIndex = getLifeIndex( monster );

        if ( lifeIndex >= 280 ) {
            return -7;
        }

        return -1 * ( ( lifeIndex - 70 ) / 35 + 1 );
    }

    private void RestoreUiLinkedList ( nint self ) {
        nint CSysFarmPtrPtr = Marshal.ReadInt32( (nint) _address_game + 0x372308 );

        if ( CSysFarmPtrPtr != 0 ) {
            nint CSysFarmPtr = Marshal.ReadInt32( CSysFarmPtrPtr + 0x3C );
            byte isDisplayed = Marshal.ReadByte( CSysFarmPtr + 0x38 );

            if ( initialized == true && isDisplayed == 1 ) {
                rootBox.Next = allBoxes.Last().Next;
                Marshal.StructureToPtr( rootBox, rootBoxPtr, false );

                allBoxes = new List<Box>();

                for ( int i = 0; i < allAddresses.Count; i++ ) {
                    Marshal.FreeCoTaskMem( allAddresses[ i ] );
                }

                allAddresses = new List<nint>();

                initialized = false;
            }
        }

        _removeUiHook!.OriginalFunction( self );
    }

    private void PrependToBoxList ( Box box, nint boxAddr ) {
        var rootBoxNext = rootBox.Next;
        rootBox.Next = boxAddr;
        box.Next = rootBoxNext;
        box.Previous = rootBoxPtr;

        var next = Marshal.PtrToStructure<Box>( rootBoxNext );
        next.Previous = boxAddr;

        Marshal.StructureToPtr( next, rootBoxNext, false );
        Marshal.StructureToPtr( rootBox, rootBoxPtr, false );
        Marshal.StructureToPtr( box, boxAddr, false );
    }

    private BoxAttribute GetBackgroundBoxAttribute ( ushort width, ushort height ) {
        BoxAttribute backgroundAttr = new BoxAttribute();
        backgroundAttr.unk1 = 5;
        backgroundAttr.Width = width;
        backgroundAttr.Height = height;
        backgroundAttr.R = 128;
        backgroundAttr.G = 128;
        backgroundAttr.B = 128;
        backgroundAttr.IsSemiTransparent = 0;
        return backgroundAttr;
    }

    private BoxAttribute GetForegroundBoxAttribute ( ushort width, ushort height ) {
        BoxAttribute backgroundAttr = new BoxAttribute();
        backgroundAttr.unk1 = 5;
        backgroundAttr.Width = width;
        backgroundAttr.Height = height;
        backgroundAttr.R = 64;
        backgroundAttr.G = 64;
        backgroundAttr.B = 128;
        backgroundAttr.IsSemiTransparent = 1;

        return backgroundAttr;
    }

    private Box GetBox ( short x, short y, short z, nint boxAttrPtr ) {
        Box box = new Box();
        box.Attribute = boxAttrPtr;
        box.X = x;
        box.Y = y;
        box.XCopy = box.X;
        box.YCopy = box.Y;
        box.Z = z;
        box.XOffset = 0;
        box.YOffset = 0;

        return box;
    }

    private void DrawBox ( ushort width, ushort height, short x, short y ) {
        BoxAttribute backgroundAttr = GetBackgroundBoxAttribute( width, height );

        nint backgroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( backgroundAttr ) );
        Marshal.StructureToPtr( backgroundAttr, backgroundAttrPtr, false );

        allAddresses = allAddresses.Prepend( backgroundAttrPtr ).ToList();

        Box box = GetBox( x, y, 2, backgroundAttrPtr );

        nint boxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( box ) );

        PrependToBoxList( box, boxAddr );

        allBoxes = allBoxes.Prepend( box ).ToList();
        allAddresses = allAddresses.Prepend( boxAddr ).ToList();


        BoxAttribute foregroundAttr = GetForegroundBoxAttribute( (ushort) ( width - 4 ), (ushort) ( height - 4 ) );

        nint foregroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( foregroundAttr ) );
        Marshal.StructureToPtr( foregroundAttr, foregroundAttrPtr, false );

        allAddresses = allAddresses.Prepend( foregroundAttrPtr ).ToList();

        Box foregroundBox = GetBox( (short) ( x + 2 ), (short) ( y + 2 ), 3, foregroundAttrPtr );

        nint foregroundBoxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( foregroundBox ) );

        PrependToBoxList( foregroundBox, foregroundBoxAddr );

        allBoxes = allBoxes.Prepend( foregroundBox ).ToList();
        allAddresses = allAddresses.Prepend( foregroundBoxAddr ).ToList();
    }

    private void DrawStressBox () {
        DrawBox( 75, 18, -130, 56 );
    }

    private void DrawFatigueBox () {
        DrawBox( 78, 18, -52, 56 );
    }

    private void DrawLifeIndexBox () {
        DrawBox( 100, 18, 29, 56 );
    }

    private void Init () {
        nint rootBoxPtrPtr;
        // There can be up to 4 pointers stored in an array, where each element
        // will point to a linked list of UI elements to draw.
        // We're reading the array from behind as a hack to get around an issue
        // with the item shop, where if you back out of the item shop
        // the linked list that we're interested in would be stored at the back 
        // of the array because for a split second multiple linked list of UI
        // elements are rendered.
        for ( int i = 3; i >= 0; i-- ) {
            rootBoxPtrPtr = (nint) _address_game + 0x369900 + 4 * i;
            rootBoxPtr = Marshal.ReadInt32( rootBoxPtrPtr );

            if ( rootBoxPtr != 0 ) {
                break;
            }
        }

        rootBox = Marshal.PtrToStructure<Box>( rootBoxPtr );
        DrawStressBox();
        DrawFatigueBox();
        DrawLifeIndexBox();
        initialized = true;
    }

    private int DrawLifeIndex ( nint unk1 ) {
        if ( initialized == false ) {
            Init();
        }

        _drawText!( -92, 57, stressTextAddr, 0 );
        _drawText!( -14, 57, fatigueTextAddr, 0 );
        _drawText!( 74, 57, lifeIndexTextAddr, 0 );

        return _loyaltyHook!.OriginalFunction( unk1 );
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod () {
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