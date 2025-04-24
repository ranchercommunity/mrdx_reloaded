using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Configuration;

namespace MRDX.Game.DynamicTournaments;

public record struct TournamentInfo(string Name, EMonsterRanks Rank, int Tier, Range<int> Id, Range<int> StatOffset);

public class TournamentPool(Config conf, ETournamentPools _pool)
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

    public readonly TournamentInfo Info;

    public readonly ETournamentPools Pool = _pool;

    public List<TournamentMonster> Monsters = new();

    public void MonsterAdd(TournamentMonster m)
    {
        if (Monsters.Contains(m)) return;
        Monsters.Add(m);
        m.pools.Add(this);
    }

    public void MonsterRemove(TournamentMonster m)
    {
        if (!Monsters.Contains(m)) return;
        Monsters.Remove(m);
        m.pools.Remove(this);
    }

    private Range<int> StatRangeFromConf()
    {
        switch
    }

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
        var promoted = Monsters[0];

        var (stat_start, stat_end) = Math.Clamp(Info.StatOffset.Max + conf.), 1, 9999);

        foreach (var mon in Monsters)
            stattotal += Math.Max(mon.StatTotal - -100, 1);

        if (stattotal > 50)
        {
            stattotal = Random.Shared.Next(stattotal);
            for (var i = 0; i < Monsters.Count; i++)
            {
                var mvalue = Math.Max(Monsters[i].StatTotal - (stat_end - 100), 1);
                ;
                stattotal -= mvalue;
                promoted = Monsters[i];
                if (stattotal <= 0) break;
            }

            MonsterPromoteToNewPool(promoted, newPool);
        }
        else
        {
            Logger.Info("Tournament Stat Totals dangeorusly low. Growth rates may be too low.", Color.Yellow);
        }

        for (var i = Monsters.Count - 1; i >= 0; i--)
            if (Monsters[i].StatTotal - 100 > stat_end)
                MonsterPromoteToNewPool(Monsters[i], newPool);
    }

    private void MonsterPromoteToNewPool(TournamentMonster monster, TournamentPool newPool)
    {
        monster.LearnTechnique();
        monster.Rank = newPool.Rank;

        MonsterRemove(monster);
        newPool.MonsterAdd(monster);
        Logger.Info(monster.monster.name + " promoted.", Color.LightBlue);
    }

    private static readonly MonsterGenus[] SpecialSubs =
    [
        MonsterGenus.Unknown1, MonsterGenus.Unknown2, MonsterGenus.Unknown3,
        MonsterGenus.Unknown4, MonsterGenus.Unknown5, MonsterGenus.Unknown6
    ];
    public TournamentMonster GenerateNewValidMonster(List<MonsterGenus> available)
    {
        Logger.Info("TP: Getting Breed", Color.AliceBlue);

        var mainRestrictions = RestrictMainBreeds.GetValueOrDefault(_tournamentPool) ?? [];
        var subRestrictions = RestrictSubBreeds.GetValueOrDefault(_tournamentPool) ?? [];

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
        var nmraw = new byte[60]; // This doesn't matter, it gets completely overwritten below anyways.
        var nm = new TournamentMonster();

        nm.GenusMain = breed.Main;
        nm.GenusSub = breed.Sub;
        Logger.Trace("TP: Breed " + nm.GenusMain + " " + nm.GenusMain, Color.AliceBlue);
        nm.Name =
            TournamentData._random_name_list[Random.Shared.Next() % TournamentData._random_name_list.Length];
        nm.Life = 80;
        abdm.monster.stat_pow = 1;
        abdm.monster.stat_ski = 1;
        abdm.monster.stat_spd = 1;
        abdm.monster.stat_def = 1;
        abdm.monster.stat_int = 1;

        abdm.monster.per_nature = (byte)Random.Shared.Next(255);
        abdm.monster.per_fear = (byte)Random.Shared.Next(25);
        abdm.monster.per_spoil = (byte)Random.Shared.Next(25);

        abdm.monster.arena_movespeed = (byte)Random.Shared.Next(4); // TODO: Where does this come from?
        abdm.monster.arena_gutsrate = (byte)Random.Shared.Next(7, 21); // 7 - 20?

        abdm.monster.battle_specials = (byte)Random.Shared.Next(4);

        // Attempt to assign three basics, weighted generally towards worse basic techs with variance.
        if (abdm.BreedInfo._techniques[0]._errantry == ErrantryType.Basic)
        {
            abdm.monster.techniques = abdm.monster.techniques | (uint)(1 << abdm.BreedInfo._techniques[0]._id);
            abdm.techniques.Add(abdm.BreedInfo._techniques[0]);
        }

        for (var tc = 0; tc < 3; tc++)
        {
            IMonsterTechnique tech = abdm.BreedInfo._techniques[0];

            for (var j = 1; j < abdm.BreedInfo._techniques.Count; j++)
            {
                var nt = abdm.BreedInfo._techniques[j];
                if (nt._errantry == ErrantryType.Basic)
                    if (nt._techValue - Random.Shared.Next(20) < tech._techValue)
                        tech = nt;
            }

            abdm.MonsterAddTechnique(tech);
        }

        Logger.Trace("TP: Basics Setup " + abdm.techniques.Count, Color.AliceBlue);

        // This is significantly messing with growth rates across the board. Going to manually set the lifespan afterwards based upon the rank.
        while (abdm.monster.stat_total < stat_start) abdm.AdvanceMonth();
        Logger.Trace("TP: Stats Generated", Color.AliceBlue);

        for (var i = 0; i < tournament_tier; i++) abdm.LearnTechnique();
        Logger.Trace("TP: Techs", Color.AliceBlue);

        abdm.lifespan = Rank switch
        {
            // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
            EMonsterRanks.L => (ushort)TournamentData.LifespanRNG.Next(12, 17),
            EMonsterRanks.M => (ushort)TournamentData.LifespanRNG.Next(16, 21),
            EMonsterRanks.S => (ushort)TournamentData.LifespanRNG.Next(20, 27),
            EMonsterRanks.A => (ushort)TournamentData.LifespanRNG.Next(24, 31),
            EMonsterRanks.B => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(14, 23)),
            EMonsterRanks.C => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(6, 13)),
            EMonsterRanks.D => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(2, 7)),
            EMonsterRanks.E => abdm.lifetotal,
            _ => abdm.lifespan
        };
        abdm.Alive = true;

        abdm.PromoteToRank(_monsterRank);

        Logger.Debug("TP: Complete", Color.AliceBlue);
        MonsterAdd(abdm);
        return abdm;
    }

    public static ETournamentPools PoolFromId(int id)
    {
        if (id is 0 or > 119)
            Logger.Error($"Pool id {id} out of tourney range");
        return id switch
        {
            >= 1 and <= 8 => ETournamentPools.S,
            >= 9 and <= 16 => ETournamentPools.A,
            >= 17 and <= 26 => ETournamentPools.B,
            >= 27 and <= 36 => ETournamentPools.C,
            >= 37 and <= 44 => ETournamentPools.D,
            >= 45 and <= 50 => ETournamentPools.E,
            >= 51 and <= 51 => ETournamentPools.X_MOO,
            >= 52 and <= 53 => ETournamentPools.L,
            >= 54 and <= 61 => ETournamentPools.M,
            >= 62 and <= 64 => ETournamentPools.A_Phoenix,
            >= 65 and <= 65 => ETournamentPools.B_Dragon,
            >= 66 and <= 66 => ETournamentPools.A_DEdge,
            >= 67 and <= 71 => ETournamentPools.F_Hero,
            >= 72 and <= 76 => ETournamentPools.F_Heel,
            >= 77 and <= 79 => ETournamentPools.F_Elder,
            >= 80 and <= 83 => ETournamentPools.S_FIMBA,
            >= 84 and <= 87 => ETournamentPools.A_FIMBA,
            >= 88 and <= 91 => ETournamentPools.B_FIMBA,
            >= 92 and <= 95 => ETournamentPools.C_FIMBA,
            >= 96 and <= 99 => ETournamentPools.D_FIMBA,
            >= 100 and <= 103 => ETournamentPools.S_FIMBA2,
            >= 104 and <= 107 => ETournamentPools.A_FIMBA2,
            >= 108 and <= 111 => ETournamentPools.B_FIMBA2,
            >= 112 and <= 115 => ETournamentPools.C_FIMBA2,
            >= 116 and <= 119 => ETournamentPools.D_FIMBA2,
            _ => ETournamentPools.B
        };
    }

    /// <summary>
    ///     Adds the tournament participants to the provided list. This is a random selection of [_minimumSize] monsters.
    /// </summary>
    /// <param name="participants"></param>
    public void AddTournamentParticipants(List<TournamentMonster> participants)
    {
        for (var i = 0; i < _minimumSize; i++) participants.Add(Monsters[i]);
    }
}