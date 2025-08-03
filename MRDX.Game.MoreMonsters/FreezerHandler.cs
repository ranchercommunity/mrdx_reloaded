using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;


namespace MRDX.Game.MoreMonsters;

// Called when transferring monster stats into the freezer.
[HookDef( BaseGame.Mr2, Region.Us, "56 33 D2 8B F1" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_FreezerWriteFreezer ( nuint self, int unk1, int unk2 );

// Called when transferring freezer stats into the monster. Also called when resetting a monster.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 8B 45 ?? 33 D2" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_FreezerWriteMonster ( nuint self, nuint unk1, int unk2 );

// Called when writing stats for a monster that's being unfrozen.
[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 8B 55 ?? 83 EC 10 53" )]
[Function( CallingConventions.MicrosoftThiscall )]
public delegate void H_FreezerWriteMonterStats ( int unk1, int unk2 );

public class FreezerHandler
{
    private Mod _mod;
    private readonly IHooks _iHooks;
    private IMonster _monsterCurrent;

    private IHook<H_FreezerWriteFreezer> _hook_freezerWriteFreezer;
    private IHook<H_FreezerWriteMonster> _hook_freezerWriteMonster;
    private IHook<H_FreezerWriteMonterStats> _hook_freezerWriteMonsterStats;
    private bool _freezerResetExtraMonsterData = false;

    public FreezerHandler ( Mod mod, IHooks iHooks, IMonster monster ) {
        _mod = mod;
        _iHooks = iHooks;
        _monsterCurrent = monster;

        _iHooks.AddHook<H_FreezerWriteFreezer>( FreezerWriteMonsterToFreezer ).ContinueWith( result => _hook_freezerWriteFreezer = result.Result );
        _iHooks.AddHook<H_FreezerWriteMonster>( FreezerFrozenMonsterClearMMBytes ).ContinueWith( result => _hook_freezerWriteMonster = result.Result );
        _iHooks.AddHook<H_FreezerWriteMonterStats>( FreezerMonsterCorrection ).ContinueWith( result => _hook_freezerWriteMonsterStats = result.Result );
    }
    private void FreezerWriteMonsterToFreezer ( nuint self, int freezerID, int unk2 ) {
        Logger.Warn( $"Freezer Writing:{self} {freezerID} {unk2}" );

        // Finds the freezer space that will be used.
        // This is similar logic the function itself uses. Except for checking for a mystery bit that I don't understand, check for the name.
        nuint freezerIDx = 0;
        for ( freezerIDx = 0; freezerIDx <= 19; freezerIDx++ ) {
            Memory.Instance.Read( Mod.address_freezer + 0x170 + ( (nuint) 524 * freezerIDx ), out byte openSlot );
            if ( openSlot == 0xFF ) { break; }
        }

        // This is where the magic happens for guts.
        Memory.Instance.WriteRaw( Mod.address_monster_mm_trueguts, [ _monsterCurrent.GutsRate ] );
        //_monsterCurrent.GutsRate = MonsterBreed.GetBreed( _monsterCurrent.GenusMain, _monsterCurrent.GenusMain ).GutsRate;

        // Write the monster's proper Subbreed
        Memory.Instance.WriteRaw( Mod.address_monster_mm_truesub, [ (byte) (_monsterCurrent.GenusSub + 1) ] );
        //_monsterCurrent.GenusSub = _monsterCurrent.GenusMain;


        _hook_freezerWriteFreezer!.OriginalFunction( self, freezerID, unk2 );


        // Read the monster's MM bytes and write them to the proper freezer space.
        byte[] unused = new byte[ Mod.unusedMonsterOffset ];
        
        Memory.Instance.ReadRaw( Mod.address_monster + nuint.Subtract(0x168, Mod.unusedMonsterOffset), out unused, Mod.unusedMonsterOffset ); // x168 is the offset of the name for the current monster.
        Memory.Instance.WriteRaw( Mod.address_freezer + nuint.Subtract(0x170, Mod.unusedMonsterOffset) + ( (nuint) 524 * (nuint) freezerIDx ), unused ); // x170 is the offset of names for monsters in the freezer.

        _freezerResetExtraMonsterData = true;

    }

    /// <summary>
    /// We hook this function only to see if we just froze a monster (FreezerWriteFreezer is called first), to determine if 
    /// we need to reset the extra monster data bytes, located in the bytes just prior to the Monster's name.
    /// The number of bytes is determined by Mod.unusedMonsterOffset. 
    /// </summary>
    /// <param name="self"></param>
    /// <param name="unk1"></param>
    /// <param name="unk2"></param>
    private void FreezerFrozenMonsterClearMMBytes ( nuint self, nuint unk1, int unk2 ) {

        _hook_freezerWriteMonster!.OriginalFunction( self, unk1, unk2 );

        if ( _freezerResetExtraMonsterData ) {
            byte[] unused = new byte[ Mod.unusedMonsterOffset ];
            Memory.Instance.WriteRaw( Mod.address_game + 0x37667C + (nuint) (0x168 - Mod.unusedMonsterOffset), unused );
        }

        _freezerResetExtraMonsterData = false;
    }

    /// <summary>
    /// After the monster's stats are set from the freezer, update the monster's guts to the MM value.
    /// Updates the monsters subbreed as well.
    /// Values of 0 represent pre-MM Freezes and can be ignored.
    /// Also update the scaling if applicable.
    /// </summary>
    /// <param name="unk1"></param>
    /// <param name="unk2"></param>
    private void FreezerMonsterCorrection ( int unk1, int unk2 ) {
        _hook_freezerWriteMonsterStats!.OriginalFunction( unk1, unk2 );

        //Memory.Instance.Read<byte>( Mod.address_monster_mm_trueguts, out byte trueGuts );
        //if ( trueGuts != 0 ) { _monsterCurrent.GutsRate = trueGuts; }

        //Memory.Instance.Read<byte>( Mod.address_monster_mm_truesub, out byte trueSub );
        //if ( trueSub != 0 ) { _monsterCurrent.GenusSub = (MonsterGenus) ( trueSub - 1 ); }

        if ( _mod._configuration.MonsterSizesEnabled ) {
            _mod.HandlerScaling.temporaryScaling = 0;
            _mod.HandlerScaling.UpdateVertexScaling(Mod.address_monster_vertex_scaling); }
    }
}
