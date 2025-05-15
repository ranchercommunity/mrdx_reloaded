using System.Drawing;
using Iced.Intel;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Configuration;

namespace MRDX.Game.DynamicTournaments;

public readonly record struct TournamentInfo(
    string Name,
    EMonsterRank Rank,
    int Tier,
    Range<int> Id,
    Range<int> StatOffset)
{
    public int Size => ( ( Id.Max + 1 ) - Id.Min );
}

public readonly record struct TournamentRuleset (
    string Name,
    EMonsterRank[] Ranks,
    MonsterGenus[] MainBreeds,
    MonsterGenus[] SubBreeds,
    int MinParticipants,
    EMonsterRegion MonsterRegion ) { }

public class TournamentPool(TournamentData tournament, Config conf, EPool pool)
{
    /*
    private static readonly Dictionary<EPool, TournamentInfo> Tourneys = new()
    {
        { EPool.S, new TournamentInfo("S Rank", EMonsterRank.S, 6, (1, 8), (0, 0)) },
        { EPool.A, new TournamentInfo("A Rank", EMonsterRank.A, 5, (9, 16), (0, 0)) },
        { EPool.B, new TournamentInfo("B Rank", EMonsterRank.B, 4, (17, 26), (0, 0)) },
        { EPool.C, new TournamentInfo("C Rank", EMonsterRank.C, 2, (27, 36), (0, 0)) },
        { EPool.D, new TournamentInfo("D Rank", EMonsterRank.D, 1, (37, 44), (0, 0)) },
        { EPool.E, new TournamentInfo("E Rank", EMonsterRank.E, 0, (45, 50), (0, 0)) },
        { EPool.X_MOO, new TournamentInfo("L - Moo", EMonsterRank.L, 9, (51, 51), (250, 9999)) },
        { EPool.L, new TournamentInfo("Legend", EMonsterRank.L, 9, (52, 53), (0, 9999)) },
        { EPool.M, new TournamentInfo("Major 4", EMonsterRank.M, 7, (54, 61), (0, 0)) },
        { EPool.A_Phoenix, new TournamentInfo("A - Phoenix", EMonsterRank.A, 6, (62, 64), (0, 0)) },
        { EPool.A_DEdge, new TournamentInfo("A - Double Edge", EMonsterRank.A, 6, (66, 66), (0, 0)) },
        { EPool.B_Dragon, new TournamentInfo("B - Dragon Tusk", EMonsterRank.B, 5, (65, 65), (0, 0)) },
        { EPool.F_Hero, new TournamentInfo("F - Hero", EMonsterRank.A, 7, (67, 71), (0, 9999)) },
        { EPool.F_Heel, new TournamentInfo("F - Heel", EMonsterRank.A, 7, (72, 76), (0, 9999)) },
        { EPool.F_Elder, new TournamentInfo("F - Elder", EMonsterRank.A, 8, (77, 79), (0, 9999)) },
        { EPool.S_FIMBA, new TournamentInfo("S FIMBA", EMonsterRank.S, 8, (80, 83), (200, 0)) },
        { EPool.A_FIMBA, new TournamentInfo("A FIMBA", EMonsterRank.A, 6, (84, 87), (200, 0)) },
        { EPool.B_FIMBA, new TournamentInfo("B FIMBA", EMonsterRank.B, 5, (88, 91), (200, 0)) },
        { EPool.C_FIMBA, new TournamentInfo("C FIMBA", EMonsterRank.C, 4, (92, 95), (100, 0)) },
        { EPool.D_FIMBA, new TournamentInfo("D FIMBA", EMonsterRank.D, 2, (96, 99), (100, 0)) },
        { EPool.S_FIMBA2, new TournamentInfo("S FIMBA2", EMonsterRank.S, 8, (100, 103), (200, 0)) },
        { EPool.A_FIMBA2, new TournamentInfo("A FIMBA2", EMonsterRank.A, 6, (104, 107), (200, 0)) },
        { EPool.B_FIMBA2, new TournamentInfo("B FIMBA2", EMonsterRank.B, 5, (108, 111), (200, 0)) },
        { EPool.C_FIMBA2, new TournamentInfo("C FIMBA2", EMonsterRank.C, 4, (112, 115), (100, 0)) },
        { EPool.D_FIMBA2, new TournamentInfo("D FIMBA2", EMonsterRank.D, 2, (116, 119), (100, 0)) },
        //{ ETournamentPools.L_FIMBA, new TournamentInfo("L FIMBA", EMonsterRanks.L, 1, (80,80), (0, 0)) }
    };*/

