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
using System.Runtime.Intrinsics.Arm;

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

public class CombinationHandler {

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

    private MonsterGenus _combinationParent1Main;
    private byte[] _combinationParent1Techniques = new byte[48];

    private MonsterGenus _combinationParent2Main;

    private int[] _breedCombinationStrength = [ 10, 7, 4, 1, 4, 4, 5, 10, 9, 5, // 0-9
                                                3, 6, 6, 6, 4, 6, 2, 2, 1, 2, // 10-19
                                                1, 1, 1, 6, 3, 4, 3, 2, 6, 1, // 20-29
                                                4, 1, 1, 6, 6, 2, 6, 6, // 30-37
                                                1, 1, 1, 1, 1, 1]; // Specials 38-43
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

        if ( _mod._configuration.MonsterSizesEnabled ) {
            ApplyMonsterScaling();
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


        // This is where ???'s are filtered out.
        if ( !_mod._configuration.CombinationSpecialSubspecies ) {
            if ( p1Sub == MonsterGenus.XX || p1Sub == MonsterGenus.XY || p1Sub == MonsterGenus.XZ ||
                 p1Sub == MonsterGenus.YX || p1Sub == MonsterGenus.YY || p1Sub == MonsterGenus.YZ ) { p1Sub = p1Main; }
            if ( p2Sub == MonsterGenus.XX || p2Sub == MonsterGenus.XY || p2Sub == MonsterGenus.XZ ||
                 p2Sub == MonsterGenus.YX || p2Sub == MonsterGenus.YY || p2Sub == MonsterGenus.YZ ) { p2Sub = p2Main; }
        }

        MonsterGenus[] order = [ p1Main, p1Sub, p2Main, p2Sub ];
        Dictionary<MonsterBreed, int> comboResults = new Dictionary<MonsterBreed, int>();
        var dcb = _discChipGenusMapping.TryGetValue( _secretSeasoning, out MonsterGenus discChipsBoost );

        
        var totalStrength = 0;
        for ( var i = 0; i < 4; i++ ) {
            for ( var j = 0; j < 4; j++ ) {
                MonsterBreed? breed = MonsterBreed.GetBreed( order[ i ], order[ j ] );
                if ( breed != null ) {
                    var strength = GetCombinationOutputStrength( ( i * 4 ) + j + 1, order[ i ] );
                    if ( dcb && order[i] == discChipsBoost && order[j] != discChipsBoost ) { strength += 5; }
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

        _combinationParent1Main = p1Main;
        _combinationParent2Main = p2Main;
        Memory.Instance.ReadRaw( _combinationParent1Address + 0x19A, out _combinationParent1Techniques, 48 );

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

            if ( childBreed.GenusMain == _combinationParent1Main ) { ApplyTechniquesParentSame( childBreed ); }
            else { ApplyTechniquesParentDifferent( childBreed ); }
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
                double rBoost = Random.Shared.Next( 0, 10 ) / 100.0;
                statPercentages[ childGrowths[ i ].Item1 ] = (
                    ( correctOrderCount == 6 ) ? 0.70 + rBoost :
                    ( correctOrderCount == 4 ) ? 0.60 + rBoost :
                    ( correctOrderCount == 3 ) ? 0.50 + rBoost :
                    ( correctOrderCount == 2 ) ? 0.40 + rBoost :
                    ( correctOrderCount == 1 ) ? 0.30 + rBoost : 0.15 );
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

    private readonly Dictionary<Item, MonsterGenus> _discChipGenusMapping = new Dictionary<Item, MonsterGenus>() {
        { Item.DiscChipsApe, MonsterGenus.Ape },
        { Item.DiscChipsBajarl, MonsterGenus.Bajarl },
        { Item.DiscChipsBaku, MonsterGenus.Baku },
        { Item.DiscChipsBeaclon, MonsterGenus.Beaclon },
        { Item.DiscChipsCentaur, MonsterGenus.Centaur },
        { Item.DiscChipsColorP, MonsterGenus.ColorPandora },
        { Item.DiscChipsDucken, MonsterGenus.Ducken },
        { Item.DiscChipsDurahan, MonsterGenus.Durahan },
        { Item.DiscChipsGaboo, MonsterGenus.Gaboo },
        { Item.DiscChipsGali, MonsterGenus.Gali },
        { Item.DiscChipsGhost, MonsterGenus.Ghost },
        { Item.DiscChipsGolem, MonsterGenus.Golem },
        { Item.DiscChipsHare, MonsterGenus.Hare },
        { Item.DiscChipsHenger, MonsterGenus.Henger },
        { Item.DiscChipsHopper, MonsterGenus.Hopper },
        { Item.DiscChipsJell, MonsterGenus.Jell },
        { Item.DiscChipsJill, MonsterGenus.Jill },
        { Item.DiscChipsJoker, MonsterGenus.Joker },
        { Item.DiscChipsKato, MonsterGenus.Kato },
        { Item.DiscChipsMetalner, MonsterGenus.Metalner },
        { Item.DiscChipsMew, MonsterGenus.Mew },
        { Item.DiscChipsMocchi, MonsterGenus.Mocchi },
        { Item.DiscChipsMock, MonsterGenus.Mock },
        { Item.DiscChipsMonol, MonsterGenus.Monol },
        { Item.DiscChipsNaga, MonsterGenus.Naga },
        { Item.DiscChipsNiton, MonsterGenus.Niton },
        { Item.DiscChipsPhoenix, MonsterGenus.Phoenix },
        { Item.DiscChipsPixie, MonsterGenus.Pixie },
        { Item.DiscChipsPlant, MonsterGenus.Plant },
        { Item.DiscChipsSuezo, MonsterGenus.Suezo },
        { Item.DiscChipsTiger, MonsterGenus.Tiger },
        { Item.DiscChipsUndine, MonsterGenus.Undine },
        { Item.DiscChipsWorm, MonsterGenus.Worm },
        { Item.DiscChipsWracky, MonsterGenus.Wracky },
        { Item.DiscChipsZilla, MonsterGenus.Zilla },
        { Item.DiscChipsZuum, MonsterGenus.Zuum },
    };
 
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



    private void ApplyTechniquesParentSame ( MonsterBreed childBreed ) {
        var techList = childBreed.TechList;
        byte[] childTechs = IMonsterTechnique.SerializeTechsLearnedMemory( childBreed.TechsKnown );

        // This function sets which techs have been learned of each errantry type. Last value [6] of LTT is the total.
        int[] learnedTechTypes = [ 0, 0, 0, 0, 0, 0, 0 ];
        for ( var i = 0; i < 24; i++ ) {
            if ( _combinationParent1Techniques[ i * 2 ] == 1 ) {
                var t = techList.FindLast( v => BitOperations.TrailingZeroCount( (uint) v.Slot ) == i ); //BO.TZC allows us to get the 'id' of the slot.
                if ( t != null ) {
                    learnedTechTypes[ (int) t.Type ]++;
                    learnedTechTypes[ 6 ]++;
                }
            }
        }

        int[] shuffle = new int[ techList.Count ];
        for ( var i = 0; i < techList.Count; i++ ) { shuffle[ i ] = i; }
        Utils.Shuffle( Random.Shared, shuffle );

        // Assign Non-Special, Non-Learned techs to the monster based on the inheritedCount.
        var inheritedCount = (int) Math.Floor ( ( learnedTechTypes[ 6 ] - ( learnedTechTypes[ 5 ] + 2 ) ) * 2.0 / 3.0 );
        var learnedTechs = 0;
        for ( var i = 0; i < shuffle.Count(); i++ ) {
            if ( learnedTechs < inheritedCount ) {
                var pendingTech = techList[ shuffle[ i ] ];
                var oneBonus = 0;
                do {
                    var slot = pendingTech.SlotPosition * 2;


                    // Determine if this is a valid sub for the technique.
                    // Going to have both versions of this block in here. We are not 100% confident as to if it work as 
                    // Any failure prevents learning, or any success allows learning. Golem is the culprit (Heavy Kick).

                    /* Any Success = Success
                    var learnable = pendingTech.ErrantryInformation.Count == 0;
                    foreach ( ITechniqueErrantryInformation errantryInfo in pendingTech.ErrantryInformation ) {
                        if ( !errantryInfo.SubRequirements ) { learnable = true; break; }
                        else {
                            bool subReq = errantryInfo.SubsRequired.Count > 0 ? errantryInfo.SubsRequired.Contains( (MonsterGenus) childBreed.GenusSub ) : true;
                            bool subLock = errantryInfo.SubsLocked.Count > 0 ? !errantryInfo.SubsLocked.Contains( (MonsterGenus) childBreed.GenusSub ) : true;
                            if ( subReq && subLock ) { learnable = true; break; }
                        }
                    } */

                    // Any Failure = Failure
                    var learnable = pendingTech.ErrantryInformation.Count != 0;
                    foreach ( ITechniqueErrantryInformation errantryInfo in pendingTech.ErrantryInformation ) {
                        bool subReq = errantryInfo.SubsRequired.Count > 0 ? errantryInfo.SubsRequired.Contains( (MonsterGenus) childBreed.GenusSub ) : true;
                        bool subLock = errantryInfo.SubsLocked.Count > 0 ? !errantryInfo.SubsLocked.Contains( (MonsterGenus) childBreed.GenusSub ) : true;
                        if ( !subReq || !subLock ) { learnable = false; break; }
                    }

                    if ( childTechs[ slot ] == 0 && _combinationParent1Techniques[ slot ] == 1 && pendingTech.Type != ErrantryType.Special && learnable ) {
                        childTechs[ slot ] = 1;
                        learnedTechs++;
                        oneBonus++;
                    }
                    // TODO - Handle the modded case where a tech chain is required for one errantry location but not others.
                    pendingTech = pendingTech.ErrantryInformation.Count == 0 ? null : pendingTech.ErrantryInformation[ 0 ].ChainTechRequired; 
                } while ( pendingTech != null && oneBonus <= 1  );
            } else { break; }
        }

        // Now transfer over the use count of each move learned.
        for ( var i = 0; i < 24; i++ ) {
            if ( childTechs[ i * 2 ] == 1 ) {
                childTechs[ ( i * 2 ) + 1 ] = _combinationParent1Techniques[ ( i * 2 ) + 1 ];
            }
        }

        Memory.Instance.WriteRaw( nuint.Add( Mod.address_monster, 0x192 ), childTechs );
    }

    /// <summary>
    /// Applies techniques to the child based on the combination rules for mismatched Parent/Baby mains.
    /// Chooses 1 tech each, if learned, from Power, Wither, Sharp, and Hit, and gives it to the baby.
    /// </summary>
    /// <param name="childBreed"></param>
    private void ApplyTechniquesParentDifferent( MonsterBreed childBreed ) {
        
        MonsterBreed parentBreed = MonsterBreed.GetBreed( _combinationParent1Main, _combinationParent1Main );
        var parentTechList = parentBreed.TechList;
        var childTechList = childBreed.TechList;
        byte[] childTechs = IMonsterTechnique.SerializeTechsLearnedMemory( childBreed.TechsKnown );

        // This function sets which techs have been learned of each errantry type. Basic and Special will be ignored later.
        int[] learnedTechTypes = [ 0, 0, 0, 0, 0, 0 ];
        for ( var i = 0; i < 24; i++ ) {
            if ( _combinationParent1Techniques[ i * 2 ] == 1 ) {
                var t = parentTechList.FindLast( v => BitOperations.TrailingZeroCount((uint) v.Slot) == i ); //BO.TZC allows us to get the 'id' of the slot.
                if ( t != null ) {
                    learnedTechTypes[ (int) t.Type ]++;
                }
            }
        }

        // Loop through each Tech Type (1-4), finds the first slot technique, and sets the skill if the LTT >= 1 for that type.
        for ( var tt = 1; tt <= 4; tt++ ) {
            if ( learnedTechTypes[ tt ] >= 1 ) {
                for ( var i = 0; i < childTechList.Count(); i++ ) {
                    var pendingTech = childTechList[ i ];
                    ErrantryLocation loc = (ErrantryLocation) (
                        tt == 1 ? 0 :
                        tt == 2 ? 2 :
                        tt == 3 ? 3 :
                        tt == 4 ? 1 : 4 );

                    if ( pendingTech.ErrantryInformation.Count > 0 && 
                         pendingTech.ErrantryInformation[0].ErrantrySlot == 0 &&
                         pendingTech.ErrantryInformation[0].Location == loc ) {
                            childTechs[ pendingTech.SlotPosition * 2 ] = 1;
                            break;
                    }
                }
            }
        }

        Memory.Instance.WriteRaw( nuint.Add( Mod.address_monster, 0x192 ), childTechs );
    }




    /// <summary>
    /// Applies monster scaling based upon a semi-random weighting between the parents and a random mutation value.
    /// </summary>
    private void ApplyMonsterScaling() {
        _mod.HandlerScaling.temporaryScaling = 0;
        if ( !_mod._configuration.MonsterSizesEnabled ) { return; }

        byte scaling = 0;

        if ( _mod._configuration.MonsterSizesGenetics == Configuration.Config.ScalingGenetics.WildWest ) {

            var p1strength = _combinationParent1Scaling == 0 ? 0 : Random.Shared.Next( 0, 2 );
            var p2strength = _combinationParent2Scaling == 0 ? 0 : Random.Shared.Next( 0, 2 );
            var mustrength = Random.Shared.Next( 1, 201 );

            scaling = (byte)
                ( ( ( p1strength * _combinationParent1Scaling ) + ( p2strength * _combinationParent2Scaling ) + mustrength )
                    / ( 1 + p1strength + p2strength ) );
        }

        else {
            var p1value = ( _combinationParent1Scaling == 0 ? 100 : _combinationParent1Scaling );
            var p2value = ( _combinationParent2Scaling == 0 ? 100 : _combinationParent2Scaling );
            var rValue = Random.Shared.Next( 1, 201 );
            int oValue = _mod.HandlerScaling.MonsterScalingFactors[ _combinationParent2Main ] -
                _mod.HandlerScaling.MonsterScalingFactors[ _combinationParent1Main ];

            oValue = oValue < 0 ? Math.Max(1, 100 - Random.Shared.Next( 1, Math.Abs( oValue ) ) ) :
                Math.Min(200, Random.Shared.Next(100, 100 + oValue) );

            double [] strengths = [ Random.Shared.NextDouble() + 1, Random.Shared.NextDouble() + 1,
                Random.Shared.NextDouble() + 1, Random.Shared.NextDouble(), 0 ];
            strengths[ 4 ] = strengths[ 0 ] + strengths[ 1 ] + strengths[ 2 ] + strengths[ 3 ];

            scaling = (byte) (
                ((p1value * strengths[ 0 ]) + ( p2value * strengths[ 1 ]) +
                ( rValue * strengths[ 2 ] ) +  ( oValue * strengths[ 3 ])) /
                strengths[ 4 ] );

        }

        Memory.Instance.Write( Mod.address_monster_mm_scaling, scaling );
        _mod.HandlerScaling.temporaryScaling = scaling;
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
