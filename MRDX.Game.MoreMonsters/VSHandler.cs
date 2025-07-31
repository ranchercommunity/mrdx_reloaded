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


class VSHandler {

    private bool _vsModeActive = false;
    private int _vsMonsterSlot = 0;
    private nuint _address_vsmode_monster_info_one {  get { return Mod.address_game + 0x376678; } }
    private nuint _address_vsmode_monster_info_two { get { return Mod.address_game + 0x376448; } }

    [ HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 18 53 56 57 A1 ?? ?? ?? ?? 33 C5 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 89 55 ??" )]
    [Function( CallingConventions.Fastcall )]
    public delegate uint H_VSModeMonsterTemporaryStorage ( nuint self, int monsterSlot, int unk2 );

    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 51 53 56 8B F1 C6 05 ?? ?? ?? ?? 01" )]
    [Function( CallingConventions.MicrosoftThiscall )]
    public delegate void H_VSModeLoop (nuint self );

    private Mod _mod;
    private readonly IHooks _iHooks;

    private IHook<H_VSModeMonsterTemporaryStorage> _hook_VSModeMonsterTemporaryStorage;
    private IHook<H_VSModeLoop> _hook_VSModeLoop;

    public VSHandler ( Mod mod, IHooks iHooks ) {
        _mod = mod;
        _iHooks = iHooks;

        _iHooks.AddHook<H_VSModeMonsterTemporaryStorage>( OverwriteMonsterValuesWithMMData ).ContinueWith( result => _hook_VSModeMonsterTemporaryStorage = result.Result );
        _iHooks.AddHook<H_VSModeLoop>( VSModeLoopFunction ).ContinueWith( result => _hook_VSModeLoop = result.Result );
    }

    private uint OverwriteMonsterValuesWithMMData( nuint self, int monsterSlot, int unk2 ) {

        _vsModeActive = true;
        _vsMonsterSlot = monsterSlot;

        uint ret = _hook_VSModeMonsterTemporaryStorage!.OriginalFunction( self, monsterSlot, unk2 );
        Logger.Info( $"Vs Mode Monster Date Overwrite Complete {self}, {monsterSlot}, {unk2}", Color.Aqua );
        _vsModeActive = false;

        return ret;
    }

    private void VSModeLoopFunction(nuint self) {
        byte guts = 0;
        if ( _vsModeActive ) {
            nuint subActual = 0x8;
            nuint gutsActual = 0x1D3;

            nuint subMM = 0x16A;
            nuint gutsMM = 0x16B;

            nuint addr = _vsMonsterSlot == 0 ? _address_vsmode_monster_info_one : _address_vsmode_monster_info_two;

            Memory.Instance.Read( addr + subMM, out byte sub );
            Memory.Instance.Read( addr + gutsMM, out guts );

            if ( sub != 0 ) {
                Memory.Instance.Write( addr + subActual, sub - 1 );
                Memory.Instance.Write( addr + gutsActual, guts );
            }
        }

      
        _hook_VSModeLoop!.OriginalFunction( self );

        if ( _vsModeActive && guts != 0 ) {
            // This is a magic number hard coded in that roughly points to the battle pointers.
            nuint addressBPS = ( ( Mod.address_game + 0x1DE8A20 ) ); 
            Memory.Instance.Read<nuint>( addressBPS, out nuint addressBPS2 );
            Memory.Instance.Read<nuint>(addressBPS2+ 0x4 + (nuint) ( 0x4 * _vsMonsterSlot ), out nuint addressBPS3);
            Logger.Info( $"Values {addressBPS} , {addressBPS2}, {addressBPS3}" );
            Memory.Instance.SafeWrite( addressBPS3 + 0x34, ref guts );


            Logger.Info( $"VS Mode Monster Data Updated {_vsMonsterSlot}, {self}", Color.Aqua );
        }
    }
    
}
