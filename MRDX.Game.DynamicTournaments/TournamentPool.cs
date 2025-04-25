using System.Drawing;
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
    public int Size => Id.Max - Id.Min;
}

public class TournamentPool(TournamentData tournament, Config conf, ETournamentPools pool)
{
    private static readonly Dictionary<ETournamentPools, TournamentInfo> Tourneys = new()
    {
        { ETournamentPools.S, new TournamentInfo("S Rank", EMonsterRanks.S, 6, (1, 9), (0, 0)) },
        { ETournamentPools.A, new TournamentInfo("A Rank", EMonsterRanks.A, 5, (9, 17), (0, 0)) },
        { ETournamentPools.B, new TournamentInfo("B Rank", EMonsterRanks.B, 4, (17, 27), (0, 0)) },
        { ETournamentPools.C, new TournamentInfo("C Rank", EMonsterRanks.C, 2, (27, 37), (0, 0)) },
        { ETournamentPools.D, new TournamentInfo("D Rank", EMonsterRanks.D, 1, (37, 45), (0, 0)) },
        { ETournamentPools.E, new TournamentInfo("E Rank", EMonsterRanks.E, 0, (45, 51), (0, 0)) },
        { ETournamentPools.X_MOO, new TournamentInfo("L - Moo", EMonsterRanks.L, 9, (51, 52), (250, 9999)) },
        { ETournamentPools.L, new TournamentInfo("Legend", EMonsterRanks.L, 9, (52, 54), (0, 9999)) },
        { ETournamentPools.M, new TournamentInfo("Major 4", EMonsterRanks.M, 7, (54, 62), (0, 0)) },
        { ETournamentPools.A_Phoenix, new TournamentInfo("A - Phoenix", EMonsterRanks.A, 6, (62, 65), (0, 0)) },
        { ETournamentPools.A_DEdge, new TournamentInfo("A - Double Edge", EMonsterRanks.A, 6, (66, 67), (0, 0)) },
        { ETournamentPools.B_Dragon, new TournamentInfo("B - Dragon Tusk", EMonsterRanks.B, 5, (65, 66), (0, 0)) },
        { ETournamentPools.F_Hero, new TournamentInfo("F - Hero", EMonsterRanks.A, 7, (67, 72), (0, 9999)) },
        { ETournamentPools.F_Heel, new TournamentInfo("F - Heel", EMonsterRanks.A, 7, (72, 77), (0, 9999)) },
        { ETournamentPools.F_Elder, new TournamentInfo("F - Elder", EMonsterRanks.A, 8, (77, 80), (0, 9999)) },
        { ETournamentPools.S_FIMBA, new TournamentInfo("S FIMBA", EMonsterRanks.S, 8, (80, 84), (200, 0)) },
        { ETournamentPools.A_FIMBA, new TournamentInfo("A FIMBA", EMonsterRanks.A, 6, (84, 88), (200, 0)) },
        { ETournamentPools.B_FIMBA, new TournamentInfo("B FIMBA", EMonsterRanks.B, 5, (88, 92), (200, 0)) },
        { ETournamentPools.C_FIMBA, new TournamentInfo("C FIMBA", EMonsterRanks.C, 4, (92, 96), (100, 0)) },
        { ETournamentPools.D_FIMBA, new TournamentInfo("D FIMBA", EMonsterRanks.D, 2, (96, 100), (100, 0)) },
        { ETournamentPools.S_FIMBA2, new TournamentInfo("S FIMBA2", EMonsterRanks.S, 8, (100, 104), (200, 0)) },
        { ETournamentPools.A_FIMBA2, new TournamentInfo("A FIMBA2", EMonsterRanks.A, 6, (104, 108), (200, 0)) },
        { ETournamentPools.B_FIMBA2, new TournamentInfo("B FIMBA2", EMonsterRanks.B, 5, (108, 112), (200, 0)) },
        { ETournamentPools.C_FIMBA2, new TournamentInfo("C FIMBA2", EMonsterRanks.C, 4, (112, 116), (100, 0)) },
        { ETournamentPools.D_FIMBA2, new TournamentInfo("D FIMBA2", EMonsterRanks.D, 2, (116, 119), (100, 0)) }
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

    private readonly TournamentData _tournamentData = tournament;

    public readonly TournamentInfo Info = Tourneys[pool];

    public readonly ETournamentPools Pool = pool;

    // public List<TournamentMonster> Monsters = new();

    // public void MonsterAdd(TournamentMonster m)
    // {
    //     if (Monsters.Contains(m)) return;
    //     Monsters.Add(m);
    //     m.pools.Add(this);
    // }
    //
    // public void MonsterRemove(TournamentMonster m)
    // {
    //     if (!Monsters.Contains(m)) return;
    //     Monsters.Remove(m);
    //     m.pools.Remove(this);
    // }

    // private Range<int> StatRangeFromConf()
    // {
    //     switch
    // }

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
        var monsters = _tournamentData.Monsters
            .FindAll(m => m.pools.Select(p => p.Pool).Contains(Pool));
        var promoted = monsters[0];

        var (stat_start, stat_end) = Math.Clamp(Info.StatOffset.Max + conf.), 1, 9999);