    private static readonly Dictionary<EPool, TournamentRuleset> Tournaments = new Dictionary<EPool, TournamentRuleset>() {
        { EPool.L, new TournamentRuleset("Legend", [EMonsterRank.L], [], [], 2, EMonsterRegion.IMA) },
        { EPool.M, new TournamentRuleset("Major 4",[EMonsterRank.M], [], [], 6, EMonsterRegion.IMA) },
        { EPool.S, new TournamentRuleset("S Rank", [EMonsterRank.S], [], [], 8, EMonsterRegion.IMA) },
        { EPool.A, new TournamentRuleset("A Rank", [EMonsterRank.A], [], [], 8, EMonsterRegion.IMA) },
        { EPool.B, new TournamentRuleset("B Rank", [EMonsterRank.B], [], [], 10, EMonsterRegion.IMA) },
        { EPool.C, new TournamentRuleset("C Rank", [EMonsterRank.C], [], [], 10, EMonsterRegion.IMA) },
        { EPool.D, new TournamentRuleset("D Rank", [EMonsterRank.D], [], [], 8, EMonsterRegion.IMA) },
        { EPool.E, new TournamentRuleset("E Rank", [EMonsterRank.E], [], [], 6, EMonsterRegion.IMA) },


        { EPool.A_Phoenix, new TournamentRuleset("A - Phoenix", [EMonsterRank.A, EMonsterRank.S],
            [MonsterGenus.Phoenix, MonsterGenus.Ducken], [MonsterGenus.Phoenix, MonsterGenus.Dragon],
            3, EMonsterRegion.IMA) },

        { EPool.A_DEdge, new TournamentRuleset("A - Double Edge", [EMonsterRank.A, EMonsterRank.S],
            [MonsterGenus.Durahan], [], 
            1, EMonsterRegion.IMA) },

        { EPool.B_Dragon, new TournamentRuleset("B - Dragon Tusk", [EMonsterRank.B, EMonsterRank.A],
            [MonsterGenus.Dragon], [], 
            1, EMonsterRegion.IMA) },

        { EPool.F_Hero, new TournamentRuleset("F - Hero", [EMonsterRank.B, EMonsterRank.A, EMonsterRank.S],
            [   MonsterGenus.Baku,      MonsterGenus.Bajarl,    MonsterGenus.Beaclon,   MonsterGenus.Centaur,       
                MonsterGenus.ColorPandora,
                MonsterGenus.Ducken,    MonsterGenus.Gaboo,     MonsterGenus.Gali,      MonsterGenus.Golem,
                MonsterGenus.Hare,      MonsterGenus.Henger,    MonsterGenus.Jill,
                MonsterGenus.Mocchi,    MonsterGenus.Niton,     MonsterGenus.Phoenix,   MonsterGenus.Plant,         
                MonsterGenus.Tiger,         
                MonsterGenus.Undine,    MonsterGenus.Worm,      MonsterGenus.Zilla,     MonsterGenus.Zuum], 
            [   MonsterGenus.Baku,      MonsterGenus.Centaur,   MonsterGenus.Gali,      MonsterGenus.Plant,
                MonsterGenus.Henger,    MonsterGenus.Undine],
            5, EMonsterRegion.IMA) },

        { EPool.F_Heel, new TournamentRuleset("F - Heel", [EMonsterRank.B, EMonsterRank.A, EMonsterRank.S],
            [   MonsterGenus.Ape,       MonsterGenus.Arrowhead, MonsterGenus.Dragon,    MonsterGenus.Hopper,
                MonsterGenus.Ghost,     MonsterGenus.Jell,      MonsterGenus.Joker,     MonsterGenus.Kato,
                MonsterGenus.Metalner,  MonsterGenus.Mew,       MonsterGenus.Mock,
                MonsterGenus.Monol,     MonsterGenus.Naga,      MonsterGenus.Pixie,     MonsterGenus.Suezo, 
                MonsterGenus.Wracky], 
            [   MonsterGenus.Ape,       MonsterGenus.Dragon,    MonsterGenus.Suezo,     MonsterGenus.Joker, 
                MonsterGenus.Monol,     MonsterGenus.Naga,      MonsterGenus.Wracky],
            5, EMonsterRegion.IMA) },


        { EPool.F_Elder, new TournamentRuleset("F - Elder", [EMonsterRank.A, EMonsterRank.S, EMonsterRank.M],
            [   MonsterGenus.Baku,      MonsterGenus.Plant,     MonsterGenus.Mew,       MonsterGenus.Ape,
                MonsterGenus.Arrowhead, MonsterGenus.Durahan,   MonsterGenus.ColorPandora,
                MonsterGenus.Mock,      MonsterGenus.Wracky,    MonsterGenus.Kato,],
            [   MonsterGenus.Baku,      MonsterGenus.Plant,     MonsterGenus.Mew,       MonsterGenus.Ape,
                MonsterGenus.Arrowhead, MonsterGenus.Durahan,   MonsterGenus.ColorPandora,
                MonsterGenus.Mock,      MonsterGenus.Wracky,    MonsterGenus.Kato,],
            3, EMonsterRegion.IMA) },

        { EPool.L_FIMBA, new TournamentRuleset("L FIMBA", [EMonsterRank.L], [], [], 1, EMonsterRegion.FIMBA) },
        { EPool.S_FIMBA, new TournamentRuleset("S FIMBA", [EMonsterRank.S, EMonsterRank.M], [], [], 1, EMonsterRegion.FIMBA) },
        { EPool.A_FIMBA, new TournamentRuleset("A FIMBA", [EMonsterRank.A], [], [], 1, EMonsterRegion.FIMBA) },
        { EPool.B_FIMBA, new TournamentRuleset("B FIMBA", [EMonsterRank.B], [], [], 1, EMonsterRegion.FIMBA) },
        { EPool.C_FIMBA, new TournamentRuleset("C FIMBA", [EMonsterRank.C], [], [], 1, EMonsterRegion.FIMBA) },
        { EPool.D_FIMBA, new TournamentRuleset("D FIMBA", [EMonsterRank.D], [], [], 1, EMonsterRegion.FIMBA) },
    };

