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

namespace MRDX.Game.MoreMonsters;

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 83 EC 34 A1 ?? ?? ?? ?? 33 C4 89 44 24 ?? A1 ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_CombinationListGenerationStarted ( nuint self );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 81 EC D0 00 00 00 A1 ?? ?? ?? ?? 33 C5 89 45 ?? A1 ?? ?? ?? ?? 56" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_CombinationListGenerationFinished ( nuint self );

[HookDef( BaseGame.Mr2, Region.Us, "8B D1 80 BA ?? ?? ?? ?? FF" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_CombinationRenameFirstChoice ( nuint self );

[HookDef( BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 0C 53 8B D9 56 57 8B 83 ?? ?? ?? ??" )]
[Function( CallingConventions.Fastcall )]
public delegate void H_CombinationFinalStatsUpdate ( nuint unk1 );

class CombinationHandler {

    private Mod _mod;
    private readonly IHooks _iHooks;
    private IMonster _monsterCurrent;

    private IHook<H_CombinationRenameFirstChoice> _hook_combinationRenameFirstChoice;
    private IHook<H_CombinationListGenerationStarted> _hook_combinationListGenerationStarted;
    private IHook<H_CombinationListGenerationFinished> _hook_combinationListGenerationFinished;
    private IHook<H_CombinationFinalStatsUpdate> _hook_combinationFinalStatsUpdate;

    private nuint _combinationChosenMonsterAddress;
    private nuint _combinationListAddress;
    private uint _combinationColorVariant;

    private nuint _address_combination_secretseasoning { get { return Mod.address_game + 0x376430; } }
    private byte _secretSeasoning = 0xFF;
    private bool _overwriteCombinationLogic = false;

    private nuint _combinationParent1Address;
    private nuint _combinationParent2Address;

    private (byte, int)[] _combinationParent1Offsets;
    private (byte, int)[] _combinationParent2Offsets;

    private byte _combinationParent1Scaling;
    private byte _combinationParent2Scaling;


    public CombinationHandler ( Mod mod, IHooks iHooks, IMonster monster ) {
        _mod = mod;
        _iHooks = iHooks;
        _monsterCurrent = monster;

        _iHooks.AddHook<H_CombinationListGenerationStarted>( SetupCombinationListGenerationStarted ).ContinueWith( result => _hook_combinationListGenerationStarted = result.Result );
        _iHooks.AddHook<H_CombinationListGenerationFinished>( SetupCombinationListGenerationFinished ).ContinueWith( result => _hook_combinationListGenerationFinished = result.Result );
        _iHooks.AddHook<H_CombinationRenameFirstChoice>( SetupCombinationRenameFirstChoice ).ContinueWith( result => _hook_combinationRenameFirstChoice = result.Result );
        _iHooks.AddHook<H_CombinationFinalStatsUpdate>( SetupCombinationFinalStatsUpdate ).ContinueWith( result => _hook_combinationFinalStatsUpdate = result.Result );
    }

    private void SetupCombinationListGenerationStarted ( nuint self ) {
        _combinationListAddress = self + 0x94 - 0x8;
        Logger.Info( $"Generation Started: {self} | {_combinationListAddress}", Color.OrangeRed );
        _hook_combinationListGenerationStarted!.OriginalFunction( self );
    }

    private void SetupCombinationRenameFirstChoice ( nuint self ) {
        _combinationChosenMonsterAddress = self + 0x180;
        Logger.Info( $"Monster Combination Choice Started: {self} | {_combinationChosenMonsterAddress}", Color.OrangeRed );
        _hook_combinationRenameFirstChoice!.OriginalFunction( self );
    }


    private void SetupCombinationListGenerationFinished ( nuint self ) {
        Logger.Info( $"Generation Finished: {self} | {_combinationListAddress}", Color.OrangeRed );
        _hook_combinationListGenerationFinished!.OriginalFunction( self );

        var comboMonster = CombinationListGetSecretSeasoning();
        _overwriteCombinationLogic = !comboMonster.Item1;

        if ( _overwriteCombinationLogic ) { GenerateCombinationListStandard(); } 
    }

    /// <summary>
    /// This function checks the secret seasoning and both stores it in a variable, but also determines if
    /// the monster combination list needs to be overwritten, and with what monster.
    /// </summary>
    /// <returns></returns>
    private (bool, MonsterGenus, MonsterGenus) CombinationListGetSecretSeasoning() {
        Memory.Instance.Read( _address_combination_secretseasoning, out byte item );

        _secretSeasoning = item;

        if ( item == 79 ) { return (true, MonsterGenus.Pixie, MonsterGenus.XY); } // DNA Red - Mia
        else if ( item == 80 ) { return (true, MonsterGenus.Pixie, MonsterGenus.XZ); } // DNA Yellow - Poison
        else if ( item == 81 ) { return (true, MonsterGenus.Mocchi, MonsterGenus.XY); } // DNA Pink - GentleMoch
        else if ( item == 82 ) { return (true, MonsterGenus.Dragon, MonsterGenus.XX); } // DNA Gray - Moo
        else if ( item == 83 ) { return (true, MonsterGenus.Mocchi, MonsterGenus.YX); } // DNA White - WhiteMocchi
        else if ( item == 84 ) { return (true, MonsterGenus.Suezo, MonsterGenus.XZ); } // DNA Green - GoldSuezo
        else if ( item == 85 ) { return (true, MonsterGenus.Golem, MonsterGenus.XZ); } // DNA Black - DreamGolem

        else if ( item == 90 ) { return (true, MonsterGenus.Dragon, MonsterGenus.Dragon); } // Dragon Tusk
        else if ( item == 92 ) { return (true, MonsterGenus.Durahan, MonsterGenus.Durahan); } // Double-Edged
        else if ( item == 93 ) { return (true, MonsterGenus.Bajarl, MonsterGenus.Bajarl); } // Magic Pot
        else if ( item == 94 ) { return (true, MonsterGenus.Joker, MonsterGenus.Joker); } // Mask

        else if ( item == 96 ) { return (true, MonsterGenus.Jill, MonsterGenus.Jill); } // Big Boots
        else if ( item == 97 ) { return (true, MonsterGenus.Phoenix, MonsterGenus.Phoenix); } // Fire Feather
        else if ( item == 100 ) { return (true, MonsterGenus.Zilla, MonsterGenus.Zilla); } // Zilla Beard
        else if ( item == 103 ) { return (true, MonsterGenus.Ducken, MonsterGenus.Ducken); } // Quack Doll

        else if ( item == 170 ) { return (true, MonsterGenus.Undine, MonsterGenus.Undine); } // Undine Slate
        else if ( item == 173 ) { return (true, MonsterGenus.Ghost, MonsterGenus.Ghost); } // Stick
        else if ( item == 175 ) { return (true, MonsterGenus.Centaur, MonsterGenus.Centaur); } // Spear

        else return (false, 0, 0);

    }
    private void GenerateCombinationListStandard() {
        // Clear All Possibilities - 2E = Value 46 is the 'No combination byte'
        for ( var i = 0; i < 14; i++ ) {
            var cAddr = _combinationListAddress + ( (nuint) i * 12 );
            Memory.Instance.Write( cAddr, (byte) 0x2e );
            Memory.Instance.Write( cAddr + 0x4, (byte) 0x2e );
            Memory.Instance.Write( cAddr + 0x8, (byte) 0x2e );
        }

        Memory.Instance.Read( _combinationChosenMonsterAddress, out byte p1Freezer );
        Memory.Instance.Read( _combinationChosenMonsterAddress + 0x4, out byte p2Freezer );

        // 524 is the length of a single freezer entry.
        _combinationParent1Address = Mod.address_freezer + ( (nuint) p1Freezer * 524 );
        _combinationParent2Address = Mod.address_freezer + ( (nuint) p2Freezer * 524 );


        Memory.Instance.Read( _combinationParent1Address + 0x8, out MonsterGenus p1Main );
        Memory.Instance.Read( _combinationParent1Address + 0xc, out MonsterGenus p1Sub );

        Memory.Instance.Read( _combinationParent2Address + 0x8, out MonsterGenus p2Main );
        Memory.Instance.Read( _combinationParent2Address + 0xc, out MonsterGenus p2Sub );

        MonsterGenus[] parents = { p1Main, p1Main, p1Sub, p2Main, p2Main, p2Sub };
        Dictionary<MonsterBreed, int> comboResults = new Dictionary<MonsterBreed, int>();

        for ( var i = 0; i < 6; i++ ) {
            for ( var j = i; j < 6; j++ ) {
                MonsterBreed? breed = MonsterBreed.GetBreed( parents[ i ], parents[ j ] );

                if ( breed != null ) {
                    if ( comboResults.ContainsKey( breed ) ) {
                        comboResults[ breed ] = comboResults[ breed ] + 1;
                    }
                    else {
                        comboResults.Add( breed, 1 );
                    }
                }

                breed = MonsterBreed.GetBreed( parents[ j ], parents[ i ] );
                if ( breed != null ) {
                    if ( comboResults.ContainsKey( breed ) ) {
                        comboResults[ breed ] = comboResults[ breed ] + 1;
                    }
                    else {
                        comboResults.Add( breed, 1 );
                    }
                }
            }
        }

        // Write Combo List to Memory
        var comboSorted = comboResults.ToList();
        comboSorted.Sort( ( pair1, pair2 ) => pair2.Value.CompareTo( pair1.Value ) );

        for ( var i = 0; i < Math.Min( 14, comboSorted.Count ); i++ ) {
            var cAddr = _combinationListAddress + ( (nuint) i * 12 );
            Memory.Instance.Write( cAddr, (byte) comboSorted[ i ].Key.Main );
            Memory.Instance.Write( cAddr + 0x4, (byte) comboSorted[ i ].Key.Sub );
            Memory.Instance.Write( cAddr + 0x8, (byte) (byte) comboSorted[ i ].Value );
        }

        _combinationParent1Offsets = GetParentOffsetsInCombinationOrder( _combinationParent1Address );
        _combinationParent2Offsets = GetParentOffsetsInCombinationOrder( _combinationParent2Address );

        // 0x16D is the offset of the scaling value 
        Memory.Instance.Read( _combinationParent1Address + 0x16D, out _combinationParent1Scaling );
        Memory.Instance.Read( _combinationParent2Address + 0x16D, out _combinationParent2Scaling );
    }

    private void SetupCombinationFinalStatsUpdate ( nuint unk1 ) {
        Logger.Info( $"Overwriting Combination Monster Status {unk1}", Color.OrangeRed );
        _hook_combinationFinalStatsUpdate!.OriginalFunction( unk1 );

        var breed = MMBreed.GetBreed( _monsterCurrent.GenusMain, _monsterCurrent.GenusSub );
        if ( _overwriteCombinationLogic && breed != null ) {
            _mod.WriteMonsterData( breed._monsterVariants[0] );
            ApplyParentStatBonuses( breed._monsterVariants[0] );
            ApplySecretSeasoning();
        }

        if ( _mod._configuration.MonsterSizesEnabled ) {
            ApplyMonsterScaling();
        }
    }

    private void ApplyParentStatBonuses( MMBreedVariant childBreed ) {
        var statOrderP1 = _combinationParent1Offsets;
        var statOrderP2 = _combinationParent2Offsets;

        (byte, double)[] childGrowths = {
            (0, childBreed.GrowthRateLife), (1, childBreed.GrowthRatePower + 0.01),
        (2, childBreed.GrowthRateIntelligence + 0.02), (3, childBreed.GrowthRateSkill + 0.03),
        (4, childBreed.GrowthRateSpeed + 0.04), (5, childBreed.GrowthRateDefense)};

        childGrowths = childGrowths.OrderByDescending( x => x.Item2 ).ToArray();

        var correctOrderCount = 0;

        for ( var i = 0; i < 6; i++ ) { 
            if ( childGrowths[i].Item1 == statOrderP1[i].Item1 && childGrowths[ i ].Item1 == statOrderP2[i].Item1 ) {
                correctOrderCount++;
            }
        }

        // Get the percentages to use for the stat orders and number of matches. statPercentages is in stat order (0=Life), not combo order.
        double[] statPercentages = new double[ 6 ];
        for ( var i = 0; i < 6; i++ ) {
            if ( childGrowths[ i ].Item1 == statOrderP1[ i ].Item1 && childGrowths[ i ].Item1 == statOrderP2[ i ].Item1 ) {
                statPercentages[ childGrowths[ i ].Item1 ] = (byte) (
                    ( correctOrderCount == 6 ) ? 0.80 :
                    ( correctOrderCount == 4 ) ? 0.70 :
                    ( correctOrderCount == 3 ) ? 0.60 :
                    ( correctOrderCount == 2 ) ? 0.50 :
                    ( correctOrderCount == 1 ) ? 0.40 : 0.15 );
            }

            else { statPercentages[ i ] = 0.15; }
        }

        statOrderP1 = statOrderP1.OrderBy( x => x.Item1 ).ToArray();
        statOrderP2 = statOrderP2.OrderBy( x => x.Item1 ).ToArray();

        // TODO - Or not - Include the % of the monster's stat to include based on combination chance. Because I've completely overhauled
        // this, I kind of don't care. We'll just use 85% for all of them (Average of 2-7% chance).

        _monsterCurrent.Life +=         (ushort) ( ( ( ( 2 * statOrderP1[ 0 ].Item2 ) + statOrderP2[ 0 ].Item2 ) / 3 ) * 0.85 * statPercentages[0] );
        _monsterCurrent.Power +=        (ushort) ( ( ( ( 2 * statOrderP1[ 1 ].Item2 ) + statOrderP2[ 1 ].Item2 ) / 3 ) * 0.85 * statPercentages[ 1 ] );
        _monsterCurrent.Intelligence += (ushort) ( ( ( ( 2 * statOrderP1[ 2 ].Item2 ) + statOrderP2[ 2 ].Item2 ) / 3 ) * 0.85 * statPercentages[ 2 ] );
        _monsterCurrent.Skill +=        (ushort) ( ( ( ( 2 * statOrderP1[ 3 ].Item2 ) + statOrderP2[ 3 ].Item2 ) / 3 ) * 0.85 * statPercentages[ 3 ] );
        _monsterCurrent.Speed +=        (ushort) ( ( ( ( 2 * statOrderP1[ 4 ].Item2 ) + statOrderP2[ 4 ].Item2 ) / 3 ) * 0.85 * statPercentages[ 4 ] );
        _monsterCurrent.Defense +=      (ushort) ( ( ( ( 2 * statOrderP1[ 5 ].Item2 ) + statOrderP2[ 5 ].Item2 ) / 3 ) * 0.85 * statPercentages[ 5 ] );

    }

    private void ApplySecretSeasoning() {
        var ss = (Item) _secretSeasoning;

        if ( ss == Item.BigFootstep ) {
            _monsterCurrent.Life += 10;
            _monsterCurrent.Defense += 10;
        }

        else if ( ss == Item.CrabsClaw ) {
            _monsterCurrent.Skill += 50;
            _monsterCurrent.Defense += 50;
        }

        else if ( ss == Item.TaurusHorn ) {
            _monsterCurrent.NatureRaw += 25;
            _monsterCurrent.NatureBase += 25;
        }
    }
    /// <summary>
    /// Applies monster scaling based upon a semi-random weighting between the parents and a random mutation value.
    /// </summary>
    private void ApplyMonsterScaling() {
        var p1strength = _combinationParent1Scaling == 0 ? 0 : Random.Shared.Next( 0, 2 );
        var p2strength = _combinationParent2Scaling == 0 ? 0 : Random.Shared.Next( 0, 2 );
        var mustrength = Random.Shared.Next( 1, 201 );

        byte scaling = (byte) 
            ( ( (p1strength * _combinationParent1Scaling ) +  ( p2strength * _combinationParent2Scaling ) + mustrength ) 
                / ( 1 + p1strength + p2strength ) );
        Memory.Instance.Write( Mod.address_monster_mm_scaling, scaling );
    }

    /// <summary>
    /// This function does my best to recreate the formula and calcualtions for the parent stat weights.
    /// It returns an array of key pairs of the stat offset in order. Key = Stat ID, Value = Stat Offset
    /// </summary>
    /// <param name="parentAddress"></param>
    /// <returns></returns>
    private (byte, int)[] GetParentOffsetsInCombinationOrder(nuint parentAddress) {
        (byte, int)[] ret = new (byte, int)[ 6 ];

        Memory.Instance.Read( parentAddress + 0x8, out byte breedMain );
        Memory.Instance.Read( parentAddress + 0xc, out byte breedSub );

        MonsterBreed parentBreed = MonsterBreed.GetBreed( (MonsterGenus) breedMain, (MonsterGenus) breedSub );

        short[] baseStats = new short[ 6 ]; //
        short[] curStats = new short[ 6 ]; // Starts at 0x10
        byte[] growths = new byte[ 6 ]; // Starts at 0x30

        for ( var i = 0; i < 6; i++ ) {
            baseStats[i] = Byte.Parse( parentBreed.SDATAValues[ 7 + i ] );
            Memory.Instance.Read( parentAddress + 0x10 + (byte) ( i * 2 ), out curStats[ i ] );
            Memory.Instance.Read( parentAddress + 0x30 + (byte) i, out growths[ i ] );
        }

        for ( var i = 5; i >= 0; i-- ) {
            byte maxStat = (byte) i;
            var maxVal = -1.0;
            for ( var j = 5; j >= 0; j-- ) {
                var weightTotal = Math.Max( 1, ( Math.Max( 0, curStats[ j ] - baseStats[ j ] ) * ( 0.5 * growths[ j ] ) ) );
                if ( curStats[j] == 0 ) { weightTotal = -1; }
                if ( maxVal < weightTotal ) {
                    maxVal = weightTotal;
                    maxStat = (byte) j;
                }
            }

            ret[ 5 - i ] = (maxStat, Math.Max(0, curStats[ maxStat ] - baseStats[ maxStat ]) );

            curStats[ maxStat ] = 0;
        }

        return ret;
    }
}
