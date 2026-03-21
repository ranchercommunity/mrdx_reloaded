using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reloaded.Memory.Sources;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using MRDX.Base.Mod.Interfaces;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections;
using System.Numerics;

namespace MRDX.Game.MoreMonsters;


public class VSHandler {

    public bool _vsModeEntered = false;
    private bool _vsModeActive = false;
    private int _vsMonsterSlot = 0;
    private nuint _address_loaded_file_monster_start { get { return Mod.address_game + 0x31EFBC; } }
    private nuint _address_vsmode_monster_info_one {  get { return Mod.address_game + 0x376678; } }
    private nuint _address_vsmode_monster_info_two { get { return Mod.address_game + 0x376448; } }


    private int _vsAlternateFixCounter = 0;

    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 83 EC 60" )]
    [Function( CallingConventions.Fastcall )]
    public delegate byte H_TitleScreenLoop ( nuint self, char unk1 );

    [ HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 18 53 56 57 A1 ?? ?? ?? ?? 33 C5 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 89 55 ??" )]
    [ Function( CallingConventions.Fastcall )]
    public delegate uint H_VSModeMonsterTemporaryStorage ( nuint self, int monsterSlot, int unk2 );

    [ HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 51 53 56 8B F1 C6 05 ?? ?? ?? ?? 01" )]
    [ Function( CallingConventions.MicrosoftThiscall )]
    public delegate void H_VSModeLoop (nuint self );

    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 3C A1 ?? ?? ?? ?? 33 C5 89 45 ?? 0F 28 05 ?? ?? ?? ??" )]
    [Function( CallingConventions.Fastcall )]
    public delegate int H_VSModeOptionFinalized ( nuint self );

    private Mod _mod;
    private readonly IHooks _iHooks;

    private IHook<H_TitleScreenLoop> _hook_titleScreenLoop;
    private IHook<H_VSModeMonsterTemporaryStorage> _hook_VSModeMonsterTemporaryStorage;
    private IHook<H_VSModeLoop> _hook_VSModeLoop;
    private IHook<H_VSModeOptionFinalized> _hook_VSModeOptionFinalized;



    public VSHandler ( Mod mod, IHooks iHooks ) {
        _mod = mod;
        _iHooks = iHooks;

        _iHooks.AddHook<H_TitleScreenLoop>( HF_TitleScreenLoop ).ContinueWith( result => _hook_titleScreenLoop = result.Result );
        _iHooks.AddHook<H_VSModeMonsterTemporaryStorage>( OverwriteMonsterValuesWithMMData ).ContinueWith( result => _hook_VSModeMonsterTemporaryStorage = result.Result );
        _iHooks.AddHook<H_VSModeLoop>( VSModeLoopFunction ).ContinueWith( result => _hook_VSModeLoop = result.Result );
        _iHooks.AddHook<H_VSModeOptionFinalized>( HF_VSModeOptionFinalized ).ContinueWith( result => _hook_VSModeOptionFinalized = result.Result );
    }

    private byte HF_TitleScreenLoop ( nuint self, char unk1 ) {
        Logger.Trace( $"Title Screen Loop Entered with {self} | {unk1} " );
        byte ret = _hook_titleScreenLoop!.OriginalFunction( self, unk1 );
        Logger.Trace( $"Title Screen Loop Exiting with {self} | {unk1} | returning {ret}" );

        _vsModeEntered = (ret == 2); // VS Mode Selected
        return ret;
    }

    private uint OverwriteMonsterValuesWithMMData( nuint self, int monsterSlot, int unk2 ) {

        _vsModeActive = true;
        _vsMonsterSlot = monsterSlot;

        uint ret = _hook_VSModeMonsterTemporaryStorage!.OriginalFunction( self, monsterSlot, unk2 );
        Logger.Info( $"Vs Mode Monster Data Overwrite Complete {self}, {monsterSlot}, {unk2}", Color.Magenta );
        _vsModeActive = false;

        return ret;
    }

    private void VSModeLoopFunction(nuint self) {
        byte guts = 0;
        if ( _vsModeActive ) {
            nuint subActual = 0x4;
            nuint gutsActual = 0x1CF;

            nuint addr = _vsMonsterSlot == 0 ? _address_vsmode_monster_info_one : _address_vsmode_monster_info_two;
            addr += 0x4; // Offsets are wrong.

            Memory.Instance.Read( addr + Mod.offset_mm_version, out ushort versionMM );
            Memory.Instance.Read( addr + Mod.offset_mm_truemain, out byte main );
            Memory.Instance.Read( addr + Mod.offset_mm_truesub, out byte sub );
            Memory.Instance.Read( addr + Mod.offset_mm_trueguts, out guts );

            // Illegal Version (Japanese Version of the game writes FFFF to the version area) - Do Nothing

            if ( versionMM >= 65534 ) { }

            // Version 0.5.0+ (1)
            else if ( versionMM >= 1 ) {
                Memory.Instance.Write( addr, main - 1 );
                Memory.Instance.Write( addr + subActual, sub - 1 );
                Memory.Instance.Write( addr + gutsActual, guts );

                // Set the scaling and alternate values for the opponent only.
                if ( _vsMonsterSlot == 1 ) {
                    Memory.Instance.Read( addr + Mod.offset_mm_alternate, out byte alt );
                    Memory.Instance.Read( addr + Mod.offset_mm_scaling, out byte scaling );
                    _mod._monsterInsideAlternate = alt;
                    _mod.handlerScaling.opponentScalingFactor = scaling;
                }
            }

            else if ( sub != 0 ) { // Do limited actions for Version 0 (0.4.4 or earlier)
                Memory.Instance.Write( addr + subActual, sub - 1 );
                Memory.Instance.Write( addr + gutsActual, guts );

                // Set the scaling and alternate values for the opponent only.
                if ( _vsMonsterSlot == 1 ) {
                    Memory.Instance.Read( addr + Mod.offset_mm_alternate, out byte alt );
                    Memory.Instance.Read( addr + Mod.offset_mm_scaling, out byte scaling );
                    _mod._monsterInsideAlternate = alt;
                    _mod.handlerScaling.opponentScalingFactor = scaling;
                }
            }

            // Reset Scaling Value for Opponent Only
            else if ( _vsMonsterSlot == 1 ) {
                _mod.handlerScaling.opponentScalingFactor = 0;
            }
        }

      
        _hook_VSModeLoop!.OriginalFunction( self );

        if ( _vsModeActive && guts != 0 ) {
            // This is a magic number hard coded in that roughly points to the battle pointers.
            nuint addressBPS = ( ( Mod.address_game + 0x1DE8A20 ) ); 
            Memory.Instance.Read<nuint>( addressBPS, out nuint addressBPS2 );
            Memory.Instance.Read<nuint>(addressBPS2+ 0x4 + (nuint) ( 0x4 * _vsMonsterSlot ), out nuint addressBPS3);
            Logger.Debug( $"Values {addressBPS} , {addressBPS2}, {addressBPS3}", Color.Magenta );
            Memory.Instance.SafeWrite( addressBPS3 + 0x34, ref guts );


            Logger.Info( $"VS Mode Monster Data Updated {_vsMonsterSlot}, {self}", Color.Magenta );
        }
    }

    private int HF_VSModeOptionFinalized ( nuint self ) {
        var ret = _hook_VSModeOptionFinalized!.OriginalFunction( self );
        Logger.Debug( $"VS Mode Option Finalized {self} returns {ret}", Color.Magenta );

        if ( ret != -1 ) {
            _vsAlternateFixCounter = 3;
        }

        return ret;
    }

    /// <summary>
    /// Used to count how many ID Checks are Run for VS Mode.
    /// At the beginning of match setup, three ID checks are run, twice on the first monster, and one for the second.
    /// This value will be used and returned to the main mod to determine if the monster alternate value needs to be checked for this value.
    /// </summary>
    /// <returns></returns>
    public bool GetVsModeOverrideAlternate () {
        if ( _vsAlternateFixCounter <= 0 ) { return false; }
        else { _vsAlternateFixCounter--;
            return _vsAlternateFixCounter == 0; // Returns True when this value is now 0.
        }
    }


    /// <summary>
    /// This function should be called when VS Mode is active and a save file was just loaded.
    /// This replaces 'illegal' MM data when the Monster is Version 0.5.0 or greater (1) with proper
    /// information.
    /// </summary>
    public void UpdateLoadedFileFreezerInformation () {

        // TODO : I need to fix the 0x8 and 0x4 additions at the end of the addresses.
        for ( var i = 0; i < 20; i++ ) {
            var startPos = _address_loaded_file_monster_start + (nuint) ( 524 * i );
            var verPosMM = startPos + Mod.offset_mm_version;
            var mainPosMM = startPos + Mod.offset_mm_truemain;
            var subPosMM = startPos + Mod.offset_mm_truesub;
            var gutsPosMM = startPos + Mod.offset_mm_trueguts;

            var mainPosActual = startPos;
            var subPosActual = startPos + 0x4;
            var gutsPosActual = startPos + 0x1CF;

            Memory.Instance.Read( verPosMM, out ushort versionMM );

            if ( versionMM >= 1 ) {
                Memory.Instance.Read( mainPosActual, out byte main );
                Memory.Instance.Read( subPosActual, out byte sub );

                // Only do data updates if we are writing a non-empty slot (0x2e) and an actual MM Breed.
                if ( main != 0x2e && MMBreed.GetBreed( (MonsterGenus) main, (MonsterGenus) sub ) != null ) {
                    byte guts = MonsterBreed.GetBreed( (MonsterGenus) main, (MonsterGenus) main ).GutsRate;
                    Memory.Instance.Write<Byte>( subPosActual, ref main );
                    Memory.Instance.Write<Byte>( gutsPosActual, ref guts );
                }
            }
        }
    }

}