    private static readonly MonsterGenus[] SpecialSubs =
    [
        MonsterGenus.XX, MonsterGenus.XY, MonsterGenus.XZ,
        MonsterGenus.YX, MonsterGenus.YY, MonsterGenus.YZ
    ];

    public readonly TournamentRuleset TournamentRuleset = Tournaments[ pool ];

    public readonly EPool Pool = pool;

    public List<TournamentMonster> ValidMonsters = new List<TournamentMonster>();

    public List<TournamentMonster> GetTournamentParticipants(List<TournamentMonster> monsters) {
        return monsters.FindAll( m => ValidateMonsterLegality( m ) );
    }

    /// <summary>
    /// Checks all of the provided monsters for use in this tournament pool.
    /// Stores the valid monsters in 'ValidMonsters'
    /// </summary>
    /// <param name="monsters"></param>
    /// <returns>The necessary number of additional monsters required to make a valid tournament.</returns>
    public int ValidateTournamentReadiness(List<TournamentMonster> monsters) {
        ValidMonsters = monsters.FindAll( m => ValidateMonsterLegality( m ) );
        return Math.Max( TournamentRuleset.MinParticipants - ValidMonsters.Count, 0 );
    }
    public bool ValidateMonsterLegality ( TournamentMonster monster ) {
        if ( TournamentRuleset.MonsterRegion != monster.Region ) { return false; }
        if ( TournamentRuleset.Ranks.Contains( monster.Rank ) == false ) { return false; }

        if ( TournamentRuleset.MainBreeds.Length != 0 || TournamentRuleset.SubBreeds.Length != 0 ) {
            if ( TournamentRuleset.MainBreeds.Contains( monster.GenusMain) == false &&
                 TournamentRuleset.SubBreeds.Contains( monster.GenusSub) == false ) { return false; }
        }

        return true;
    }

    public TournamentMonster GenerateNewValidMonster(List<MonsterGenus> available)
    {
        var breed = DetermineGeneratedMonsterBreed( available );
        var monster = GenerateNewValidMonster(breed);

        ValidMonsters.Add( monster );
        return monster;
    }

    private MonsterBreed DetermineGeneratedMonsterBreed( List<MonsterGenus> available ) {
        var mainRestrictions = TournamentRuleset.MainBreeds;
        var subRestrictions = TournamentRuleset.SubBreeds;

        var allBreeds = new List<MonsterBreed>( MonsterBreed.AllBreeds );
        Utils.Shuffle( Random.Shared, allBreeds );
        var breed = allBreeds[ 0 ];
        foreach ( var b in allBreeds ) {
            if ( SpecialSubs.Contains( b.Sub ) && Random.Shared.NextDouble() < conf.SpeciesUnique ) { continue; }

            if ( mainRestrictions is [] && subRestrictions is [] ) {
                if ( !available.Contains( b.Main ) || !available.Contains( b.Sub ) ) { continue; }
                else {
                    breed = b;
                    break;
                }
            }

            // If we have a restriction, guarantee we generate a breed with this main or sub
            if ( ( mainRestrictions is not [] && mainRestrictions.Contains( b.Main ) ) ||
                ( subRestrictions is not [] && subRestrictions.Contains( b.Sub ) ) ) {
                breed = b;
                break;
            }
        }

        Logger.Info( $"Breed chosen for {TournamentRuleset.Name} - {breed.Main}/{breed.Sub}", Color.AliceBlue );
        return breed;
    }


