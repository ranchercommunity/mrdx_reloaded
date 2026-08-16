using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Memory.Sources;


namespace MRDX.Game.MoreMonsters;

public class VersionUpdateHandler {

    private Mod _mod;
    private readonly IHooks _iHooks;
    private IMonster _monsterCurrent;

    private static byte[] playTypePerMain = [ 0, 0, 0, 1, 0, 0, 2, 2, 2, 2, 0, 0, 2, 2, 1, 0, 0, 0, 2, 0, 0, 1, 2, 1, 0, 0, 0, 1, 0, 0, 2, 0, 1, 0, 1, 0, 0, 0 ]; // Thanks Teawch
    

    public VersionUpdateHandler ( Mod mod, IHooks iHooks, IMonster monster ) {
        _mod = mod;
        _iHooks = iHooks;
        _monsterCurrent = monster;
    }

    public void VersionUpdateFreezerMonster_V0x5x0_MM1 ( byte freezerSlot ) {

        var startPos = Mod.address_freezer + (nuint) ( 524 * freezerSlot );
        var mainPosActual = startPos + 0x4 + 0x4;
        var subPosActual = startPos + 0x8 + 0x4;
        var gutsPosActual = startPos + 0x1D3 + 0x4;

        Memory.Instance.Read( mainPosActual, out byte mainA );
        Memory.Instance.Read( subPosActual, out byte subA );
        Memory.Instance.Read( gutsPosActual, out byte gutsA );

        var verPosMM = startPos + Mod.offset_mm_version + 0x8;
        var mainPosMM = startPos + Mod.offset_mm_truemain + 0x8;
        var subPosMM = startPos + Mod.offset_mm_truesub + 0x8;
        var gutsPosMM = startPos + Mod.offset_mm_trueguts + 0x8;
        var wormPosMM = startPos + Mod.offset_mm_wormsub + 0x8;

        byte mmMain = (byte) ( mainA + 1 );
        byte mmSub = (byte) ( subA + 1 );

        // Update an MM Monster to the proper Version, Main, Sub, and GR
        if ( MMBreed.GetBreed( (MonsterGenus) mainA, (MonsterGenus) subA ) != null ) {
            Memory.Instance.Write<short>( verPosMM, ref Mod.memory_mm_version );
            Memory.Instance.Write<Byte>( mainPosMM, ref mmMain );
        }

        // Update standard breed monsters to the proper version, Main, Sub, and GR
        else {
            Memory.Instance.Write<short>( verPosMM, ref Mod.memory_mm_version );
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

    public void VersionUpdateFreezerMonster_V0x6x0_MM4 ( byte freezerSlot ) {

        var startPos = Mod.address_freezer + (nuint) ( 524 * freezerSlot );
        var mainPosActual = startPos + 0x4 + 0x4;
        var playPosActual = startPos + 0xE5 + 0x8;
        var verPosMM = startPos + Mod.offset_mm_version + 0x8;

        Memory.Instance.Read( mainPosActual, out byte mainActual );

        // Update the monster to have the proper Play Type
        Memory.Instance.Write<Byte>( playPosActual, ref playTypePerMain[ mainActual ] );

        Memory.Instance.Write<short>( verPosMM, ref Mod.memory_mm_version );
        Logger.Info( $"Monster in Freezer Slot {freezerSlot} updated to Version 0.6.0 Standards (v4)." );
    }
};
