using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Iced.Intel;
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
    public byte Type;

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

/**
 * Common text based drawing code used by several different text rendering routines
 * textParams is typically 0x0010000C, represented as two shorts. 0x0010 is the 'height' of the text, 0x000C is the 'width'.
 * colorPtr points to a representation as reversed hex. F7F700, which is a bright yellow, would be 0x007f7f
 */
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 81 EC B0 04 00 00" )]
[Function( CallingConventions.Fastcall )]
public delegate int DrawTextToScreenPORT ( uint x, ushort y, nint textPtr, int textParams, nint colorPtr );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 78 A1 ?? ?? ?? ?? 33 C5 89 45 ?? 8B 45 ??")]
[Function( CallingConventions.Fastcall )]
public delegate void DrawStyle ( int unk1, int y );

[ HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 56 57 8B 7D 08 6A 03" )]
[Function( CallingConventions.Cdecl )]
public delegate int DrawLoyalty ( nint unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 56 8B F1 57 BF E7 03 00 00" )]
[Function( CallingConventions.Fastcall )]
public delegate void DrawMonsterInfoPage2 ( int unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC 14 09 00 00")]
[Function( CallingConventions.Fastcall )]
public delegate void DrawMonsterInfoPage3 ( int unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC 7C 09 00 00" )]
[Function( CallingConventions.Fastcall )]
public delegate void DrawMonsterInfoPage4 ( int unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 53 56 8B 75 08 8D 44 24 14" )]
[Function( CallingConventions.Fastcall )]
public delegate int DrawTextWithPadding ( short x, short y, nint text, short padding );

// Function that is called every tick while you're in the farm.
// Hooking this causes various UI elements in the Farm to not function for some reason.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 53 56 57 A1 ?? ?? ?? ?? 33 C5 50 8D 45 F4 64 A3 ?? ?? ?? ?? 8B 75 08 8B 7D 10 F6 46 0C 03 0F 84" )]
[Function( CallingConventions.Cdecl )]
public delegate int DrawFarmUiElements ( nint unk1, nint unk2, nint unk3 );

[HookDef( BaseGame.Mr2, Region.Us, "80 79 38 01 75 38" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void RemovesSomeUiElements ( nint self );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 81 EC C0 08 00 00 A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 56 57 8B F2 8B F9 8B 4D ?? 8D 94 24 ?? ?? ?? ?? E8 ?? ?? ?? ?? 8D 44 24 ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void DrawUIBoxes ( nint dt0, nint dt1, int unk2, int unk3 );


// TODO - This signature is wrong. However, it's also not needed.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 48 A1 ?? ?? ?? ?? 33 C5 89 45 ?? 8B 45 ?? 53 56")]
[Function( CallingConventions.Fastcall )]
public delegate int UNKModifyRootBox ( ushort unk1, nuint unk2, int unk3 );

[HookDef( BaseGame.Mr2, Region.Us, "53 8B D9 56 8B 34 9D ?? ?? ?? ??")]
[Function( CallingConventions.Fastcall )]
public delegate void UNKClearRootBox ( int unk1 );


[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 10 53 33 C0")]
[Function( CallingConventions.Fastcall )]
public delegate void UNKTechniquePageChangeTech ( nuint unk1 );

[HookDef( BaseGame.Mr2, Region.Us, "56 8B F1 57 8B 86 ?? ?? ?? ?? 8B BE ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void UNKTechniqueUpdateSelectedData ( nuint unk1 );

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private IHook<DrawStyle>? _hook_drawStyle;
    private IHook<DrawLoyalty>? _hook_drawLoyalty;

    private IHook<DrawFarmUiElements> _hook_drawFarmUIElements;
    private IHook<DrawTextToScreenPORT> _hook_drawTextToScreen;
    private IHook<RemovesSomeUiElements>? _removeUiHook;
    private IHook<DrawUIBoxes>? _hook_drawUIBoxes;

    private IHook<DrawMonsterInfoPage2>? _hook_drawMonsterInfoPage2;
    private IHook<DrawMonsterInfoPage3>? _hook_drawMonsterInfoPage3;
    private IHook<DrawMonsterInfoPage4>? _hook_drawMonsterInfoPage4;

    private IHook<UNKClearRootBox>? _hook_clearRootBox;
    private IHook<UNKModifyRootBox>? _hook_modifyRootBox;

    private IHook<UNKTechniquePageChangeTech> _hook_techPageChangeTech;
    private IHook<UNKTechniqueUpdateSelectedData> _hook_techPageUpdateSelected;
    private byte _techSelected = 0;
    private byte[] _techUses = new byte[24];

    private ParseTextWithCommandCodes? _wrapfunc_parseTextWithCommandCodes;
    private DrawTextWithPadding? _wrapfunc_drawTextWithPadding;
    private DrawTextToScreenPORT? _wrapfunc_drawTextToScreen;

    private nuint _address_game;
    private nuint _address_monster;
    private IMonster monster;

    private List<Box> boxesAll = new List<Box>();
    private List<nint> addressesAdded = new List<nint>();
    private List<Box> boxesAdded = new List<Box>();

    private List<MR2UIElementBox> boxElements = new List<MR2UIElementBox>();
    private List<MR2UITextElement> textElements = new List<MR2UITextElement>();

    private nint _rootBoxPtrPrev;
    private nint rootBoxPtr;
    private Box rootBox;

    private int _previousFarmStatus = -1;
    private bool initialized = false;
    private bool initialized_mp3 = false;
    private bool initialized_mp4 = false;

    private (bool, bool) _watchMove_SLMInfo = (false, false);


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
        _address_monster = nuint.Add( _address_game, 0x37667C );

        hooks!.AddHook<DrawStyle>( HFDrawStyle ).ContinueWith( result => _hook_drawStyle = result.Result.Activate() );
        hooks!.AddHook<DrawLoyalty>( DrawLifeIndex ).ContinueWith( result => _hook_drawLoyalty = result.Result.Activate() );

        hooks!.AddHook<DrawFarmUiElements>( HFDrawFarmUIElements ).ContinueWith( result => _hook_drawFarmUIElements = result.Result.Activate() );
        hooks!.AddHook<RemovesSomeUiElements>( RestoreUiLinkedList ).ContinueWith( result => _removeUiHook = result.Result.Activate() );
        
        
        hooks!.AddHook<DrawTextToScreenPORT>( HFDrawTextToScreen ).ContinueWith( result => _hook_drawTextToScreen = result.Result.Activate() );
        hooks!.AddHook<DrawUIBoxes>( HFDrawUIBoxes ).ContinueWith( result => _hook_drawUIBoxes = result.Result.Activate() );
        hooks!.AddHook<DrawMonsterInfoPage2>( HFDrawMonsterInfoPage2 ).ContinueWith( result => _hook_drawMonsterInfoPage2 = result.Result.Activate() );
        hooks!.AddHook<DrawMonsterInfoPage3>( HFDrawMonsterInfoPage3 ).ContinueWith( result => _hook_drawMonsterInfoPage3 = result.Result.Activate() );
        hooks!.AddHook<DrawMonsterInfoPage4>( HFDrawMonsterInfoPage4 ).ContinueWith( result => _hook_drawMonsterInfoPage4 = result.Result.Activate() );

        hooks!.CreateWrapper<ParseTextWithCommandCodes>().ContinueWith( result => _wrapfunc_parseTextWithCommandCodes = result.Result );
        hooks!.CreateWrapper<DrawTextWithPadding>().ContinueWith( result => _wrapfunc_drawTextWithPadding = result.Result );
        hooks!.CreateWrapper<DrawTextToScreenPORT>().ContinueWith( result => _wrapfunc_drawTextToScreen = result.Result );

        //hooks!.AddHook<UNKModifyRootBox>( HFModifyRootBox ).ContinueWith( result => _hook_modifyRootBox = result.Result.Activate() );
        hooks!.AddHook<UNKClearRootBox>( HFClearRootBox ).ContinueWith( result => _hook_clearRootBox = result.Result.Activate() );

        hooks!.AddHook<UNKTechniquePageChangeTech>( HFTechniquePageChangeTech ).ContinueWith( result => _hook_techPageChangeTech = result.Result.Activate() );
        hooks!.AddHook<UNKTechniqueUpdateSelectedData>( HFTechniqueUpdateSelectedData ).ContinueWith( result => _hook_techPageUpdateSelected = result.Result.Activate() );

        WeakReference<IGame> _game = _modLoader.GetController<IGame>();
        _game.TryGetTarget( out var g );
        g!.OnMonsterChanged += MonsterChanged;


    }

    public int HFModifyRootBox ( ushort unk1, nuint unk2, int unk3 ) {
        int ret = _hook_modifyRootBox!.OriginalFunction( unk1, unk2, unk3 );
        //Logger.Error( $"ModifyRootBox: {unk1} {unk2} {unk3} : {ret}" );
        return ret;
    }


    public void HFClearRootBox ( int unk1 ) {
        //Logger.Error( $"ClearRootBox: {unk1}" );
        RestoreUILinkedList();
        _hook_clearRootBox!.OriginalFunction( unk1 );
    }

    public void HFTechniquePageChangeTech ( nuint unk1 ) {
        //Logger.Error( $"TechPageChangeTech: {unk1.ToString( "X" )}" );
        _hook_techPageChangeTech!.OriginalFunction( unk1 );
        
    }

    public void HFTechniqueUpdateSelectedData ( nuint unk1 ) {
        //Logger.Error( $"TechniqueUpdateSelected: {unk1.ToString( "X" )}" );

        _hook_techPageUpdateSelected!.OriginalFunction( unk1 );
        Memory.Instance.Read( unk1 + 0x108, out nuint techChosenPtr );
        Memory.Instance.Read( techChosenPtr, out _techSelected );

        textElements[ 16 ].UpdateText( _techUses[ _techSelected ].ToString() );
        textElements[ 16 ].FreeMemory();
        textElements[ 16 ].AllocateMemory( _wrapfunc_parseTextWithCommandCodes );
    }

    public int HFDrawFarmUIElements( nint unk1, nint unk2, nint unk3 ) {
        _watchMove_SLMInfo.Item1 = true;
        var ret = _hook_drawFarmUIElements!.OriginalFunction( unk1, unk2, unk3 );
        //Logger.Error( $"Drawing Farm UI: {unk1.ToString("X")} {unk2.ToString( "X" )} {unk3.ToString( "X" )} {ret}" );

 
        //Logger.Error( $"Drawing Farm UI: {unk1} {unk2} {unk3} {ret}" );
        _watchMove_SLMInfo = (false, false);

        return ret;
    }
    public int HFDrawTextToScreen ( uint x, ushort y, nint textPtr, int textParams, nint colorPtr ) {
       
        if ( _watchMove_SLMInfo.Item2 ) {
            //Logger.Error( $"Drawing Text MODIFIED: {x}, {y}, {textPtr}, {textParams}, {colorPtr}", Color.White );
            return _hook_drawTextToScreen!.OriginalFunction( x - 32, (ushort) (y + 12), textPtr, textParams, colorPtr );
        }
        //Logger.Error( $"Drawing Text: {x}, {y}, {textPtr}, {textParams}, {colorPtr}", Color.Orange );
        return _hook_drawTextToScreen!.OriginalFunction( x, y, textPtr, textParams, colorPtr );
    }

    private void HFDrawUIBoxes( nint dt0, nint dt1, int unk2, int unk3 ) {
        //Logger.Error( $"Drawing UI Boxes: {dt0}, {dt1}, {unk2}, {unk3}" );
        _hook_drawUIBoxes!.OriginalFunction( dt0, dt1, unk2, unk3 );
    }

    private void HFDrawMonsterInfoPage2 ( int unk1 ) {
        _watchMove_SLMInfo = (false, false);
        _hook_drawMonsterInfoPage2!.OriginalFunction( unk1 );
    }

    private void HFDrawMonsterInfoPage3 (int unk1 ) {
        _watchMove_SLMInfo = (false, false);

        if ( CheckAndInitialize(ref initialized_mp3) ) {

            // Row 3
            AddUIElement_StandardGrayBox( -140, -42, 64, 20 );
            AddUIElement_StandardGrayTranslucentBox( -76, -42, 84, 20 );

            AddUIElement_StandardGrayBox( 20, -42, 52, 20 );
            AddUIElement_StandardGrayTranslucentBox( 72, -42, 64, 20 );


            // Row 2
            AddUIElement_StandardGrayBox( -140, -22, 64, 20 );
            AddUIElement_StandardGrayTranslucentBox( -76, -22, 84, 20 );

            AddUIElement_StandardGrayBox( 20, -22, 52, 20 );
            AddUIElement_StandardGrayTranslucentBox( 72, -22, 64, 20 );


            // Row 3
            AddUIElement_StandardGrayBox( -140, -2, 64, 20 );
            AddUIElement_StandardGrayTranslucentBox( -76, -2, 84, 20 );

            AddUIElement_StandardGrayBox( 20, -2, 52, 20 );
            AddUIElement_StandardGrayTranslucentBox( 72, -2, 64, 20 );

        }

        textElements[ 3 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -107 , -40, 0 );
        textElements[ 4 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -34, -40, 0 );

        textElements[ 7 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -107, -20, 0 );
        textElements[ 8 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -34, -20, 0 );


        textElements[ 5 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 46, -40, 0 );
        textElements[ 6 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 105, -40, 0 );

        textElements[ 9 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 46, -20, 0 );
        textElements[ 10 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 105, -20, 0 );


        textElements[ 11 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -107, 0, 0 );
        textElements[ 12 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, -34, 0, 0 );

        textElements[ 13 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 46, 0, 0 );
        textElements[ 14 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 105, 0, 0 );


        _hook_drawMonsterInfoPage3!.OriginalFunction( unk1 );
    }

    private void HFDrawMonsterInfoPage4 ( int unk1 ) {
        //Logger.Error( $"Page 4 : {unk1}", Color.Green);
        

        if ( CheckAndInitialize( ref initialized_mp4 ) ) {

            // Row 3
            AddUIElement_StandardGrayDarkTranslucentBox( -8, 76, 96, 20 );
            AddUIElement_StandardGrayDarkTranslucentBox( 96, 76, 30, 20 );


        }

        textElements[ 15 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 26, 78, 0 );
        textElements[ 16 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, 111, 78, 0 );



        _hook_drawMonsterInfoPage4!.OriginalFunction( unk1 );
    }

    private void MonsterChanged ( IMonsterChange mon ) {
        monster = mon.Current;

        for ( var i = 0; i < boxElements.Count; i++ ) {
            if ( !boxElements[ i ].baseGame && boxElements[i].address != 0 ) {
                Marshal.FreeCoTaskMem( boxElements[ i ].address );
            }
        }

        if ( textElements.Count == 0 ) {
            textElements.Add( new MR2UITextElement( false, TextForLifespanHit(monster) ) );
            textElements.Add( new MR2UITextElement( false, "SFLi", 10, 10, 0xffffff ) );
            textElements.Add( new MR2UITextElement( false, "SFLi", 10, 10, 0x000000 ) );

            textElements.Add( new MR2UITextElement( false, $"Lifespan" ) );
            textElements.Add( new MR2UITextElement( false, TextForMonsterLifespan( monster ) ) );

            textElements.Add( new MR2UITextElement( false, $"L.Stage" ) );
            textElements.Add( new MR2UITextElement( false, TextForMonsterLStage( monster ) ) );

            textElements.Add( new MR2UITextElement( false, $"Growths" ) );
            textElements.Add( new MR2UITextElement( false, TextForGrowths( monster ) ) );

            textElements.Add( new MR2UITextElement( false, $"G.Pat" ) );
            textElements.Add( new MR2UITextElement( false, TextForGrowthPattern( monster ) ) );

            textElements.Add( new MR2UITextElement( false, $"Guts" ) );
            textElements.Add( new MR2UITextElement( false, TextForGrowthPattern( monster ) ) );

            textElements.Add( new MR2UITextElement( false, $"A.Speed" ) );
            textElements.Add( new MR2UITextElement( false, TextForGrowths( monster ) ) );

            textElements.Add( new MR2UITextElement( false, "Tech Uses" ) );
            textElements.Add( new MR2UITextElement( false, "0" ) ); // 16

            GetTechUses( monster );
        }

        else {
            textElements[ 0 ].UpdateText( TextForLifespanHit( monster ) );

            textElements[ 4 ].UpdateText( TextForMonsterLifespan( monster ) );
            textElements[ 6 ].UpdateText( TextForMonsterLStage( monster ) );
            
            textElements[ 8 ].UpdateText( TextForGrowths( monster ) );
            textElements[ 10 ].UpdateText( TextForGrowthPattern( monster ) );

            textElements[ 12 ].UpdateText( TextForGuts( monster ) );
            textElements[ 14 ].UpdateText( TextForArenaSpeed( monster ) );

            GetTechUses( monster );
        }

        foreach ( MR2UITextElement te in textElements ) {
            te.FreeMemory();
            te.AllocateMemory( _wrapfunc_parseTextWithCommandCodes );
        }
    }


    private string TextForMonsterLStage( IMonster monster ) {
        return $"{(byte) monster.LifeStage + 1}";
    }

    private string TextForMonsterLifespan( IMonster monster ) {
        return $"{monster.Lifespan} / {monster.InitalLifespan}";
    }

    private string TextForGrowthPattern( IMonster monster ) {
        switch (monster.LifeType) {
            case LifeType.Normal: return "Normal"; break;
            case LifeType.Precocious: return "Preco."; break;
            case LifeType.LateBloom: return "Late B."; break;
            case LifeType.Sustainable: return "Sustain"; break;
            case LifeType.Prodigy: return "Prodigy"; break;
            default: return "Error";
        }
    }

    private string TextForGrowths( IMonster monster ) {
        return $"{TextForGrowthSingleStat(monster.GrowthRateLife)} {TextForGrowthSingleStat( monster.GrowthRatePower )} " +
            $"{ TextForGrowthSingleStat( monster.GrowthRateIntelligence )} {TextForGrowthSingleStat( monster.GrowthRateSkill )} " +
            $"{ TextForGrowthSingleStat( monster.GrowthRateSpeed )} {TextForGrowthSingleStat( monster.GrowthRateDefense)}";
    }

    private string TextForGrowthSingleStat( byte growth ) {
        switch (growth) {
            case 0: return "E";
            case 1: return "D";
            case 2: return "C";
            case 3: return "B";
            case 4: return "A";
            default: return "X";
        }
    }

    private string TextForArenaSpeed ( IMonster monster ) {
        return TextForGrowthSingleStat( monster.ArenaSpeed );
    }

    private string TextForGuts( IMonster monster ) {
        double gps = 30.0 / (Math.Max( (byte) 1, monster.GutsRate) );
        return $"{monster.GutsRate} - {gps.ToString("0.#")}/s";
    }

    private string TextForLifespanHit ( IMonster monster ) {
        int lifeIndex = monster.Fatigue + ( monster.Stress * 2 );
        int lifeIndexHit = ( -1 * ( Math.Max(0, (int) Math.Ceiling((lifeIndex - 70.0)  / 35.0 )) ) );
        if ( lifeIndex >= 280 ) {
            lifeIndexHit = -7;
        }

        return $"{monster.Stress} {monster.Fatigue} {lifeIndexHit}";
    }

    private void GetTechUses ( IMonster monster ) {
        // TODO - MoveUseCount is never set up properly. Once that's fixed use it.
        byte[] raw = new byte[ 48 ];
        Memory.Instance.ReadRaw( _address_monster + 0x192, out raw, 48 );
        for ( var i = 0; i < 24; i++ ) {
            _techUses[ i ] = raw[ ( i * 2 ) + 1 ]; // Skip every other byte starting with 1
        }
            //for ( var i = 0; i < 24; i++ ) { 
            //    _techUses[ i ] = 0; 
            //}
            //monster.MoveUseCount.CopyTo( _techUses, 0 );
        }

    private void RestoreUiLinkedList ( nint self ) {
        Logger.Error( "RemoveSomeUIElements called.", Color.Red );
        nint CSysFarmPtrPtr = Marshal.ReadInt32( (nint) _address_game + 0x372308 );

        if ( CSysFarmPtrPtr != 0 ) {
            nint CSysFarmPtr = Marshal.ReadInt32( CSysFarmPtrPtr + 0x3C );
            byte isDisplayed = Marshal.ReadByte( CSysFarmPtr + 0x38 );

            if ( initialized == true && isDisplayed == 1 ) {
                RestoreUILinkedList();
            }
        }

        _removeUiHook!.OriginalFunction( self );
        
    }

    private void RestoreUILinkedList() {
        if ( boxesAdded.Count > 0 ) {
            rootBox.Next = boxesAdded.Last().Next;
        }

        if ( rootBoxPtr != 0 ) {
            Marshal.StructureToPtr( rootBox, rootBoxPtr, false );
        } rootBoxPtr = 0;

        boxesAdded = new List<Box>();

        for ( int i = 0; i < addressesAdded.Count; i++ ) {
            Marshal.FreeCoTaskMem( addressesAdded[ i ] );
        }

        addressesAdded = new List<nint>();

        initialized = false;
        initialized_mp3 = false;
        initialized_mp4 = false;
    }

    private void RebuildBoxList () {
        boxesAll = new List<Box>();

        rootBox = Marshal.PtrToStructure<Box>( rootBoxPtr );
        boxesAll.Add( rootBox );

        while ( boxesAll[boxesAll.Count - 1].Next != 0x00 ) {
            boxesAll.Add( Marshal.PtrToStructure<Box>( boxesAll[ boxesAll.Count - 1 ].Next ) );
        }

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

    private BoxAttribute SetupStandardUIBoxBackgroundBA ( ushort width, ushort height ) {
        BoxAttribute bAttr = new BoxAttribute();
        bAttr.Type = 5;
        bAttr.Width = width;
        bAttr.Height = height;
        bAttr.R = 128;
        bAttr.G = 128;
        bAttr.B = 128;
        bAttr.IsSemiTransparent = 0;
        return bAttr;
    }

    private BoxAttribute SetupStandardBlueBoxForegroundBA ( ushort width, ushort height ) {
        BoxAttribute bAttr = new BoxAttribute();
        bAttr.Type = 5;
        bAttr.Width = width;
        bAttr.Height = height;
        bAttr.R = 64;
        bAttr.G = 64;
        bAttr.B = 128;
        bAttr.IsSemiTransparent = 1;

        return bAttr;
    }

    private BoxAttribute SetupStandardGrayBoxForegroundBA ( ushort width, ushort height ) {
        BoxAttribute bAttr = new BoxAttribute();
        bAttr.Type = 5;
        bAttr.Width = width;
        bAttr.Height = height;
        bAttr.R = 64;
        bAttr.G = 64;
        bAttr.B = 64;
        bAttr.IsSemiTransparent = 0;

        return bAttr;
    }

    private BoxAttribute SetupStandardGrayDarkBoxBA ( ushort width, ushort height ) {
        BoxAttribute bAttr = new BoxAttribute();
        bAttr.Type = 5;
        bAttr.Width = width;
        bAttr.Height = height;
        bAttr.R = 32;
        bAttr.G = 32;
        bAttr.B = 32;
        bAttr.IsSemiTransparent = 0;

        return bAttr;
    }

    private BoxAttribute SetupBorderlessBlueBA ( ushort width, ushort height ) {
        BoxAttribute bAttr = new BoxAttribute();
        bAttr.Type = 1;
        bAttr.Width = width;
        bAttr.Height = height;
        bAttr.R = 64;
        bAttr.G = 64;
        bAttr.B = 128;
        bAttr.IsSemiTransparent = 1;

        return bAttr;
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

    private void AddUIElement_StandardBlueBox ( short x, short y, ushort width, ushort height ) {
        BoxAttribute backgroundAttr = SetupStandardUIBoxBackgroundBA( width, height );

        nint backgroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( backgroundAttr ) );
        Marshal.StructureToPtr( backgroundAttr, backgroundAttrPtr, false );

        addressesAdded = addressesAdded.Prepend( backgroundAttrPtr ).ToList();

        Box box = GetBox( x, y, 2, backgroundAttrPtr );

        nint boxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( box ) );

        PrependToBoxList( box, boxAddr );

        boxesAdded = boxesAdded.Prepend( box ).ToList();
        addressesAdded = addressesAdded.Prepend( boxAddr ).ToList();


        BoxAttribute foregroundAttr = SetupStandardBlueBoxForegroundBA( (ushort) ( width - 4 ), (ushort) ( height - 4 ) );

        nint foregroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( foregroundAttr ) );
        Marshal.StructureToPtr( foregroundAttr, foregroundAttrPtr, false );

        addressesAdded = addressesAdded.Prepend( foregroundAttrPtr ).ToList();

        Box foregroundBox = GetBox( (short) ( x + 2 ), (short) ( y + 2 ), 3, foregroundAttrPtr );

        nint foregroundBoxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( foregroundBox ) );

        PrependToBoxList( foregroundBox, foregroundBoxAddr );

        boxesAdded = boxesAdded.Prepend( foregroundBox ).ToList();
        addressesAdded = addressesAdded.Prepend( foregroundBoxAddr ).ToList();
    }

    private void AddUIElement_BorderlessBlueBox( short x, short y, ushort width, ushort height ) {
        BoxAttribute bAttr = SetupBorderlessBlueBA( width, height );
        nint attrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( bAttr ) );
        Marshal.StructureToPtr( bAttr, attrPtr, false );

        Box blBox = GetBox( x, y, 3, attrPtr );
        nint boxPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( blBox ) );
        PrependToBoxList( blBox, boxPtr );

        boxesAdded.Add( blBox );
        addressesAdded.Add( attrPtr );
        addressesAdded.Add( boxPtr );

        //boxesAdded = boxesAdded.Prepend( blBox ).ToList();
        //addressesAdded = addressesAdded.Prepend( attrPtr ).ToList();
        //addressesAdded = addressesAdded.Prepend( boxPtr ).ToList();
    }

    private void AddUIElement_StandardGrayBox ( short x, short y, ushort width, ushort height ) {
        BoxAttribute backgroundAttr = SetupStandardGrayBoxForegroundBA( width, height );
        nint backgroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( backgroundAttr ) );
        Marshal.StructureToPtr( backgroundAttr, backgroundAttrPtr, false );

        Box box = GetBox( x, y, 2, backgroundAttrPtr );
        nint boxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( box ) );
        PrependToBoxList( box, boxAddr );

        addressesAdded = addressesAdded.Prepend( backgroundAttrPtr ).ToList();
        boxesAdded = boxesAdded.Prepend( box ).ToList();
        addressesAdded = addressesAdded.Prepend( boxAddr ).ToList();
    }



    private void AddUIElement_StandardGrayTranslucentBox ( short x, short y, ushort width, ushort height ) {
        BoxAttribute backgroundAttr = SetupStandardGrayBoxForegroundBA( width, height );
        backgroundAttr.IsSemiTransparent = 1;
        nint backgroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( backgroundAttr ) );
        Marshal.StructureToPtr( backgroundAttr, backgroundAttrPtr, false );

        Box box = GetBox( x, y, 2, backgroundAttrPtr );
        nint boxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( box ) );
        PrependToBoxList( box, boxAddr );

        addressesAdded = addressesAdded.Prepend( backgroundAttrPtr ).ToList();
        boxesAdded = boxesAdded.Prepend( box ).ToList();
        addressesAdded = addressesAdded.Prepend( boxAddr ).ToList();
    }

    private void AddUIElement_StandardGrayDarkTranslucentBox ( short x, short y, ushort width, ushort height ) {
        BoxAttribute backgroundAttr = SetupStandardGrayDarkBoxBA( width, height );
        backgroundAttr.IsSemiTransparent = 1;
        nint backgroundAttrPtr = Marshal.AllocCoTaskMem( Marshal.SizeOf( backgroundAttr ) );
        Marshal.StructureToPtr( backgroundAttr, backgroundAttrPtr, false );

        Box box = GetBox( x, y, 2, backgroundAttrPtr );
        nint boxAddr = Marshal.AllocCoTaskMem( Marshal.SizeOf( box ) );
        PrependToBoxList( box, boxAddr );

        addressesAdded = addressesAdded.Prepend( backgroundAttrPtr ).ToList();
        boxesAdded = boxesAdded.Prepend( box ).ToList();
        addressesAdded = addressesAdded.Prepend( boxAddr ).ToList();
    }

    private void UpdateLoyaltyBoxes() {
        for ( var i = 0; i < boxesAll.Count; i++ ) {
            var addr = boxesAll[ i ].Next;
            if ( addr == 0 ) { break; }
            Memory.Instance.SafeWrite( addr + 0xC, boxesAll[ i + 1].XCopy - 32  );
            Memory.Instance.SafeWrite( addr + 0xE, boxesAll[ i + 1 ].YCopy + 12 );
            Memory.Instance.SafeWrite( addr + 0x10, boxesAll[ i + 1 ].XCopy - 32 );
            Memory.Instance.SafeWrite( addr + 0x12, boxesAll[ i + 1 ].YCopy + 12 );
        }

        AddUIElement_BorderlessBlueBox( 88, 90, 30, 8 );
        AddUIElement_StandardBlueBox( 86, 96, 66, 20 );
    }

    public bool CheckAndInitialize(ref bool _initialized) {
        if ( _initialized ) { return false; }

        // There can be up to 4 pointers stored in an array, where each element
        // will point to a linked list of UI elements to draw.
        // We're reading the array from behind as a hack to get around an issue
        // with the item shop, where if you back out of the item shop
        // the linked list that we're interested in would be stored at the back 
        // of the array because for a split second multiple linked list of UI
        // elements are rendered.

        var reinit = false;
        for ( int i = 3; i >= 0; i-- ) {
            var rbPtr = Marshal.ReadInt32( (nint) (_address_game + (nuint) (0x369900 + 4 * i)) );

            if ( rbPtr != 0 ) {
                if ( rbPtr != _rootBoxPtrPrev ) {
                    reinit = true;
                    RestoreUILinkedList();

                    rootBoxPtr = rbPtr;
                }
            }
        }

        if ( reinit ) {
            RebuildBoxList();
            _initialized = true;
            return true;
        }

        return false;
        
    }
    private void Init ( ref bool _initialized ) {
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

        RebuildBoxList();
        _initialized = true;
    }

    private void HFDrawStyle ( int unk1, int y ) {


        //Logger.Error( $"Style {unk1} {_watchMove_SLMInfo.Item1} {initialized}", Color.Yellow );
        if ( _watchMove_SLMInfo.Item1 ) {

            /*if ( _previousFarmStatus != 0 ) {
                _previousFarmStatus = 0;
                RestoreUILinkedList();
            }*/

            
            if ( CheckAndInitialize( ref initialized ) ) { 
                UpdateLoyaltyBoxes();
            }

            if ( textElements.Count > 0 && boxesAdded.Count > 0 ) {
                _watchMove_SLMInfo.Item2 = false;
                
                textElements[ 0 ].DrawTextWithPadding( _wrapfunc_drawTextWithPadding, (short) ( boxesAdded[ 0 ].X + 30 ), 98, 0 );

                textElements[ 1 ].DrawTextToScreen( _wrapfunc_drawTextToScreen, (uint) ( boxesAdded[ 1 ].X + 36 ), (ushort) ( boxesAdded[ 1 ].Y - 18 ) );

                textElements[ 2 ].DrawTextToScreen( _wrapfunc_drawTextToScreen, (uint) ( boxesAdded[ 1 ].X + 37 ), (ushort) ( boxesAdded[ 1 ].Y - 18 ) );
                textElements[ 2 ].DrawTextToScreen( _wrapfunc_drawTextToScreen, (uint) ( boxesAdded[ 1 ].X + 35 ), (ushort) ( boxesAdded[ 1 ].Y - 18 ) );
                textElements[ 2 ].DrawTextToScreen( _wrapfunc_drawTextToScreen, (uint) ( boxesAdded[ 1 ].X + 36 ), (ushort) ( boxesAdded[ 1 ].Y - 17 ) );
                textElements[ 2 ].DrawTextToScreen( _wrapfunc_drawTextToScreen, (uint) ( boxesAdded[ 1 ].X + 36 ), (ushort) ( boxesAdded[ 1 ].Y - 19 ) );

            }

            _watchMove_SLMInfo.Item2 = _watchMove_SLMInfo.Item1;
        }

        _hook_drawStyle!.OriginalFunction( unk1, y );
    }

    private int DrawLifeIndex ( nint unk1 ) {
        var ret = _hook_drawLoyalty!.OriginalFunction( unk1 );
        return ret;
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

public class MR2UIElementBox {
    public bool baseGame = true;

    public nint address;

    public Box box;
    public BoxAttribute attributes;

    public MR2UIElementBox( short x, short y, short z, BoxAttribute attribute ) {


        Box box = new Box();
        //box.Attribute = boxAttrPtr;
        box.Attribute = 0;
        box.X = x;
        box.Y = y;
        box.XCopy = box.X;
        box.YCopy = box.Y;
        box.Z = z;
        box.XOffset = 0;
        box.YOffset = 0;

        
    }
    public void WriteBoxMemory() {
        Marshal.StructureToPtr<Box>( box, address, false );
    }
}

public class MR2UITextElement {
    public bool baseGame = true;

    public nint address_text;
    public nint address_text_parsed;
    public nint address_attributes;

    public string text;
    public byte[] rawText;

    public ushort height;
    public ushort width;

    public int color;

    public MR2UITextElement(bool _base, string _text, ushort _height = 0x10, ushort _width = 0x0C, int _color = 0x7f7f7f) {
        baseGame = _base;
        text = _text;
        rawText = text.AsMr2().AsBytes();

        height = _height;
        width = _width;
        color = _color;
    }

    public void UpdateText(string _text) {
        text = _text;
        rawText = text.AsMr2().AsBytes();
    }

    public void FreeMemory() {
        if ( address_text == 0 ) { return; }

        Marshal.FreeCoTaskMem( address_text );
        Marshal.FreeCoTaskMem( address_text_parsed );
        Marshal.FreeCoTaskMem( address_attributes );
        address_text = 0;
    }

    public void AllocateMemory(ParseTextWithCommandCodes _func) {
        if ( address_text != 0 ) { return; }

        address_text = Marshal.AllocCoTaskMem( rawText.Length );
        Marshal.Copy( rawText, 0, address_text, rawText.Length );

        address_text_parsed = Marshal.AllocCoTaskMem( 1028 );
        _func!( address_text, address_text_parsed, 0 );

        address_attributes = Marshal.AllocCoTaskMem( 12 );
        Memory.Instance.Write( address_attributes, 0x00000000 );
        Memory.Instance.Write( address_attributes + 4, 0x00000000 );
        Memory.Instance.Write( address_attributes + 4, width);
        Memory.Instance.Write( address_attributes + 6, height );
        Memory.Instance.Write( address_attributes + 8, color );
    }

    public void DrawTextToScreen( DrawTextToScreenPORT _func, uint x, ushort y ) {
        if ( address_text == 0 ) { return; }
        _func!( x - 32, (ushort) ( y + 12 ), address_text_parsed, (int) address_attributes, address_attributes + 8 );
    }

    public void DrawTextWithPadding( DrawTextWithPadding _func, short x, short y, short padding ) {
        if ( address_text == 0 ) { return; }
        _func!( x, y, address_text, padding );
    }

}