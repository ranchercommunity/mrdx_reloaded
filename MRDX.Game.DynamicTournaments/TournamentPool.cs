using System.Drawing;
using Iced.Intel;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Configuration;

namespace MRDX.Game.DynamicTournaments;

public readonly record struct TournamentInfo(
    string Name,
    EMonsterRanks Rank,
    int Tier,
    Range<int> Id,
    Range<int> StatOffset)
{
    public int Size => ( ( Id.Max + 1 ) - Id.Min );
}

public readonly record struct TournamentRuleset (
    string Name,
    EMonsterRanks[] Ranks,
    MonsterGenus[] MainBreeds,
    MonsterGenus[] SubBreeds,
    int MinParticipants,
    EMonsterRegion MonsterRegion ) { }

public class TournamentPool(TournamentData tournament, Config conf, ETournamentPools pool)
{
    private static readonly Dictionary<ETournamentPools, TournamentInfo> Tourneys = new()
    {
        { ETournamentPools.S, new TournamentInfo("S Rank", EMonsterRanks.S, 6, (1, 8), (0, 0)) },
        { ETournamentPools.A, new TournamentInfo("A Rank", EMonsterRanks.A, 5, (9, 16), (0, 0)) },
        { ETournamentPools.B, new TournamentInfo("B Rank", EMonsterRanks.B, 4, (17, 26), (0, 0)) },
        { ETournamentPools.C, new TournamentInfo("C Rank", EMonsterRanks.C, 2, (27, 36), (0, 0)) },
        { ETournamentPools.D, new TournamentInfo("D Rank", EMonsterRanks.D, 1, (37, 44), (0, 0)) },
        { ETournamentPools.E, new TournamentInfo("E Rank", EMonsterRanks.E, 0, (45, 50), (0, 0)) },
        { ETournamentPools.X_MOO, new TournamentInfo("L - Moo", EMonsterRanks.L, 9, (51, 51), (250, 9999)) },
        { ETournamentPools.L, new TournamentInfo("Legend", EMonsterRanks.L, 9, (52, 53), (0, 9999)) },
        { ETournamentPools.M, new TournamentInfo("Major 4", EMonsterRanks.M, 7, (54, 61), (0, 0)) },
        { ETournamentPools.A_Phoenix, new TournamentInfo("A - Phoenix", EMonsterRanks.A, 6, (62, 64), (0, 0)) },
        { ETournamentPools.A_DEdge, new TournamentInfo("A - Double Edge", EMonsterRanks.A, 6, (66, 66), (0, 0)) },
        { ETournamentPools.B_Dragon, new TournamentInfo("B - Dragon Tusk", EMonsterRanks.B, 5, (65, 65), (0, 0)) },
        { ETournamentPools.F_Hero, new TournamentInfo("F - Hero", EMonsterRanks.A, 7, (67, 71), (0, 9999)) },
        { ETournamentPools.F_Heel, new TournamentInfo("F - Heel", EMonsterRanks.A, 7, (72, 76), (0, 9999)) },
        { ETournamentPools.F_Elder, new TournamentInfo("F - Elder", EMonsterRanks.A, 8, (77, 79), (0, 9999)) },
        { ETournamentPools.S_FIMBA, new TournamentInfo("S FIMBA", EMonsterRanks.S, 8, (80, 83), (200, 0)) },
        { ETournamentPools.A_FIMBA, new TournamentInfo("A FIMBA", EMonsterRanks.A, 6, (84, 87), (200, 0)) },
        { ETournamentPools.B_FIMBA, new TournamentInfo("B FIMBA", EMonsterRanks.B, 5, (88, 91), (200, 0)) },
        { ETournamentPools.C_FIMBA, new TournamentInfo("C FIMBA", EMonsterRanks.C, 4, (92, 95), (100, 0)) },
        { ETournamentPools.D_FIMBA, new TournamentInfo("D FIMBA", EMonsterRanks.D, 2, (96, 99), (100, 0)) },
        { ETournamentPools.S_FIMBA2, new TournamentInfo("S FIMBA2", EMonsterRanks.S, 8, (100, 103), (200, 0)) },
        { ETournamentPools.A_FIMBA2, new TournamentInfo("A FIMBA2", EMonsterRanks.A, 6, (104, 107), (200, 0)) },
        { ETournamentPools.B_FIMBA2, new TournamentInfo("B FIMBA2", EMonsterRanks.B, 5, (108, 111), (200, 0)) },
        { ETournamentPools.C_FIMBA2, new TournamentInfo("C FIMBA2", EMonsterRanks.C, 4, (112, 115), (100, 0)) },
        { ETournamentPools.D_FIMBA2, new TournamentInfo("D FIMBA2", EMonsterRanks.D, 2, (116, 118), (100, 0)) },
        //{ ETournamentPools.L_FIMBA, new TournamentInfo("L FIMBA", EMonsterRanks.L, 1, (80,80), (0, 0)) }
    };

