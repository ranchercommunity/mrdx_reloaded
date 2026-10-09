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
using static MRDX.Base.Mod.Interfaces.IGame;

namespace MRDX.Game.MoreMonsters;

public class WormHandler {

    // Called at the beginning of the Cocooning process. Determines which monster is used for the target, among other things.
    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 4C A1 ?? ?? ?? ?? 33 C5 89 45 ?? 53 56 57 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 8B F1" )]
    [Function( CallingConventions.MicrosoftThiscall )]
    public delegate nuint H_CModeHatch ( nuint unk0, nuint unk1, nuint vtable, int unk3 );

    // This function is called during the Worm Cocoon Animation/Cutscene (When it spins the cocoon at night).
    // Returns 0 while the cutscene is active, 1 when complete. 
    // During these function calls, the update to monster data occurs.
    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 83 EC 24 A1 ?? ?? ?? ?? 33 C4 89 44 24 ?? 53" )]
    [Function( CallingConventions.Cdecl )]
    public delegate nuint H_WormCocoonStart ( nuint unk1, nuint unk2, nuint unk3 );

    [HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 53 8B 5D ?? 57 8B F9" )]
    [Function( CallingConventions.MicrosoftThiscall )]
    public delegate void H_TechWriting ( nuint unk1, nuint unk2, char unk3 );


    private Mod _mod;
    private readonly IHooks _iHooks;
    private IGame _gameState;
    private IMonster _monsterCurrent;


    private IHook<H_WormCocoonStart> _hook_wormCocoonStart;
    private IHook<H_CModeHatch> _hook_cModeHatch;
    private IHook<H_TechWriting> _hook_techWriting;

    private bool _snapshotUpdate = false;
    private IMonster _monsterSnapshot;

    private MonsterGenus[] _cocoonStandardMains = [ MonsterGenus.Pixie, MonsterGenus.Hare, 
        MonsterGenus.Gali, MonsterGenus.Suezo, MonsterGenus.Jell, MonsterGenus.Monol, MonsterGenus.Naga ];

    private MonsterGenus[] _cocooonValidMains = [ MonsterGenus.Pixie, MonsterGenus.Henger, MonsterGenus.Golem, MonsterGenus.Zuum, 
        MonsterGenus.Arrowhead, MonsterGenus.Tiger, 
        MonsterGenus.Hare, MonsterGenus.Gali, MonsterGenus.Ghost, MonsterGenus.Suezo, 
        MonsterGenus.Jell, MonsterGenus.Mock, MonsterGenus.Ducken,
        MonsterGenus.Plant, MonsterGenus.Monol, MonsterGenus.Naga ];

    private MonsterGenus _cocoonMainBreed;
    private byte[] _updatedTechniques;

    public WormHandler ( Mod mod, IHooks iHooks, IGame game, IMonster monster ) {
        _mod = mod;
        _iHooks = iHooks;
        _gameState = game;
        _monsterCurrent = monster;

        _iHooks.AddHook<H_WormCocoonStart>( HF_WormCocoonStart ).ContinueWith( result => _hook_wormCocoonStart = result.Result );
        _iHooks.AddHook<H_CModeHatch>( HF_CModeHatch ).ContinueWith( result => _hook_cModeHatch = result.Result );
        _iHooks.AddHook<H_TechWriting>( HF_TechWriting ).ContinueWith( result => _hook_techWriting = result.Result );

        _gameState.OnMonsterChanged += MonsterChanged;
    }


    // This function is called during the generic state update. 
    // This is needed to notify that the sub update is ready and to update stats if appropriate.
    private bool _wormCocooningSubReady = true;
    private bool _wormStatUpdateReady = false;
    private bool _wormTechUpdateReady = false;
    public void CocooningGenericUpdate () {
        _wormCocooningSubReady = true;

        if ( _wormTechUpdateReady ) {
            _monsterCurrent.Life = _monsterSnapshot.Life;
            _monsterCurrent.Power = _monsterSnapshot.Power;
            _monsterCurrent.Intelligence = _monsterSnapshot.Intelligence;
            _monsterCurrent.Skill = _monsterSnapshot.Skill;
            _monsterCurrent.Speed = _monsterSnapshot.Speed;
            _monsterCurrent.Defense = _monsterSnapshot.Defense;
            Memory.Instance.WriteRaw( nuint.Add( Mod.address_monster, 0x192 ), _updatedTechniques );

            _wormTechUpdateReady = false;
        }
    }


    private void MonsterChanged ( IMonsterChange mon ) {
        if ( _snapshotUpdate ) {
            _monsterSnapshot = mon.Previous;
            _snapshotUpdate = false;
            UpdateCocoonStats();
        }
    }

    private void HF_TechWriting ( nuint unk1, nuint unk2, char unk3 ) {
        _hook_techWriting!.OriginalFunction( unk1, unk2, unk3 );

        if ( _wormStatUpdateReady ) {
            _wormTechUpdateReady = true;
            _wormStatUpdateReady = false;
        }
    }

    /// <summary>
    /// Sets the technique for the provided memory block. 
    /// Blocks are in the following format: [ ID0, ID0-USES, ID1, ID1-USES, ...]
    /// </summary>
    /// <param name="tech"></param>
    /// <param name="range"></param>
    /// <param name="id"></param>
    private void EnableTechnique( byte[] tech, int range, int id ) {
        tech[ ( ( range * 6 ) + id ) * 2 ] = 1;
    }

    private void UpdateCocoonStats() {
        if ( _cocoonStandardMains.Contains( _cocoonMainBreed ) || _cocoonMainBreed == MonsterGenus.Beaclon ) { return; } // Exits if the result is a standard monster

        var newTechniqueData = new byte[ 48 ];

        // Henger
        if ( _cocoonMainBreed == MonsterGenus.Henger ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Defense = (ushort) Math.Min( 999, _monsterSnapshot.Defense + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Bite - Punch
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }     // Somersault - Kick
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Somersaults - Heavy Chop

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 3 ); }      // Sting - Low Kick
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Tail Lash - Arm Cannon
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }      // Two Lashes - Napalm Shot
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 3 ); }      // Three Lashes - Burst Cannon
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }      // Roll Assault - Laser Sword
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Pierce Throw - Laser Cutter

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }     // Pinch Throw - Hammer Fall
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }     // Pierce - Yoyo
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Tusk Slash - Fist Missile
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 4 ); }     // Injection - Eye Beam

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }     // Poison Gas - Sound Wave
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 3 ); }     // Wheel Attack - Laser Swords

            _updatedTechniques = newTechniqueData;
        }

        // Golem
        if ( _cocoonMainBreed == MonsterGenus.Golem ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Power = (ushort) Math.Min( 999, _monsterSnapshot.Power + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { newTechniqueData[ 2 ] = 1; }      // Bite -> Kick
            if ( _updatedTechniques[ 2 ] != 0 ) { newTechniqueData[ 28 ] = 1; }     // Somersalt -> Charge
            if ( _updatedTechniques[ 4 ] != 0 ) { newTechniqueData[ 0 ] = 1; }      // Somersalts -> Heavy Punch

            if ( _updatedTechniques[ 12 ] != 0 ) { newTechniqueData[ 12 ] = 1; }      // Sting
            if ( _updatedTechniques[ 14 ] != 0 ) { newTechniqueData[ 6 ] = 1; }      // Tail Lash
            if ( _updatedTechniques[ 16 ] != 0 ) { newTechniqueData[ 24 ] = 1; }      // Two Lashes
            if ( _updatedTechniques[ 18 ] != 0 ) { newTechniqueData[ 46 ] = 1; }      // Three Lashes
            if ( _updatedTechniques[ 20 ] != 0 ) { newTechniqueData[ 30 ] = 1; }      // Roll Assault
            if ( _updatedTechniques[ 22 ] != 0 ) { newTechniqueData[ 8 ] = 1; }      // Pierce Throw

            if ( _updatedTechniques[ 24 ] != 0 ) { newTechniqueData[ 14 ] = 1; }     // Pinch Throw
            if ( _updatedTechniques[ 26 ] != 0 ) { newTechniqueData[ 10 ] = 1; }     // Pierce
            if ( _updatedTechniques[ 28 ] != 0 ) { newTechniqueData[ 18 ] = 1; }     // Tusk Slash
            if ( _updatedTechniques[ 30 ] != 0 ) { newTechniqueData[ 14 ] = 1; }     // Injection

            if ( _updatedTechniques[ 36 ] != 0 ) { newTechniqueData[ 38 ] = 1; }     // Poison Gas
            if ( _updatedTechniques[ 38 ] != 0 ) { newTechniqueData[ 42 ] = 1; }     // Wheel Attack

            _updatedTechniques = newTechniqueData;
        }

        // Zuum
        if ( _cocoonMainBreed == MonsterGenus.Zuum ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Power = (ushort) Math.Min( 999, _monsterSnapshot.Power + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { newTechniqueData[ 0 ] = 1; }      // Bite - Claw
            if ( _updatedTechniques[ 2 ] != 0 ) { newTechniqueData[ 14 ] = 1; }     // Somersalt - Tail Lash
            if ( _updatedTechniques[ 4 ] != 0 ) { newTechniqueData[ 22 ] = 1; }      // Somersalts - Tail Combo

            if ( _updatedTechniques[ 12 ] != 0 ) { newTechniqueData[ 12 ] = 1; }      // Sting - Tail
            if ( _updatedTechniques[ 14 ] != 0 ) { newTechniqueData[ 24 ] = 1; }      // Tail Lash - Jumping Claw
            if ( _updatedTechniques[ 16 ] != 0 ) { newTechniqueData[ 26 ] = 1; }      // Two Lashes - Diving Claw
            if ( _updatedTechniques[ 18 ] != 0 ) { newTechniqueData[ 28 ] = 1; }      // Three Lashes - Aerial Claw
            if ( _updatedTechniques[ 20 ] != 0 ) { newTechniqueData[ 40 ] = 1; }      // Roll Assault - Roll Assault
            if ( _updatedTechniques[ 22 ] != 0 ) { newTechniqueData[ 4 ] = 1; }      // Pierce Throw - Bite

            if ( _updatedTechniques[ 24 ] != 0 ) { newTechniqueData[ 30 ] = 1; }     // Pinch Throw - Fireball
            if ( _updatedTechniques[ 26 ] != 0 ) { newTechniqueData[ 20 ] = 1; }     // Pierce - Hypotism
            if ( _updatedTechniques[ 28 ] != 0 ) { newTechniqueData[ 36 ] = 1; }     // Tusk Slash - Charge
            if ( _updatedTechniques[ 30 ] != 0 ) { newTechniqueData[ 38 ] = 1; }     // Injection - Fire Charge

            if ( _updatedTechniques[ 36 ] != 0 ) { newTechniqueData[ 18 ] = 1; }     // Poison Gas - Dust Cloud
            if ( _updatedTechniques[ 38 ] != 0 ) { newTechniqueData[ 42 ] = 1; }     // Wheel Attack - Five Balls

            _updatedTechniques = newTechniqueData;
        }

        // Arrow Head
        if ( _cocoonMainBreed == MonsterGenus.Arrowhead ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Defense = (ushort) Math.Min( 999, _monsterSnapshot.Defense + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Bite - Punch
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }     // Somersalt - Hidden Sting
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Somersaults - Long Punch

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Sting - Claw Assault
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }      // Tail Lash - Sting
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }      // Two Lashes - Triple Stings
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 3 ); }      // Three Lashes - Acrobatics
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Roll Assault - Bloodsuction
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 3 ); }      // Pierce Throw - Tail Swing

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }     // Pinch Throw - Claw Pinch
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Pierce - Energy Shot
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }     // Tusk Slash - Jumping Claw
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 3 ); }     // Injection - Javeling

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 2 ); }     // Poison Gas - Energy Shots
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 4 ); }     // Wheel Attack - Meteor

            _updatedTechniques = newTechniqueData;
        }

        // Tiger
        if ( _cocoonMainBreed == MonsterGenus.Tiger ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Skill = (ushort) Math.Min( 999, _monsterSnapshot.Skill + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Bite - Bite
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }     // Somersalt - Charge
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }      // Somersalts - Roll Assault

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Sting - Scratch
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }      // Tail Lash - Roll Assault
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Two Lashes - Charge
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 3 ); }      // Three Lashes - Stab
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 2 ); }      // Roll Assault - Roar
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }      // Pierce Throw - One-Two

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }     // Pinch Throw - Combination
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }     // Pierce - Bolt
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Tusk Slash - Ice Bomb
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }     // Injection - Blizzard

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }     // Poison Gas - Lightning
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 2 ); }     // Wheel Attack - Roar

            _updatedTechniques = newTechniqueData;
        }

        // Ghost
        if ( _cocoonMainBreed == MonsterGenus.Ghost ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Intelligence = (ushort) Math.Min( 999, _monsterSnapshot.Intelligence + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }      // Bite - Charge
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }     // Somersalt - Energy Shot
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Somersalts - Necromancy

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Sting - Toy Hammer
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Tail Lash - Energy Shot
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Two Lashes - Necromancy
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Three Lashes - Uppercut
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Roll Assault - Uppercut
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }      // Pierce Throw - Dove Bomb

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }     // Pinch Throw - Pigeon Bomb
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }     // Pierce - Surprise
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }     // Tusk Slash - Magic Card
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Injection - Magic Cards

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }     // Poison Gas - Astonishment
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 3 ); }     // Wheel Attack - Combination

            _updatedTechniques = newTechniqueData;
        }

        // Mock
        if ( _cocoonMainBreed == MonsterGenus.Mock ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Intelligence = (ushort) Math.Min( 999, _monsterSnapshot.Intelligence + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { }      // Bite - Nothing!
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }     // Somersalt - Head Butt
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Somersalts - Leaf Cutter

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Sting - Leaf Gun
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }      // Tail Lash - Leaf Gun
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Two Lashes  - Leaf Cutter
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 2 ); }      // Three Lashes - Twisters
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }      // Roll Assault - Energy Steal
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }      // Pierce Throw - Leaf Gatling

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }     // Pinch Throw - Leaf Gatling
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }     // Pierce - Twig Gun
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }     // Tusk Slash - Leaf Cutter
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Injection - Twister

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 3 ); }     // Poison Gas - Twig Gatling
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 2 ); }     // Wheel Attack - Twisters

            _updatedTechniques = newTechniqueData;
        }

        // Ducken
        if ( _cocoonMainBreed == MonsterGenus.Ducken ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Speed = (ushort) Math.Min( 999, _monsterSnapshot.Speed + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Bite - Flutter Slap
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }     // Somersalt - Bound
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 3 ); }      // Somersalts - Bound Charge

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Sting - Beak Thrust
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }      // Tail Lash - Eye Beam
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }      // Two Lashes - Beam Shower
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 3 ); }      // Three Lashes - Maximal Beam
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 4 ); }      // Roll Assault - Frantic Beam
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }      // Pierce Throw - Missile

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 4 ); }     // Pinch Throw - Bombing
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }     // Pierce - Ducken Dance
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 3 ); }     // Tusk Slash - Falling Beak
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 5 ); }     // Injection - Boomerang

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }     // Poison Gas - Surprise
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }     // Wheel Attack - Explosion

            _updatedTechniques = newTechniqueData;
        }

        // Plant
        if ( _cocoonMainBreed == MonsterGenus.Plant ) {
            _monsterSnapshot.GenusMain = _monsterCurrent.GenusMain;
            _monsterSnapshot.GenusSub = _monsterCurrent.GenusSub;
            _monsterSnapshot.Life = (ushort) Math.Min( 999, _monsterSnapshot.Life + 30 );

            if ( _updatedTechniques[ 0 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 0 ); }      // Bite - Slap
            if ( _updatedTechniques[ 2 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }     // Somersalt - Root Attack
            if ( _updatedTechniques[ 4 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Somersalts - Root Combo

            if ( _updatedTechniques[ 12 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 0 ); }      // Sting - Jab
            if ( _updatedTechniques[ 14 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 1 ); }      // Tail Lash - Root Attack
            if ( _updatedTechniques[ 16 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 2 ); }      // Two Lashes - Root Combo
            if ( _updatedTechniques[ 18 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 2 ); }      // Three Lashes - Face Drill
            if ( _updatedTechniques[ 20 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 3 ); }      // Roll Assault - Life Steal
            if ( _updatedTechniques[ 22 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 1 ); }      // Pierce Throw - Jab Combo

            if ( _updatedTechniques[ 24 ] != 0 ) { EnableTechnique( newTechniqueData, 1, 2 ); }     // Pinch Throw - Plant Combo
            if ( _updatedTechniques[ 26 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 0 ); }     // Pierce - Toxic Nectar
            if ( _updatedTechniques[ 28 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 0 ); }     // Tusk Slash - Seed Gun
            if ( _updatedTechniques[ 30 ] != 0 ) { EnableTechnique( newTechniqueData, 3, 1 ); }     // Injection - Seed Gatling

            if ( _updatedTechniques[ 36 ] != 0 ) { EnableTechnique( newTechniqueData, 2, 1 ); }     // Poison Gas - Toxic Pollen
            if ( _updatedTechniques[ 38 ] != 0 ) { EnableTechnique( newTechniqueData, 0, 3 ); }     // Wheel Attack - Life Steal

            _updatedTechniques = newTechniqueData;
        }

        _wormStatUpdateReady = true;

    }

    /// <summary>
    /// This function is called every frame during the Cocooning Night animation.
    /// This function returns 1 if the animation is still playing, and 0 once it is complete.
    /// 
    /// </summary>
    /// <param name="self"></param>
    /// <param name="unk1"></param>
    /// <param name="unk2"></param>
    /// <param name="unk3ptr"></param>
    ///
    public nuint HF_WormCocoonStart ( nuint unk1, nuint unk2, nuint unk3 ) {

        if ( _wormCocooningSubReady ) {
            Logger.Debug( $"Worm Cocoon with {unk1.ToString( "X" )}, {unk2.ToString( "X" )}, {unk3.ToString( "X" )}", Color.WhiteSmoke );
            _snapshotUpdate = true;
        }

        byte wormSub = ( (byte) _monsterCurrent.GenusSub );
        byte wormSubMM = (byte) ( _monsterCurrent.GenusSub + 1 );

        nuint ret = _hook_wormCocoonStart!.OriginalFunction( unk1, unk2, unk3 );
        if ( _wormCocooningSubReady && ret == 1 ) {
            Memory.Instance.Write<byte>( Mod.address_monster_mm_wormsub, ref wormSubMM );
            Logger.Debug( $"Worm Sub : {ret} {wormSubMM} written to MM Data.", Color.WhiteSmoke );
            _wormCocooningSubReady = false;
        }

        else if ( ret == 0 ) {
            Logger.Debug( $"Worm Cocooning Ending", Color.WhiteSmoke );
            _wormCocooningSubReady = true;
        }

        return ret;
    }


    // This function handles updating the Worm Cocoon Sub to the new breed if and when appropriate.
    public nuint HF_CModeHatch ( nuint unk0, nuint unk1, nuint vtable, int unk3 ) {
        Logger.Debug( $"Worm Cocoon Detected {unk0.ToString( "X" )}, {unk1.ToString( "X" )}, {vtable.ToString( "X" )}, {unk3}", Color.WhiteSmoke );

        _updatedTechniques = new byte[ 48 ];
        var techAddr = nuint.Add( Mod.address_monster, 0x192 );
        Memory.Instance.ReadRaw( techAddr, out _updatedTechniques, 48 );

        var ret = _hook_cModeHatch!.OriginalFunction( unk0, unk1, vtable, unk3 );

        var cocoonMainAddr = nuint.Add( unk0, 0x80 );
        var cocoonSubAddr = nuint.Add( cocoonMainAddr, 4 );
        byte wormSub = ( (byte) _monsterCurrent.GenusSub );

        Memory.Instance.Read( cocoonMainAddr, out byte cocoonMainBreed );
        _cocoonMainBreed = (MonsterGenus) cocoonMainBreed;


        if ( _cocoonMainBreed == MonsterGenus.Beaclon ) { } // Do nothing for the main breed if the standard cocoon is for Beaclon

        else if ( _mod._configuration.WormCocoonSpeciesManual && _mod._configuration.WormCocoonBreedMain != Configuration.Config.ValidWormMains.Random ) {
            _cocoonMainBreed = (MonsterGenus) _mod._configuration.WormCocoonBreedMain;
        }

        else if ( _cocoonStandardMains.Contains( _cocoonMainBreed ) ) {
            MonsterGenus newBreed = _cocooonValidMains[ Random.Shared.Next( _cocooonValidMains.Length ) ];
            if ( _cocoonStandardMains.Contains( newBreed ) ) { } // Do nothing if the game rolled a standard breed for both internal and base game random
            else { _cocoonMainBreed = newBreed; }

        }

        byte cmb = (byte) _cocoonMainBreed;
        Memory.Instance.Write<byte>( cocoonMainAddr, ref cmb ); // Writes main breed to memory


        if ( _mod._configuration.WormCocoonSubOverwrite ) {
            Logger.Debug( $"Worm Cocooning - Checking for New Sub : {_cocoonMainBreed} {wormSub}", Color.WhiteSmoke );
            if ( MMBreed.GetBreed( _cocoonMainBreed, (MonsterGenus) wormSub ) != null || MonsterBreed.GetBreed( _cocoonMainBreed, (MonsterGenus) wormSub ) != null ) {
                Memory.Instance.Write<byte>( cocoonSubAddr, ref wormSub );
                Logger.Debug( $"Special Worm Cocoon : {wormSub} is the sub now instead.", Color.WhiteSmoke );
            }
        }

        else {
            Logger.Debug( $"Worm Cocooning - Sub Overwrite Option Disabled : {cocoonMainBreed} {wormSub}", Color.WhiteSmoke );
        }


        return ret;
    }
}