    private TournamentMonster GenerateNewValidMonster ( MonsterBreed breed ) {
        var nm = GenerateNewMonster( breed );

        // Logger.Trace("TP: Basics Setup " + nm.techs.Count, Color.AliceBlue);

        var minRank = TournamentRuleset.Ranks.Max();
        // Unfortunately ranks are in reverse order, making this a bit odd to read. (Max is actually the lowest available rank).
        while ( nm.StatTotal < TournamentData.RankBSTRanges[ minRank ].Min )
            nm.AdvanceMonth();

        nm.PromoteToRank( minRank );

        nm.Lifespan = minRank switch {
            // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
            EMonsterRank.L => (ushort) TournamentData.LifespanRNG.Next( 12, 16 + 1 ),
            EMonsterRank.M => (ushort) TournamentData.LifespanRNG.Next( 16, 20 + 1 ),
            EMonsterRank.S => (ushort) TournamentData.LifespanRNG.Next( 20, 26 + 1 ),
            EMonsterRank.A => (ushort) TournamentData.LifespanRNG.Next( 24, 30 + 1 ),
            EMonsterRank.B => (ushort) ( nm.LifeTotal - TournamentData.LifespanRNG.Next( 14, 22 + 1 ) ),
            EMonsterRank.C => (ushort) ( nm.LifeTotal - TournamentData.LifespanRNG.Next( 6, 12 ) ),
            EMonsterRank.D => (ushort) ( nm.LifeTotal - TournamentData.LifespanRNG.Next( 2, 6 + 1 ) ),
            EMonsterRank.E => nm.LifeTotal,
            _ => nm.Lifespan
        };
        nm.Alive = true;

        nm.Region = TournamentRuleset.MonsterRegion;

        return nm;
    }

    private TournamentMonster GenerateNewMonster(MonsterBreed breed) {
        Logger.Debug( "TP: Generating ", Color.AliceBlue );

        /*Utils.Shuffle( Random.Shared, TournamentData.RandomNameList );
        var name = TournamentData.RandomNameList[ 0 ];
        for ( var i = 0; i < TournamentData.RandomNameList.Length; i++ ) {
            if ( TournamentData.Monsters)
        }*/

        var monData = new BattleMonsterData {
            GenusMain = breed.Main,
            GenusSub = breed.Sub,
            Name = TournamentData.RandomNameList[ Random.Shared.Next( TournamentData.RandomNameList.Length ) ],
            Life = 80,
            Power = 1,
            Skill = 1,
            Speed = 1,
            Defense = 1,
            Intelligence = 1,
            Nature = (sbyte) Random.Shared.Next( 255 ),
            Fear = (byte) Random.Shared.Next( 25 ),
            Spoil = (byte) Random.Shared.Next( 25 ),
            ArenaSpeed = 0,
            GutsRate = 10,
            BattleSpecial = (BattleSpecials) Random.Shared.Next( 4 )
        };

        switch ( tournament._config.SpeciesAccuracyTraits ) {
            case Config.ESpeciesAccuracyTraits.Strict:
                monData.ArenaSpeed = Byte.Parse( breed.SDATAValues[ 19 ] );
                monData.GutsRate = Byte.Parse( breed.SDATAValues[ 20 ] );
                break;
            case Config.ESpeciesAccuracyTraits.Loose:
                monData.ArenaSpeed = Math.Clamp( (byte) ( Byte.Parse( breed.SDATAValues[ 19 ] ) + Random.Shared.Next( -1, 1 ) ), (byte) 0, (byte) 4 );
                monData.GutsRate = Math.Clamp( (byte) ( Byte.Parse( breed.SDATAValues[ 20 ] ) + Random.Shared.Next( -2, 2 ) ), (byte) 6, (byte) 21 );
                break;
            case Config.ESpeciesAccuracyTraits.WildWest:
                monData.ArenaSpeed = (byte) Random.Shared.Next( 0, 4 );
                monData.GutsRate = (byte) Random.Shared.Next( 7, 21 );
                break;
        }

        var nm = new TournamentMonster( conf, monData );
        Logger.Trace( $"TP: Breed " + nm.GenusMain + " " + nm.GenusSub + $" AS:{monData.ArenaSpeed}|GUTS:{monData.GutsRate}", Color.AliceBlue );

        // Attempt to assign three basics, weighted generally towards worse basic techs with variance.
        if ( nm.BreedInfo.TechList[ 0 ].Type == ErrantryType.Basic )
            nm.MonsterAddTechnique( nm.BreedInfo.TechList[ 0 ] );
        for ( var tc = 0; tc < 3; tc++ ) {
            var tech = nm.BreedInfo.TechList[ 0 ];

            for ( var j = 1; j < nm.BreedInfo.TechList.Count; j++ ) {
                var nt = nm.BreedInfo.TechList[ j ];
                if ( nt.Type == ErrantryType.Basic )
                    if ( nt.TechValue - Random.Shared.Next( 20 ) < tech.TechValue )
                        tech = nt;
            }

            nm.MonsterAddTechnique( tech );
        }

        return nm;
    }

}