    private static readonly Dictionary<ETournamentPools, TournamentRuleset> Tournaments = new Dictionary<ETournamentPools, TournamentRuleset>() {
        { ETournamentPools.L, new TournamentRuleset("Legend", [EMonsterRanks.L], [], [], 1, EMonsterRegion.IMA) },
        { ETournamentPools.M, new TournamentRuleset("Major 4",[EMonsterRanks.M], [], [], 6, EMonsterRegion.IMA) },
        { ETournamentPools.S, new TournamentRuleset("S Rank", [EMonsterRanks.S], [], [], 8, EMonsterRegion.IMA) },
        { ETournamentPools.A, new TournamentRuleset("A Rank", [EMonsterRanks.A], [], [], 8, EMonsterRegion.IMA) },
        { ETournamentPools.B, new TournamentRuleset("B Rank", [EMonsterRanks.B], [], [], 10, EMonsterRegion.IMA) },
        { ETournamentPools.C, new TournamentRuleset("C Rank", [EMonsterRanks.C], [], [], 10, EMonsterRegion.IMA) },
        { ETournamentPools.D, new TournamentRuleset("D Rank", [EMonsterRanks.D], [], [], 8, EMonsterRegion.IMA) },
        { ETournamentPools.E, new TournamentRuleset("E Rank", [EMonsterRanks.E], [], [], 6, EMonsterRegion.IMA) },


        { ETournamentPools.A_Phoenix, new TournamentRuleset("A - Phoenix", [EMonsterRanks.A],
            [MonsterGenus.Phoenix], [MonsterGenus.Phoenix],
            3, EMonsterRegion.IMA) },

        { ETournamentPools.A_DEdge, new TournamentRuleset("A - Double Edge", [EMonsterRanks.A],
            [MonsterGenus.Durahan], [], 1, EMonsterRegion.IMA) },

        { ETournamentPools.B_Dragon, new TournamentRuleset("B - Dragon Tusk", [EMonsterRanks.B],
            [MonsterGenus.Dragon], [], 1, EMonsterRegion.IMA) },

        { ETournamentPools.F_Hero, new TournamentRuleset("F - Hero", [EMonsterRanks.B, EMonsterRanks.A],
            [   MonsterGenus.Baku, MonsterGenus.Centaur, MonsterGenus.ColorPandora,
                MonsterGenus.Ducken, MonsterGenus.Gali, MonsterGenus.Hare, MonsterGenus.Henger,
                MonsterGenus.Mocchi, MonsterGenus.Niton, MonsterGenus.Tiger, MonsterGenus.Undine], [],
            5, EMonsterRegion.IMA) },

        { ETournamentPools.F_Heel, new TournamentRuleset("F - Heel", [EMonsterRanks.B, EMonsterRanks.A],
            [   MonsterGenus.Ape, MonsterGenus.Dragon, MonsterGenus.Joker, MonsterGenus.Kato,
                MonsterGenus.Monol, MonsterGenus.Naga, MonsterGenus.Pixie, MonsterGenus.Suezo, MonsterGenus.Wracky], [],
            5, EMonsterRegion.IMA) },


        { ETournamentPools.F_Elder, new TournamentRuleset("F - Elder", [EMonsterRanks.A],
            [   MonsterGenus.Plant, MonsterGenus.Mew, MonsterGenus.Ape,
                MonsterGenus.Arrowhead, MonsterGenus.Durahan, MonsterGenus.ColorPandora,
                MonsterGenus.Mock, MonsterGenus.Wracky], [],
            3, EMonsterRegion.IMA) },

        //{ ETournamentPools.L_FIMBA, new TournamentRuleset("L FIMBA", [EMonsterRanks.L], [], [], 1, EMonsterRegion.IMA) },
        { ETournamentPools.S_FIMBA, new TournamentRuleset("S FIMBA", [EMonsterRanks.S], [], [], 1, EMonsterRegion.FIMBA) },
        { ETournamentPools.A_FIMBA, new TournamentRuleset("A FIMBA", [EMonsterRanks.A], [], [], 1, EMonsterRegion.FIMBA) },
        { ETournamentPools.B_FIMBA, new TournamentRuleset("B FIMBA", [EMonsterRanks.B], [], [], 1, EMonsterRegion.FIMBA) },
        { ETournamentPools.C_FIMBA, new TournamentRuleset("C FIMBA", [EMonsterRanks.C], [], [], 1, EMonsterRegion.FIMBA) },
        { ETournamentPools.D_FIMBA, new TournamentRuleset("D FIMBA", [EMonsterRanks.D], [], [], 1, EMonsterRegion.FIMBA) },
    };

