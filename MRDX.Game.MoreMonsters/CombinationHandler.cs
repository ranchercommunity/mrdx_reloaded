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
    private Item _secretSeasoning = (Item) 0xFF;
    private bool _overwriteCombinationLogic = false;
    private List<KeyValuePair<MonsterBreed, int>> _combinationPotentialResults;

    private nuint _combinationParent1Address;
    private nuint _combinationParent2Address;

    private (byte, int)[] _combinationParent1Offsets;
    private (byte, int)[] _combinationParent2Offsets;

    private byte _combinationParent1Scaling;
    private byte _combinationParent2Scaling;

    private int[] _breedCombinationStrength = [ 10, 7, 4, 1, 4, 4, 5, 10, 9, 5,
                                                3, 6, 6, 6, 4, 6, 2, 2, 1, 2,
                                                1, 1, 1, 6, 3, 4, 3, 2, 6, 1,
                                                4, 1, 1, 6, 6, 6, 6];
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

        if ( _overwriteCombinationLogic ) { 
            ClearCombinationList(); 

            if ( _mod._configuration.CombinationChanceAdjustment == Configuration.Config.CombinaitonSettings.Modified ) {
                GenerateCombinationListModified();
            } else {
                GenerateCombinationListBaseGame();
            }
        } 
    }

    /// <summary>
    /// This function checks the secret seasoning and both stores it in a variable, but also determines if
    /// the monster combination list needs to be overwritten, and with what monster.
    /// </summary>
    /// <returns></returns>
    private (bool, MonsterGenus, MonsterGenus) CombinationListGetSecretSeasoning() {
        Memory.Instance.Read( _address_combination_secretseasoning, out byte item );

        _secretSeasoning = (Item) item;
 
        if ( _secretSeasoning == Item.DnaCapsuleRed ) { return (true, MonsterGenus.Pixie, MonsterGenus.XY); } // DNA Red - Mia
        else if ( _secretSeasoning == Item.DnaCapsuleYellow ) { return (true, MonsterGenus.Pixie, MonsterGenus.XZ); } // DNA Yellow - Poison
        else if ( _secretSeasoning == Item.DnaCapsulePink ) { return (true, MonsterGenus.Mocchi, MonsterGenus.XY); } // DNA Pink - GentleMoch
        else if ( _secretSeasoning == Item.DnaCapsuleGray ) { return (true, MonsterGenus.Dragon, MonsterGenus.XX); } // DNA Gray - Moo
        else if ( _secretSeasoning == Item.DnaCapsuleWhite ) { return (true, MonsterGenus.Mocchi, MonsterGenus.YX); } // DNA White - WhiteMocchi
        else if ( _secretSeasoning == Item.DnaCapsuleGreen ) { return (true, MonsterGenus.Suezo, MonsterGenus.XZ); } // DNA Green - GoldSuezo
        else if ( _secretSeasoning == Item.DnaCapsuleBlack ) { return (true, MonsterGenus.Golem, MonsterGenus.XZ); } // DNA Black - DreamGolem

        else if ( _secretSeasoning == Item.DragonTusk ) { return (true, MonsterGenus.Dragon, MonsterGenus.Dragon); } // Dragon Tusk
        else if ( _secretSeasoning == Item.DoubleEdged ) { return (true, MonsterGenus.Durahan, MonsterGenus.Durahan); } // Double-Edged
        else if ( _secretSeasoning == Item.MagicPot ) { return (true, MonsterGenus.Bajarl, MonsterGenus.Bajarl); } // Magic Pot
        else if ( _secretSeasoning == Item.Mask ) { return (true, MonsterGenus.Joker, MonsterGenus.Joker); } // Mask

        else if ( _secretSeasoning == Item.BigBoots ) { return (true, MonsterGenus.Jill, MonsterGenus.Jill); } // Big Boots
        else if ( _secretSeasoning == Item.FireFeather ) { return (true, MonsterGenus.Phoenix, MonsterGenus.Phoenix); } // Fire Feather
        else if ( _secretSeasoning == Item.ZillaBeard ) { return (true, MonsterGenus.Zilla, MonsterGenus.Zilla); } // Zilla Beard
        else if ( _secretSeasoning == Item.QuackDoll2 ) { return (true, MonsterGenus.Ducken, MonsterGenus.Ducken); } // Quack Doll

        else if ( _secretSeasoning == Item.UndineSlate ) { return (true, MonsterGenus.Undine, MonsterGenus.Undine); } // Undine Slate
        else if ( _secretSeasoning == Item.StickGreen ) { return (true, MonsterGenus.Ghost, MonsterGenus.Ghost); } // Stick
        else if ( _secretSeasoning == Item.Spear ) { return (true, MonsterGenus.Centaur, MonsterGenus.Centaur); } // Spear

        else return (false, 0, 0);

    }

    /// <summary>
    /// This function both clears the combination list and stores the parent data so the proper list can be built.
    /// </summary>
    private void ClearCombinationList() {
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

        // 0x16D is the offset of the scaling value 
        Memory.Instance.Read( _combinationParent1Address + 0x16D, out _combinationParent1Scaling );
        Memory.Instance.Read( _combinationParent2Address + 0x16D, out _combinationParent2Scaling );
    }

    private void GenerateCombinationListBaseGame () {
        Memory.Instance.Read( _combinationParent1Address + 0x8, out MonsterGenus p1Main );
        Memory.Instance.Read( _combinationParent1Address + 0xc, out MonsterGenus p1Sub );

        Memory.Instance.Read( _combinationParent2Address + 0x8, out MonsterGenus p2Main );
        Memory.Instance.Read( _combinationParent2Address + 0xc, out MonsterGenus p2Sub );

        Dictionary<MonsterBreed, int> comboResults = new Dictionary<MonsterBreed, int>();

        MonsterGenus[] order = [ p1Main, p1Sub, p2Main, p2Sub ];
        var totalStrength = 0;
        for ( var i = 0; i < 4; i++ ) {
            for ( var j = 0; j < 4; j++ ) {
                MonsterBreed? breed = MonsterBreed.GetBreed( order[ i ], order[ j ] );
                if ( breed != null ) {
                    var strength = GetCombinationOutputStrength( ( i * 4 ) + j + 1, order[ i ] );
                    if ( strength != 0 ) {
                        if ( comboResults.ContainsKey( breed ) ) {
                            comboResults[ breed ] = comboResults[ breed ] + strength;
                        }
                        else {
                            comboResults.Add( breed, strength );
                        }
                        totalStrength += strength;
                    }
                }
            }
        }

        // TODO: DISC CHIPS

        int totalPercent = 0;
        foreach ( var breed in comboResults.Keys ) {
            comboResults[ breed ] = (comboResults[ breed ] * 1000 ) / totalStrength;
            comboResults[ breed ] = Math.Max(1, ( comboResults[ breed ] + 5 ) / 10);
            totalPercent += comboResults[ breed ];
        }

        // Sort List
        var comboSorted = comboResults.ToList();
        comboSorted.Sort( ( pair1, pair2 ) => pair2.Value.CompareTo( pair1.Value ) );

        // Write Combo List to Memory
        for ( var i = 0; i < Math.Min( 14, comboSorted.Count ); i++ ) {
            var cAddr = _combinationListAddress + ( (nuint) i * 12 );
            Memory.Instance.Write( cAddr, (byte) comboSorted[ i ].Key.GenusMain );
            Memory.Instance.Write( cAddr + 0x4, (byte) comboSorted[ i ].Key.GenusSub );
            Memory.Instance.Write( cAddr + 0x8, (byte) (byte) comboSorted[ i ].Value );
        }

        _combinationParent1Offsets = GetParentOffsetsInCombinationOrder( _combinationParent1Address );
        _combinationParent2Offsets = GetParentOffsetsInCombinationOrder( _combinationParent2Address );

        _combinationPotentialResults = comboSorted;
    }

    private int GetCombinationOutputStrength ( int position, MonsterGenus genus ) {
        switch ( position ) {
            case 1: return 4 + ( _breedCombinationStrength[ (int) genus ] );
            case 2: return 4 * ( _breedCombinationStrength[ (int) genus ] );
            case 3: return 8 * ( _breedCombinationStrength[ (int) genus ] );
            case 4: return 4 * ( _breedCombinationStrength[ (int) genus ] );
            case 5: return 0;
            case 6: return 3 + ( _breedCombinationStrength[ (int) genus ] );
            case 7: return 4 * ( _breedCombinationStrength[ (int) genus ] );
            case 8: return 2 * ( _breedCombinationStrength[ (int) genus ] );
            case 9: return 4 * ( _breedCombinationStrength[ (int) genus ] );
            case 10: return 2 * ( _breedCombinationStrength[ (int) genus ] );
            case 11: return 2 + ( _breedCombinationStrength[ (int) genus ] );
            case 12: return 2 * ( _breedCombinationStrength[ (int) genus ] );
            case 13: return 2 * ( _breedCombinationStrength[ (int) genus ] );
            case 14: return 1 * ( _breedCombinationStrength[ (int) genus ] );
            case 15: return 0;
            case 16: return 1 + ( _breedCombinationStrength[ (int) genus ] );
        }

        return 0;
    }

    private void GenerateCombinationListModified() {
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
            Memory.Instance.Write( cAddr, (byte) comboSorted[ i ].Key.GenusMain );
            Memory.Instance.Write( cAddr + 0x4, (byte) comboSorted[ i ].Key.GenusSub );
            Memory.Instance.Write( cAddr + 0x8, (byte) (byte) comboSorted[ i ].Value );
        }

        _combinationParent1Offsets = GetParentOffsetsInCombinationOrder( _combinationParent1Address );
        _combinationParent2Offsets = GetParentOffsetsInCombinationOrder( _combinationParent2Address );

        _combinationPotentialResults = comboSorted;
    }



    /// <summary>
    ///  This function prepares for the stat bonuses call. 
    ///  If the monster is an MMBreed, we overwrite what the game tried with the base variant.
    ///  If the monster is a base game breed, it was properly set up already.
    ///  For either case, we build the 
    /// </summary>
    /// <param name="unk1"></param>
    private void SetupCombinationFinalStatsUpdate ( nuint unk1 ) {
        Logger.Info( $"Overwriting Combination Monster Status {unk1}", Color.OrangeRed );
        _hook_combinationFinalStatsUpdate!.OriginalFunction( unk1 );

        (byte, double)[] childGrowths = new (byte, double)[ 6 ];
        
        MonsterBreed childBreed = MonsterBreed.GetBreed( _monsterCurrent.GenusMain, _monsterCurrent.GenusSub );
        /* TODO - Perhaps introduce variants into this process? Not sure how that'll work to be honest.
        //var mmBreed = MMBreed.GetBreed( _monsterCurrent.GenusMain, _monsterCurrent.GenusSub );

        if ( mmBreed != null ) {
            _mod.WriteMonsterData( mmBreed._monsterVariants[ 0 ] );
            var variant = mmBreed._monsterVariants[ 0 ];
            childGrowths = [ (0, variant.GrowthRateLife), (1, variant.GrowthRatePower + 0.01),
                (2, variant.GrowthRateIntelligence + 0.02), (3, variant.GrowthRateSkill + 0.03),
                (4, variant.GrowthRateSpeed + 0.04), (5, variant.GrowthRateDefense + 0.05) ];
        }*/

   
        // This is a bit strange, but we're accounting for the fact that it orders by Starting Stat (/2000 to get a small amount)
        // plus infinitely small values in the stat order to break ties.
            childGrowths = [ 
                (0, childBreed.GrowthRateLife + (childBreed.Life / 2000.0) ), 
                (1, childBreed.GrowthRatePower + 0.0001 + (childBreed.Power / 2000.0) ),
                (2, childBreed.GrowthRateIntelligence + 0.0002 + (childBreed.Intelligence / 2000.0) ), 
                (3, childBreed.GrowthRateSkill + 0.0003 + (childBreed.Skill / 2000.0) ),
                (4, childBreed.GrowthRateSpeed + 0.0004 + (childBreed.Speed / 2000.0) ), 
                (5, childBreed.GrowthRateDefense + 0.0005 + (childBreed.Defense / 2000.0) ) ];

        if ( _overwriteCombinationLogic ) {
            _mod.WriteMonsterData( childBreed );
            ApplyParentStatBonuses(childBreed, childGrowths );
            ApplySecretSeasoning();
        }

        if ( _mod._configuration.MonsterSizesEnabled ) {
            ApplyMonsterScaling();
        }
    }

    private void ApplyParentStatBonuses( MonsterBreed breed, (byte, double)[] childGrowths ) {

        var statOrderP1 = _combinationParent1Offsets;
        var statOrderP2 = _combinationParent2Offsets;

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
                statPercentages[ childGrowths[ i ].Item1 ] = (
                    ( correctOrderCount == 6 ) ? 0.80 :
                    ( correctOrderCount == 4 ) ? 0.70 :
                    ( correctOrderCount == 3 ) ? 0.60 :
                    ( correctOrderCount == 2 ) ? 0.50 :
                    ( correctOrderCount == 1 ) ? 0.40 : 0.15 );
            }

            else { statPercentages[ childGrowths[ i ].Item1 ] = 0.15; }
        }

        double chancePercentage = 0.85;
        if ( _mod._configuration.CombinationChanceAdjustment == Configuration.Config.CombinaitonSettings.NoChanges ) { 
            foreach ( var kvp in _combinationPotentialResults ) {
                if ( kvp.Key == breed ) {
                    if ( kvp.Value < 2.0 ) { chancePercentage = 1.0; }
                    else if ( kvp.Value < 4.0 ) { chancePercentage = 0.9; }
                    else if ( kvp.Value < 8.0 ) { chancePercentage = 0.8; }
                    else if ( kvp.Value < 15.0 ) { chancePercentage = 0.7; }
                    else if ( kvp.Value < 30.0 ) { chancePercentage = 0.6; }
                    else if ( kvp.Value < 50.0 ) { chancePercentage = 0.5; }
                    else if ( kvp.Value < 75.0 ) { chancePercentage = 0.4; }
                    else if ( kvp.Value < 100.0 ) { chancePercentage = 0.3; }
                    else { chancePercentage = 0.2; }
                }
            }
        }
        statOrderP1 = statOrderP1.OrderBy( x => x.Item1 ).ToArray();
        statOrderP2 = statOrderP2.OrderBy( x => x.Item1 ).ToArray();

        double[] statOffsets = new double[ 6 ];
        for ( var i = 0; i < 6; i++ ) { statOffsets[ i ] = ( ( ( 2 * statOrderP1[ i ].Item2 ) + statOrderP2[ i ].Item2 ) / 3 ); }

        var ski = (ushort) ( statOffsets[ 3 ] * statPercentages[ 3 ] * chancePercentage ); ;
        _monsterCurrent.Life +=         (ushort) ( statOffsets[ 0 ] * statPercentages[ 0 ] * chancePercentage );
        _monsterCurrent.Power +=        (ushort) ( statOffsets[ 1 ] * statPercentages[ 1 ] * chancePercentage );
        _monsterCurrent.Intelligence += (ushort) ( statOffsets[ 2 ] * statPercentages[ 2 ] * chancePercentage );
        _monsterCurrent.Skill +=        (ushort) ( statOffsets[ 3 ] * statPercentages[ 3 ] * chancePercentage );
        _monsterCurrent.Speed +=        (ushort) ( statOffsets[ 4 ] * statPercentages[ 4 ] * chancePercentage );
        _monsterCurrent.Defense +=      (ushort) ( statOffsets[ 5 ] * statPercentages[ 5 ] * chancePercentage );

    }

    /// <summary>
    /// Applies the secret seasoning stats if they are stat affecting items (i.e., non monster species ones).
    /// </summary>
    private void ApplySecretSeasoning() {
        bool original = _mod._configuration.CombinationItemAdjustment == Configuration.Config.CombinaitonSettings.NoChanges;
        if ( _secretSeasoning == Item.BigFootstep ) {
            if ( original ) { _monsterCurrent.Life += 10; _monsterCurrent.Defense += 10; }
            else { _monsterCurrent.Life += 50; _monsterCurrent.Defense += 50; }
        }

        else if ( _secretSeasoning == Item.CrabsClaw ) {
            _monsterCurrent.Skill += 50; _monsterCurrent.Defense += 50;
        }

        else if ( _secretSeasoning == Item.TaurusHorn ) {
            _monsterCurrent.NatureRaw += 25;
            if ( !original ) { _monsterCurrent.NatureBase += 25; }
        }

        else if ( _secretSeasoning == Item.OldSheath ) {
            _monsterCurrent.Defense -= 10;
            if ( !original ) { _monsterCurrent.Defense += 20; }
        }

        else if ( _secretSeasoning == Item.DiscChipsApe ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.ParepareJungle;
        }

        else if ( _secretSeasoning == Item.DiscChipsArrowhead ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Guard;
        }

        else if ( _secretSeasoning == Item.DiscChipsBajarl ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Vigor;
        }

        else if ( _secretSeasoning == Item.DiscChipsBaku ) {
            _monsterCurrent.Form += 50;
            if ( !original ) { _monsterCurrent.Life += 25; _monsterCurrent.Power += 25; }
        }

        else if ( _secretSeasoning == Item.DiscChipsBeaclon ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Pull;
        }

        else if ( _secretSeasoning == Item.DiscChipsCentaur ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.MandyDesert;
        }

        else if ( _secretSeasoning == Item.DiscChipsColorP ) { // TODO: One of these is not going to work.
            _monsterCurrent.LoyalSpoil += 50;
            if ( !original ) { _monsterCurrent.Life += 50; }
        }

        else if ( _secretSeasoning == Item.DiscChipsDucken ) {
            _monsterCurrent.Speed += 50;
        }

        else if ( _secretSeasoning == Item.DiscChipsDurahan ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Domino;
        }

        else if ( _secretSeasoning == Item.DiscChipsGaboo ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Fight;
        }

        else if ( _secretSeasoning == Item.DiscChipsGali ) {
            _monsterCurrent.NatureRaw += 50;
            if ( !original ) { _monsterCurrent.NatureBase += 50; }
        }

        else if ( _secretSeasoning == Item.DiscChipsGhost ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Dodge;
        }

        else if ( _secretSeasoning == Item.DiscChipsGolem ) {
            _monsterCurrent.Power += 50;
        }

        else if ( _secretSeasoning == Item.DiscChipsHare ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Grit;
        }

        else if ( _secretSeasoning == Item.DiscChipsHenger ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Shoot;
        }

        else if ( _secretSeasoning == Item.DiscChipsHopper ) {
            _monsterCurrent.Form -= 50;
            if ( !original ) { _monsterCurrent.Skill += 25; _monsterCurrent.Speed += 25; }
        }

        else if ( _secretSeasoning == Item.DiscChipsJell ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Endure;
        }

        else if ( _secretSeasoning == Item.DiscChipsJill ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.PapasMountain;
        }

        else if ( _secretSeasoning == Item.DiscChipsJoker ) {
            _monsterCurrent.NatureRaw -= 50;
            if ( !original ) { _monsterCurrent.NatureBase -= 50; }
        }

        else if ( _secretSeasoning == Item.DiscChipsKato ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Meditate;
        }

        else if ( _secretSeasoning == Item.DiscChipsMetalner ) { // TODO: One of these is not going to work.
            _monsterCurrent.LoyalSpoil += 50;
            if ( !original ) { _monsterCurrent.Defense += 50; }
        }

        else if ( _secretSeasoning == Item.DiscChipsMew ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Hurry;
        }

        else if ( _secretSeasoning == Item.DiscChipsMocchi ) { 
            _monsterCurrent.Fame += 50;
            if ( !original ) { _monsterCurrent.Lifespan += 5; _monsterCurrent.InitalLifespan += 5; }
        }

        else if ( _secretSeasoning == Item.DiscChipsMock ) {
            _monsterCurrent.Lifespan += 10; _monsterCurrent.InitalLifespan += 10;
        }

        else if ( _secretSeasoning == Item.DiscChipsMonol ) {
            _monsterCurrent.Defense += 50;
        }

        else if ( _secretSeasoning == Item.DiscChipsNaga ) {
            _monsterCurrent.Skill += 50;
        }

        else if ( _secretSeasoning == Item.DiscChipsNiton ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Swim;
        }

        else if ( _secretSeasoning == Item.DiscChipsPhoenix ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.KawreaVolcano;
        }

        else if ( _secretSeasoning == Item.DiscChipsPixie ) {
            _monsterCurrent.Intelligence += 50;
        }

        else if ( _secretSeasoning == Item.DiscChipsPlant ) {
            _monsterCurrent.Lifespan += 10; _monsterCurrent.InitalLifespan += 10;
        }
        else if ( _secretSeasoning == Item.DiscChipsSuezo ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Ease;
        }

        else if ( _secretSeasoning == Item.DiscChipsTiger ) {
            _monsterCurrent.BattleSpecial |= (ushort) BattleSpecials.Will;
        }

        else if ( _secretSeasoning == Item.DiscChipsUndine ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Study;
        }

        else if ( _secretSeasoning == Item.DiscChipsWorm ) {
            _monsterCurrent.Life += 50;
        }
        else if ( _secretSeasoning == Item.DiscChipsWracky ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Leap;
        }
        else if ( _secretSeasoning == Item.DiscChipsZilla ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.TorbleSea;
        }
        else if ( _secretSeasoning == Item.DiscChipsZuum ) {
            _monsterCurrent.TrainBoost |= (ushort) TrainingBoosts.Run;
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

        ushort[] baseStats = new ushort[ 6 ]; //
        ushort[] curStats = new ushort[ 6 ]; // Starts at 0x10
        byte[] growths = new byte[ 6 ]; // Starts at 0x30

        // Read Stats in order of L, P, I, Sk, Sp, D. Manual as the orders are nonsensical in memory.
        // Current Stats increments by Shorts, LPDSkSpI, Growths by Bytes PILSkSD
        baseStats[ 0 ] = parentBreed.Life;
        Memory.Instance.Read( parentAddress + 0x10, out curStats[ 0 ] );
        Memory.Instance.Read( parentAddress + 0x30 + 0x2, out growths[ 0 ] );

        baseStats[ 1 ] = parentBreed.Power;
        Memory.Instance.Read( parentAddress + 0x10 + 0x2, out curStats[ 1 ] );
        Memory.Instance.Read( parentAddress + 0x30, out growths[ 1 ] );

        baseStats[ 2 ] = parentBreed.Intelligence;
        Memory.Instance.Read( parentAddress + 0x10 + 0xA, out curStats[ 2 ] );
        Memory.Instance.Read( parentAddress + 0x30 + 0x1, out growths[ 2 ] );

        baseStats[ 3 ] = parentBreed.Skill;
        Memory.Instance.Read( parentAddress + 0x10 + 0x6, out curStats[ 3 ] );
        Memory.Instance.Read( parentAddress + 0x30 + 0x3, out growths[ 3 ] );

        baseStats[ 4 ] = parentBreed.Speed;
        Memory.Instance.Read( parentAddress + 0x10 + 0x8, out curStats[ 4 ] );
        Memory.Instance.Read( parentAddress + 0x30 + 0x4, out growths[ 4 ] );

        baseStats[ 5 ] = parentBreed.Defense;
        Memory.Instance.Read( parentAddress + 0x10 + 0x4, out curStats[ 5 ] );
        Memory.Instance.Read( parentAddress + 0x30 + 0x5, out growths[ 5 ] );

        for ( var i = 5; i >= 0; i-- ) {
            byte maxStat = (byte) i;
            var maxVal = -1.0;
            for ( var j = 5; j >= 0; j-- ) {
                var weightTotal = Math.Max( 1, ( curStats[ j ] * ( 0.5 * growths[ j ] ) ) );
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