        foreach (var mon in monsters)
            stattotal += Math.Max(mon.StatTotal - -100, 1);

        if (stattotal > 50)
        {
            stattotal = Random.Shared.Next(stattotal);
            for (var i = 0; i < monsters.Count; i++)
            {
                var mvalue = Math.Max(monsters[i].StatTotal - (stat_end - 100), 1);
                ;
                stattotal -= mvalue;
                promoted = monsters[i];
                if (stattotal <= 0) break;
            }

            MonsterPromoteToNewPool(promoted, newPool);
        }
        else
        {
            Logger.Info("Tournament Stat Totals dangeorusly low. Growth rates may be too low.", Color.Yellow);
        }

        for (var i = monsters.Count - 1; i >= 0; i--)
            if (monsters[i].StatTotal - 100 > stat_end)
                MonsterPromoteToNewPool(monsters[i], newPool);
    }

    // private void MonsterPromoteToNewPool(TournamentMonster monster, TournamentPool newPool)
    // {
    //     monster.LearnTechnique();
    //     monster.Rank = newPool.Rank;
    //
    //     MonsterRemove(monster);
    //     newPool.MonsterAdd(monster);
    //     Logger.Info(monster.monster.name + " promoted.", Color.LightBlue);
    // }

    public TournamentMonster GenerateNewValidMonster(List<MonsterGenus> available)
    {
        Logger.Info("TP: Getting Breed", Color.AliceBlue);

        var mainRestrictions = RestrictMainBreeds.GetValueOrDefault(Pool) ?? [];
        var subRestrictions = RestrictSubBreeds.GetValueOrDefault(Pool) ?? [];

        var allBreeds = new List<MonsterBreed>(IMonster.AllBreeds);
        Utils.Shuffle(Random.Shared, allBreeds);
        var breed = allBreeds[0];
        foreach (var b in allBreeds)
        {
            if (!available.Contains(breed.Main) || !available.Contains(breed.Sub)) continue;
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
        Logger.Debug("TP: Generating", Color.AliceBlue);
        var nm = new TournamentMonster
        {
            GenusMain = breed.Main,
            GenusSub = breed.Sub,
            Name = TournamentData.RandomNameList[Random.Shared.Next(TournamentData.RandomNameList.Length)],
            Life = 80,
            Power = 1,
            Skill = 1,
            Speed = 1,
            Defense = 1,
            Intelligence = 1,
            Nature = (sbyte)Random.Shared.Next(255),
            Fear = (byte)Random.Shared.Next(25),
            Spoil = (byte)Random.Shared.Next(25),
            ArenaSpeed = (byte)Random.Shared.Next(4), // TODO: Where does this come from?
            GutsRate = (byte)Random.Shared.Next(7, 21), // 7 - 20?
            BattleSpecial = (BattleSpecials)Random.Shared.Next(4)
        };
        Logger.Trace("TP: Breed " + nm.GenusMain + " " + nm.GenusMain, Color.AliceBlue);

        // Attempt to assign three basics, weighted generally towards worse basic techs with variance.
        if (nm.BreedInfo.TechList[0].Type == ErrantryType.Basic)
            nm.MonsterAddTechnique(nm.BreedInfo.TechList[0]);
        // nm.techniques.Add(nm.BreedInfo._techniques[0]);
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
        while (nm.StatTotal < stat_start)
            nm.AdvanceMonth();
        Logger.Trace("TP: Stats Generated", Color.AliceBlue);

        for (var i = 0; i < tournament_tier; i++)
            nm.LearnTechnique();
        Logger.Trace("TP: Techs", Color.AliceBlue);

        nm.Lifespan = Info.Rank switch
        {
            // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
            EMonsterRanks.L => (ushort)TournamentData.LifespanRNG.Next(12, 17),
            EMonsterRanks.M => (ushort)TournamentData.LifespanRNG.Next(16, 21),
            EMonsterRanks.S => (ushort)TournamentData.LifespanRNG.Next(20, 27),
            EMonsterRanks.A => (ushort)TournamentData.LifespanRNG.Next(24, 31),
            EMonsterRanks.B => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next(14, 23)),
            EMonsterRanks.C => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next(6, 13)),
            EMonsterRanks.D => (ushort)(nm.LifeTotal - TournamentData.LifespanRNG.Next(2, 7)),
            EMonsterRanks.E => nm.LifeTotal,
            _ => nm.Lifespan
        };
        nm.Alive = true;

        nm.PromoteToRank(_monsterRank);

        Logger.Debug("TP: Complete", Color.AliceBlue);
        return nm;
    }

    public static ETournamentPools PoolFromId(int id)
    {
        if (id is 0 or > 119)
            Logger.Error($"Pool id {id} out of tourney range");
        return Tourneys
            .First(kvpair => kvpair.Value.Id.Min <= id && id < kvpair.Value.Id.Max)
            .Key;
    }

    /// <summary>
    ///     Adds the tournament participants to the provided list. This is a random selection of [_minimumSize] monsters.
    /// </summary>
    /// <param name="participants"></param>
    public void AddTournamentParticipants(List<TournamentMonster> participants)
    {
        for (var i = 0; i < Info.Size; i++)
            participants.Add(Monsters[i]);
    }
}