    private static readonly Dictionary<ETournamentPools, MonsterGenus[]> RestrictMainBreeds = new()
    {
        { ETournamentPools.A_Phoenix, [MonsterGenus.Phoenix] },
        { ETournamentPools.B_Dragon, [MonsterGenus.Dragon] },
        { ETournamentPools.A_DEdge, [MonsterGenus.Durahan] },
        {
            ETournamentPools.F_Elder, [
                MonsterGenus.Plant, MonsterGenus.Mew, MonsterGenus.Ape,
                MonsterGenus.Arrowhead, MonsterGenus.Durahan, MonsterGenus.ColorPandora,
                MonsterGenus.Mock, MonsterGenus.Wracky
            ]
        },
        {
            ETournamentPools.F_Hero, [
                MonsterGenus.Baku, MonsterGenus.Centaur, MonsterGenus.ColorPandora,
                MonsterGenus.Ducken, MonsterGenus.Gali, MonsterGenus.Hare, MonsterGenus.Henger,
                MonsterGenus.Mocchi, MonsterGenus.Niton, MonsterGenus.Tiger, MonsterGenus.Undine
            ]
        },
        {
            ETournamentPools.F_Heel, [
                MonsterGenus.Ape, MonsterGenus.Dragon, MonsterGenus.Joker, MonsterGenus.Kato,
                MonsterGenus.Monol, MonsterGenus.Naga, MonsterGenus.Pixie, MonsterGenus.Suezo, MonsterGenus.Wracky
            ]
        }
    };

    private static readonly Dictionary<ETournamentPools, MonsterGenus[]> RestrictSubBreeds = new()
    {
        { ETournamentPools.A_Phoenix, [MonsterGenus.Phoenix] }
    };

    private static readonly MonsterGenus[] SpecialSubs =
    [
        MonsterGenus.XX, MonsterGenus.XY, MonsterGenus.XZ,
        MonsterGenus.YX, MonsterGenus.YY, MonsterGenus.YZ
    ];

    public readonly TournamentInfo Info = Tourneys[pool];

    public readonly ETournamentPools Pool = pool;

    private Range<int> RankRange => Info.Rank switch
    {
        EMonsterRanks.L => (conf.RankM4, 9999),
        EMonsterRanks.M => (conf.RankS, conf.RankM4),
        EMonsterRanks.S => (conf.RankA, conf.RankS),
        EMonsterRanks.A => (conf.RankB, conf.RankA),
        EMonsterRanks.B => (conf.RankC, conf.RankB),
        EMonsterRanks.C => (conf.RankD, conf.RankC),
        EMonsterRanks.D => (conf.RankE, conf.RankD),
        EMonsterRanks.E => (conf.RankZ, conf.RankE),
        _ => throw new ArgumentOutOfRangeException()
    };

    private int StatStart => Math.Clamp(Info.StatOffset.Min + RankRange.Min, 1, 9999);
    private int StatEnd => Math.Clamp(Info.StatOffset.Max + RankRange.Max, 1, 9999);

    /// <summary>
    ///     This function promotes two sets of monsters.
    ///     1. A random monster from the class, extremely weighted towards any monsters approaching the top of the soft stat
    ///     cap.
    ///     2. Any monsters remaining in the class that are over the soft stat cap by a small amount.
    /// </summary>
    public void MonstersPromoteToNewPool(TournamentPool newPool)
    {
        Logger.Info("Promoting monsters from " + Info.Name + " to " + newPool.Info.Name, Color.LightBlue);
        var stattotal = 0;
        // Find all monsters in this pool
        var monsters = tournament.Monsters
            .FindAll(m => m.Pools.Select(p => p.Pool).Contains(Pool));
        if (monsters.Count == 0)
        {
            Logger.Debug($"Pool {Pool} is empty! nothing to promote");
            return;
        }

        var promoted = monsters[0];

        foreach (var mon in monsters)
            stattotal += Math.Max(mon.StatTotal - (StatEnd - 100), 1);

        if (stattotal > 50)
        {
            stattotal = Random.Shared.Next(stattotal);
            foreach (var monster in monsters)
            {
                var mvalue = Math.Max(monster.StatTotal - (StatEnd - 100), 1);

                stattotal -= mvalue;
                promoted = monster;
                if (stattotal <= 0) break;
            }

            MonsterPromoteToNewPool(promoted, newPool);
            monsters.Remove( promoted );
        }
        else
        {
            Logger.Warn("Tournament Stat Totals dangeorusly low. Growth rates may be too low.");
        }

        // Soft Cap Monster Promotions
        for (var i = monsters.Count - 1; i >= 0; i--)
            if (monsters[i].StatTotal - 100 > StatEnd)
                MonsterPromoteToNewPool(monsters[i], newPool);
    }

