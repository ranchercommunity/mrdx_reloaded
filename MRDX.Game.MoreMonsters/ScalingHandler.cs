﻿using System;
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



class ScalingHandler {

    private Mod _mod;
    private readonly IHooks _iHooks;
    private IMonster _monsterCurrent;

    private IHook<UpdateGenericState> _hook_updateGenericState;
    private short _vertexScaling;

    public Dictionary<MonsterGenus, byte> MonsterScalingFactors = new Dictionary<MonsterGenus, byte>() 
    {   { MonsterGenus.Pixie, 50 },
        { MonsterGenus.Dragon, 160 },
        { MonsterGenus.Centaur, 125 },
        { MonsterGenus.ColorPandora, 120 },
        { MonsterGenus.Beaclon, 155 },
        { MonsterGenus.Henger, 100 },
        { MonsterGenus.Wracky, 20 },
        { MonsterGenus.Golem, 170 },
        { MonsterGenus.Zuum, 95 },
        { MonsterGenus.Durahan, 110 },
        { MonsterGenus.Arrowhead, 100 },
        { MonsterGenus.Tiger, 90 },
        { MonsterGenus.Hopper, 55 },
        { MonsterGenus.Hare, 80 },
        { MonsterGenus.Baku, 160 },
        { MonsterGenus.Gali, 85 },
        { MonsterGenus.Kato, 70 },
        { MonsterGenus.Zilla, 200 },
        { MonsterGenus.Bajarl, 10 },
        { MonsterGenus.Mew, 35 },
        { MonsterGenus.Phoenix, 110 },
        { MonsterGenus.Ghost, 80 },
        { MonsterGenus.Metalner, 105 },
        { MonsterGenus.Suezo, 95 },
        { MonsterGenus.Jill, 120 },
        { MonsterGenus.Mocchi, 80 },
        { MonsterGenus.Joker, 125 },
        { MonsterGenus.Gaboo, 100 },
        { MonsterGenus.Jell, 100 },
        { MonsterGenus.Undine, 100 },
        { MonsterGenus.Niton, 90 },
        { MonsterGenus.Mock, 120 },
        { MonsterGenus.Ducken, 85 },
        { MonsterGenus.Plant, 75 },
        { MonsterGenus.Monol, 130 },
        { MonsterGenus.Ape, 125 },
        { MonsterGenus.Worm, 120 },
        { MonsterGenus.Naga, 110 } };

    public byte temporaryScaling = 0; // Temporary Scaling must be manually set and reset
    public ScalingHandler ( Mod mod, IHooks iHooks, IMonster monster ) {
        _mod = mod;
        _iHooks = iHooks;
        _monsterCurrent = monster;

        _iHooks.AddHook<UpdateGenericState>( CheckVertexScalingUpdate ).ContinueWith( result => _hook_updateGenericState = result.Result );
    }

    /// <summary>
    /// Reads the location of the scaling factor in memory for monsters, then applies the configuration options to get a % scaling.
    /// This location in memory is by default 0, so set it to 1. Monster sizes should be from 1-201.
    /// </summary>
    /// <returns></returns>
    public double GetCurrentMonsterScalingFactor (byte monsterScaling) {
        //byte monsterScaling = _shrineMonsterScaling;
        if ( monsterScaling == 0 ) {
            Memory.Instance.Read( Mod.address_monster_mm_scaling, out monsterScaling );
        }

        if ( monsterScaling == 0 ) { return 1; }

        if ( monsterScaling <= 100 ) { return Single.Lerp( (float) _mod._configuration.MonsterSizeMinimum, (float) 1.0, ( ( (float) monsterScaling ) - 1 ) / 99 ); }
        else if ( monsterScaling == 101 ) { return 1.0; }
        else { return Single.Lerp( (float) 1.0, (float) _mod._configuration.MonsterSizeMaximum, ( ( (float) monsterScaling ) - 101 ) / 100 ); }
    }

    public double GetCurrentMonsterScalingFactor () {
        return GetCurrentMonsterScalingFactor( temporaryScaling );
    }
 
    /// <summary>
    /// Only activates if Monster Sizes is enabled in the configuration options.
    /// Updates the location of the monster's vertex scaling based upon the monster's vertex scaling value.
    /// Only updates these values if it detects a change from one value to reduce memory writes.
    /// </summary>
    /// <param name="parent"></param>
    private void CheckVertexScalingUpdate ( nint parent ) {
        _hook_updateGenericState!.OriginalFunction( parent );

        if ( !_mod._configuration.MonsterSizesEnabled ) { return; }

        Memory.Instance.Read( Mod.address_monster_vertex_scaling, out ushort vertexScalingA );

        if ( vertexScalingA != _vertexScaling ) {
            UpdateVertexScaling();
        }
    }

    public void UpdateVertexScaling() {
        Memory.Instance.Read( Mod.address_monster_vertex_scaling, out ushort vertexScalingA );
        if ( vertexScalingA != 0x00 ) {
            Memory.Instance.Read( Mod.address_monster_vertex_scaling + 0x2, out ushort vertexScalingB );
            Memory.Instance.Read( Mod.address_monster_vertex_scaling + 0x4, out ushort vertexScalingC );

            double scaling = GetCurrentMonsterScalingFactor();
            Memory.Instance.WriteRaw( Mod.address_monster_vertex_scaling, BitConverter.GetBytes( (ushort) ( vertexScalingA * scaling ) ) );
            Memory.Instance.WriteRaw( Mod.address_monster_vertex_scaling + 0x2, BitConverter.GetBytes( (ushort) ( vertexScalingB * scaling ) ) );
            Memory.Instance.WriteRaw( Mod.address_monster_vertex_scaling + 0x4, BitConverter.GetBytes( (ushort) ( vertexScalingC * scaling ) ) );
            _vertexScaling = (short) ( vertexScalingA * scaling );
        }
    }
}