    private void MonsterPromoteToNewPool(TournamentMonster monster, TournamentPool newPool)
    {
        monster.LearnTechnique();
        monster.Rank = newPool.Info.Rank;
        monster.Pools.Remove( this ); 
        monster.Pools.Add( newPool );
        Logger.Info( $"{monster.Name} R {monster.Rank}: promoted from {this.Info.Name} to {newPool.Info.Name}", Color.LightBlue );

    }

    public TournamentMonster GenerateNewValidMonster(List<MonsterGenus> available)
    {
        Logger.Info("TP: Getting Breed", Color.AliceBlue);

        var mainRestrictions = RestrictMainBreeds.GetValueOrDefault(Pool) ?? [];
        var subRestrictions = RestrictSubBreeds.GetValueOrDefault(Pool) ?? [];

        var allBreeds = new List<MonsterBreed>(MonsterBreed.AllBreeds);
        Utils.Shuffle(Random.Shared, allBreeds);
        var breed = allBreeds[0];
        foreach (var b in allBreeds)
        {
            if (!available.Contains(b.Main) || !available.Contains(b.Sub)) continue;
            if (mainRestrictions is [] && subRestrictions is [])
            {
                // Tourney has no breed restrictions so check to see if we allow a unique monster
                if (SpecialSubs.Contains(b.Main) &&
                    Random.Shared.NextDouble() < conf.SpeciesUnique)
                    continue;
                breed = b;
                break;
            }

            // If we have a restriction, guarantee we generate a breed with this main or sub
            if ((mainRestrictions is not [] && mainRestrictions.Contains(b.Main)) ||
                (subRestrictions is not [] && subRestrictions.Contains(b.Sub)))
            {
                breed = b;
                break;
            }
        }

        Logger.Info("Breed chosen " + breed.Main + "/" + breed.Sub, Color.AliceBlue);
        return GenerateNewValidMonster(breed);
    }

    private TournamentMonster GenerateNewValidMonster(MonsterBreed breed)
    {
        Logger.Debug("TP: Generating ", Color.AliceBlue);
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

        switch (tournament._config.SpeciesAccuracyTraits) {
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
        Logger.Trace($"TP: Breed " + nm.GenusMain + " " + nm.GenusSub + $" AS:{monData.ArenaSpeed}|GUTS:{monData.GutsRate}", Color.AliceBlue);

        // Attempt to assign three basics, weighted generally towards worse basic techs with variance.
        if (nm.BreedInfo.TechList[0].Type == ErrantryType.Basic)
            nm.MonsterAddTechnique(nm.BreedInfo.TechList[0]);
        for (var tc = 0; tc < 3; tc++)
        {
            var tech = nm.BreedInfo.TechList[0];

            for (var j = 1; j < nm.BreedInfo.TechList.Count; j++)
            {
                var nt = nm.BreedInfo.TechList[j];
                if (nt.Type == ErrantryType.Basic)
                    if (nt.TechValue - Random.Shared.Next(20) < tech.TechValue)
                        tech = nt;
            }

            nm.MonsterAddTechnique(tech);
        }

        // Logger.Trace("TP: Basics Setup " + nm.techs.Count, Color.AliceBlue);

        // This is significantly messing with growth rates across the board. Going to manually set the lifespan afterwards based upon the rank.
        while (nm.StatTotal < StatStart)
            nm.AdvanceMonth();
        Logger.Trace("TP: Stats Generated", Color.AliceBlue);

        for (var i = 0; i < Info.Tier; i++)
            nm.LearnTechnique();
        Logger.Trace("TP: Techs", Color.AliceBlue);

        nm.Lifespan = Info.Rank switch
        {
            // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
            EMonsterRanks.L => (ushort)TournamentData.LifespanRNG.Next( 12, 16 + 1 ),
            EMonsterRanks.M => (ushort)TournamentData.LifespanRNG.Next( 16, 20 + 1 ),
            EMonsterRanks.S => (ushort)TournamentData.LifespanRNG.Next( 20, 26 + 1 ),
            EMonsterRanks.A => (ushort)TournamentData.LifespanRNG.Next( 24, 30 + 1 ),
            EMonsterRanks.B => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next( 14, 22 + 1 ) ),
            EMonsterRanks.C => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next( 6, 12 ) ),
            EMonsterRanks.D => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next( 2, 6 + 1) ),
            EMonsterRanks.E => nm.LifeTotal,
            _ => nm.Lifespan
        };
        nm.Alive = true;

        nm.PromoteToRank(Info.Rank);

        Logger.Debug("TP: Complete", Color.AliceBlue);
        nm.Pools.Add(this);
        return nm;
    }

    public static ETournamentPools PoolFromId(int id)
    {
        if (id is 0 or >= 119)
            Logger.Error($"Pool id {id} out of tourney range");
        return Tourneys
            .First(kvpair => kvpair.Value.Id.Min <= id && id <= kvpair.Value.Id.Max)
            .Key;
    }
}