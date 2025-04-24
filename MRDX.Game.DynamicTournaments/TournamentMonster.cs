using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public class TournamentMonster : BattleMonsterData, IBattleMonsterData
{
    private readonly GrowthGroups _growthGroup;
    private readonly byte _growthIntensity;
    private readonly ushort _growthRate;
    private readonly Config.TechInt _trainerIntelligence;
    private readonly ushort lifetotal;
    private byte _growthDef;
    private byte _growthInt;

    private byte _growthLif;
    private byte _growthPow;
    private byte _growthSki;
    private byte _growthSpd;

    public bool Alive = true;
    public MonsterBreed BreedInfo;

    private List<byte> growth_options;
    private ushort lifespan;

    public List<TournamentPool> pools = new();

    public EMonsterRanks Rank;
    public List<IMonsterTechnique> techniques = [];

    public TournamentMonster(Config config, IBattleMonsterData m) : base(m)
    {
        Logger.Info("Creating monster from game data.", Color.Lime);
        BreedInfo = MonsterBreed.GetBreedInfo(GenusMain, GenusSub);

        var lifespanmin = config.LifespanMin;

        lifetotal = (ushort)(lifespanmin +
                             TournamentData.LifespanRNG.Next(1 + config.LifespanMax -
                                                             lifespanmin)); // Configuration File

        lifespan = lifetotal;
        lifespan -= (ushort)(4 * (StatTotal /
                                  500)); // Take an arbitrary amount of life off for starting stats.

        _growthRate = (ushort)config.GrowthMonthly;
        for (var i = 0; i < 4; i++)
        {
            _growthRate -= (ushort)config.GrowthVariance;
            _growthRate += (ushort)TournamentData.GrowthRNG.Next(2 * config.GrowthVariance);
        }

        _growthRate = Math.Clamp(_growthRate, (ushort)(config.GrowthMonthly / 2), (ushort)(config.GrowthMonthly * 3));
        Logger.Debug("Growth Post Variance" + _growthRate, Color.Lime);

        // This section applies special bonuses to monster breeds. These numbers needed to be way lower. Double Rare species were busted.
        var bonuses = new List<MonsterGenus>();
        var bonuses2 = new List<MonsterGenus>();
        bonuses.AddRange([
            MonsterGenus.Dragon, MonsterGenus.Centaur, MonsterGenus.Beaclon, MonsterGenus.Henger, MonsterGenus.Wracky,
            MonsterGenus.Durahan, MonsterGenus.Gali, MonsterGenus.Zilla, MonsterGenus.Bajarl, MonsterGenus.Phoenix,
            MonsterGenus.Metalner, MonsterGenus.Jill, MonsterGenus.Joker, MonsterGenus.Undine, MonsterGenus.Mock,
            MonsterGenus.Unknown1, MonsterGenus.Unknown2, MonsterGenus.Unknown3, MonsterGenus.Unknown4,
            MonsterGenus.Unknown5, MonsterGenus.Unknown6
        ]);

        bonuses2.AddRange([
            MonsterGenus.Dragon, MonsterGenus.Centaur, MonsterGenus.Beaclon, MonsterGenus.Durahan, MonsterGenus.Zilla,
            MonsterGenus.Phoenix, MonsterGenus.Metalner, MonsterGenus.Joker, MonsterGenus.Undine,
            MonsterGenus.Unknown1, MonsterGenus.Unknown2, MonsterGenus.Unknown3, MonsterGenus.Unknown4,
            MonsterGenus.Unknown5, MonsterGenus.Unknown6
        ]);

        if (bonuses.Contains(GenusMain)) _growthRate += 3;
        if (bonuses2.Contains(GenusMain)) _growthRate += 3;
        if (bonuses.Contains(GenusSub)) _growthRate += 2;
        if (bonuses2.Contains(GenusSub)) _growthRate += 2;

        _growthRate = (ushort)(1 + _growthRate / 4); // Account for Prime Bonus, 4x
        Logger.Debug("Growth Post Prime" + _growthRate, Color.Lime);

        Logger.Info(
            "A new monster has entered the fray: " + m.Name + ", " + m.GenusMain + "/" + m.GenusSub + ", LIFE: " +
            lifespan + ", GROWTH: " + _growthRate, Color.Lime);

        _growthGroup = (GrowthGroups)(TournamentData.GrowthRNG.Next() % 6);
        _growthIntensity = (byte)(TournamentData.GrowthRNG.Next() % 6);

        SetupGrowthOptions(config);

        _trainerIntelligence = config.TechIntelligence;
        if (Random.Shared.Next(10) == 0)
            _trainerIntelligence = (Config.TechInt)Random.Shared.Next(4);

        // Read through the techniques uint to add the correct technique IDs. TODO: I should have just done a static list and had an invalid tech slot in the empty ones.
        for (var i = 0; i < 24; i++)
            if (((IBattleMonsterData)this).TechSlot.HasFlag((TechSlots)i))
                for (var j = 0; j < BreedInfo._techniques.Count; j++)
                    if (BreedInfo._techniques[j]._id == i)
                        MonsterAddTechnique(BreedInfo._techniques[j]);
    }

    public TournamentMonster(byte[] rawabd) : base(IBattleMonsterData.FromBytes(rawabd[40..100]))
    {
        Logger.Info("Loading monster from ABD Save File.", Color.Lime);
        var rawmonster = new byte[60];
        rawabd[40..100].CopyTo(rawmonster, 0);

        BreedInfo = MonsterBreed.GetBreedInfo(GenusMain, GenusSub);

        lifetotal = rawabd[0];
        lifespan = rawabd[2];
        _growthRate = rawabd[4];
        _growthGroup = (GrowthGroups)rawabd[6];
        _growthIntensity = rawabd[7];

        Rank = (EMonsterRanks)rawabd[8];

        var rawpools = new byte[4];
        rawabd[10..14].CopyTo(rawpools, 0);

        _growthLif = rawabd[14];
        _growthPow = rawabd[15];
        _growthSki = rawabd[16];
        _growthSpd = rawabd[17];
        _growthDef = rawabd[18];
        _growthInt = rawabd[19];

        growth_options = new List<byte>();
        for (var i = 14; i < 19; i++)
        {
            if (rawabd[i] == 0) rawabd[i] = 1;
            for (var j = 0; j < rawabd[i]; j++) growth_options.Add((byte)(i - 14));
        }

        _trainerIntelligence = (Config.TechInt)rawabd[20];

        // Read through the techniques uint to add the correct technique IDs. TODO: I should have just done a static list and had an invalid tech slot in the empty ones.
        for (var i = 0; i < 24; i++)
            if (TechSlot.HasFlag((TechSlots)i))
                for (var j = 0; j < BreedInfo._techniques.Count; j++)
                    if (BreedInfo._techniques[j]._id == i)
                        MonsterAddTechnique(BreedInfo._techniques[j]);

        // DEBUG DEBUG DEBUG
        // for ( var i = 0; i < techniques.Count; i++ ) { TournamentData._mod.DebugLog( 2, monster.name + " has " + techniques[ i ], Color.Orange ); }
    }

    private void SetupGrowthOptions(Config config)
    {
        // Life, Pow, Skill, Speed, Def, Int

        growth_options = new List<byte>();
        byte[] gopts = [0];

        if (_growthGroup == GrowthGroups.Balanced)
        {
            gopts = [5, 5, 5, 5, 5, 5];

            if (_growthIntensity == 1)
                gopts = [6, 4, 6, 4, 4, 4];
            else if (_growthIntensity == 2)
                gopts = [4, 4, 4, 6, 6, 4];
            else if (_growthIntensity == 3)
                gopts = [4, 6, 4, 4, 4, 6];
            else if (_growthIntensity == 4)
                gopts = [5, 6, 4, 4, 4, 4];
            else if (_growthIntensity == 5) gopts = [5, 4, 4, 4, 4, 6];
        }

        else if (_growthGroup == GrowthGroups.Power)
        {
            gopts = [4, 5, 3, 1, 3, 2];

            if (_growthIntensity == 1)
                gopts = [8, 11, 7, 2, 6, 3];
            else if (_growthIntensity == 2)
                gopts = [10, 14, 8, 2, 6, 3];
            else if (_growthIntensity == 3)
                gopts = [12, 18, 10, 2, 7, 2];
            else if (_growthIntensity == 4)
                gopts = [8, 2, 8, 2, 6, 11];
            else if (_growthIntensity == 5) gopts = [11, 2, 10, 4, 6, 18];
        }

        else if (_growthGroup == GrowthGroups.Intel)
        {
            gopts = [3, 1, 3, 4, 1, 5];

            if (_growthIntensity == 1)
                gopts = [5, 2, 6, 5, 3, 9];
            else if (_growthIntensity == 2)
                gopts = [8, 2, 9, 6, 3, 12];
            else if (_growthIntensity == 3)
                gopts = [12, 2, 12, 8, 3, 16];
            else if (_growthIntensity == 4)
                gopts = [8, 12, 9, 6, 3, 2];
            else if (_growthIntensity == 5) gopts = [12, 16, 12, 8, 3, 2];
        }

        else if (_growthGroup == GrowthGroups.Defend)
        {
            gopts = [5, 3, 4, 3, 5, 3];

            if (_growthIntensity == 1)
                gopts = [7, 3, 5, 4, 6, 3];
            else if (_growthIntensity == 2)
                gopts = [10, 4, 8, 5, 11, 4];
            else if (_growthIntensity == 3)
                gopts = [12, 4, 10, 6, 13, 4];
            else if (_growthIntensity == 4)
                gopts = [10, 4, 8, 10, 10, 4];
            else if (_growthIntensity == 5) gopts = [12, 4, 10, 13, 13, 4];
        }

        else if (_growthGroup == GrowthGroups.Wither)
        {
            gopts = [4, 3, 6, 5, 2, 4];

            if (_growthIntensity == 1)
                gopts = [5, 3, 9, 7, 2, 5];
            else if (_growthIntensity == 2)
                gopts = [6, 4, 13, 8, 2, 5];
            else if (_growthIntensity == 3)
                gopts = [9, 4, 15, 9, 3, 5];
            else if (_growthIntensity == 4)
                gopts = [6, 4, 13, 8, 2, 5];
            else if (_growthIntensity == 5) gopts = [9, 4, 15, 9, 3, 5];
        }

        else if (_growthGroup == GrowthGroups.Speedy)
        {
            gopts = [4, 4, 5, 6, 2, 3];

            if (_growthIntensity == 1)
                gopts = [5, 4, 5, 8, 2, 3];
            else if (_growthIntensity == 2)
                gopts = [6, 4, 7, 10, 2, 4];
            else if (_growthIntensity == 3)
                gopts = [7, 5, 10, 14, 2, 4];
            else if (_growthIntensity == 4)
                gopts = [8, 2, 10, 13, 2, 8];
            else if (_growthIntensity == 5) gopts = [7, 8, 10, 13, 4, 2];
        }

        // This is some fun variance. 16% chance of a stat (1 on average) getting a slight penalty or boost.
        // CONFIG: Wildcard chance of a stat being effectively completely randomized with no rhyme or reason. (Approximately 1/50 monsters have a stat altered in this way at 300).
        for (var i = 0; i < gopts.Length; i++)
        {
            if (Random.Shared.Next(6) == 0) gopts[i] = (byte)(gopts[i] - Random.Shared.Next(1, 6));
            if (Random.Shared.Next(config.Wildcard) == 0)
            {
                if (i == 0 || i == 2)
                    gopts[i] = (byte)Random.Shared.Next(4, 21);
                else
                    gopts[i] = (byte)Random.Shared.Next(1, 21);
            }

            if (gopts[i] == 0) gopts[i] = 1;
        }

        _growthLif = gopts[0];
        _growthPow = gopts[1];
        _growthSki = gopts[2];
        _growthSpd = gopts[3];
        _growthDef = gopts[4];
        _growthInt = gopts[5];

        for (var i = 0; i < gopts.Length; i++)
        for (var j = 0; j < gopts[i]; j++)
            growth_options.Add((byte)i);
    }

    public void AdvanceMonth()
    {
        var agegroup = 1;
        lifespan--;
        if (lifespan == 0) Alive = false;

        // Added an additonal prime grouping. This can result in a situation where a monster gets like, 2-3 months of optimal growth for ultra short life creatures but whatever, that's the price they pay. Longer life is better in this world objectively. Sorry! :(
        if (lifetotal - lifespan > 9) agegroup++;
        if (lifetotal - lifespan > 15) agegroup++;
        if (lifetotal - lifespan > 22) agegroup++;
        if (lifespan < 16) agegroup--;
        if (lifespan < 6) agegroup -= 2;

        agegroup *= _growthRate;

        Logger.Trace(
            "Monster " + Name + " Advancing Month: [STATS: " + StatTotal + ", GROWTH:" + _growthRate +
            " CGROW: " + agegroup + ", LIFE: " + lifespan + "]", Color.Yellow);

        for (var i = 0; i < agegroup; i++)
        {
            var stat = growth_options[TournamentData.GrowthRNG.Next() % growth_options.Count()];

            if (stat == 0 && Life <= 999)
                Life++;
            else if (stat == 1 && Power <= 999)
                Power++;
            else if (stat == 2 && Skill <= 999)
                Skill++;
            else if (stat == 3 && Speed <= 999)
                Speed++;
            else if (stat == 4 && Defense <= 999)
                Defense++;
            else if (stat == 5 && Intelligence <= 999)
                Intelligence++;
            else
                i -= TournamentData.GrowthRNG.Next(5) > 0 ? 1 : 0;
        }

        if (Fear < 100) Fear += (byte)(TournamentData.GrowthRNG.Next() % 2);
        if (Spoil < 100) Spoil += (byte)(TournamentData.GrowthRNG.Next() % 2);

        Logger.Trace(
            "Monster " + Name + " Completed Growth: [STATS: " + StatTotal + ", GROWTH:" +
            _growthRate +
            ", LIFE: " + lifespan + ", ALIVE: " + Alive + "]", Color.Yellow);
    }

    public void LearnTechnique()
    {
        // TODO: Smarter Logic About which tech to get
        Logger.Trace(
            "Monster " + Name + " attempting to learn technique. They have " + techniques.Count + " | " +
            TechSlot + " now.", Color.Orange);

        var techint = _trainerIntelligence;
        var tech = BreedInfo._techniques[0];
        var techvariance = 25;
        if (techint == Config.TechInt.Minimal)
            techvariance = 30;
        else if (techint == Config.TechInt.Average)
            techvariance = 25;
        else if (techint == Config.TechInt.Smart)
            techvariance = 15;
        else if (techint == Config.TechInt.Genius) techvariance = 10;

        var weightedLearnPool = new List<int>();

        var missingRanges = new List<TechRange> { TechRange.Melee, TechRange.Short, TechRange.Medium, TechRange.Long };
        for (var i = 0; i < techniques.Count; i++) missingRanges.Remove(techniques[i].Range);

        for (var i = 0; i < BreedInfo._techniques.Count; i++)
        {
            tech = BreedInfo._techniques[i];

            if (!techniques.Contains(tech))
            {
                double techval = tech._techValue + Random.Shared.Next() % techvariance;

                if (missingRanges.Contains(tech._range)) techval += 20;
                if (techint != Config.TechInt.Minimal)
                {
                    if (tech._scaling == TechType.Power && Power < Intelligence)
                        techval = techval * 0.8 * ((float)Power / Intelligence);

                    else if (tech._scaling == TechType.Intelligence && Intelligence < Power)
                        techval = techval * 0.8 * ((float)Intelligence / Power);
                }

                if (tech._errantry == ErrantryType.Basic && techniques.Count <= 4) techval *= 2;
                if (_growthGroup == GrowthGroups.Power && tech._errantry == ErrantryType.Heavy)
                    techval *= 2;
                else if (_growthGroup == GrowthGroups.Intel && tech._errantry == ErrantryType.Skill)
                    techval *= 2;
                else if (_growthGroup == GrowthGroups.Wither && tech._errantry == ErrantryType.Withering)
                    techval *= 2;
                else if (_growthGroup == GrowthGroups.Speedy && tech._errantry == ErrantryType.Sharp)
                    techval *= 2;

                if (tech._errantry == ErrantryType.Special)
                {
                    if (Rank is EMonsterRanks.S or EMonsterRanks.A or EMonsterRanks.B)
                        techval *= 2;
                    else
                        techval *= 0.2;
                }

                if (techint == Config.TechInt.Smart || techint == Config.TechInt.Genius)
                    techval = (int)(techval * 1.1);

                Logger.Debug(techval + " TV: " + tech, Color.Beige);
                for (var j = 10; j < techval; j++) weightedLearnPool.Add(tech._id);
            }
        }

        if (weightedLearnPool.Count > 0)
        {
            var chosen = weightedLearnPool[Random.Shared.Next() % weightedLearnPool.Count];
            for (var i = 0; i < BreedInfo._techniques.Count; i++)
                if (BreedInfo._techniques[i]._id == chosen)
                    tech = BreedInfo._techniques[i];

            MonsterAddTechnique(tech);
            Logger.Info(
                "Monster " + Name + " learned " + tech + " they have " + techniques.Count + "|" +
                TechSlot + " now.", Color.Orange);
        }

        if (techint == Config.TechInt.Genius && techniques.Count > 8 &&
            Random.Shared.Next() % 20 < techniques.Count) UnlearnTechnique();
    }

    public void UnlearnTechnique()
    {
        Logger.Info("Monster " + Name + " is attempting an unlearn a tech.", Color.Orange);

        var weightedPool = new List<int>();
        var minVal = 1000;
        var tech = techniques[0];

        int[] rangeCounts = [0, 0, 0, 0];
        var techweights = new double[techniques.Count];

        for (var i = 0; i < techniques.Count; i++) rangeCounts[(int)techniques[i].Range] += 1;

        for (var i = 0; i < techniques.Count; i++)
        {
            tech = techniques[i];
            var techval = tech.TechValue;

            if (tech.Scaling == TechType.Power && Power < Intelligence)
                techval = techval * 0.8 * ((float)Power / Intelligence);

            else if (tech.Scaling == TechType.Intelligence && Intelligence < Power)
                techval = techval * 0.8 * ((float)Intelligence / Power);

            if (tech.Type == ErrantryType.Special)
                techval *= 1.25;
            else if (_growthGroup == GrowthGroups.Power && tech.Type == ErrantryType.Heavy)
                techval *= 1.25;
            else if (_growthGroup == GrowthGroups.Intel && tech.Type == ErrantryType.Skill)
                techval *= 1.25;
            else if (_growthGroup == GrowthGroups.Wither && tech.Type == ErrantryType.Withering)
                techval *= 1.25;
            else if (_growthGroup == GrowthGroups.Speedy && tech.Type == ErrantryType.Sharp) techval *= 1.25;

            if (rangeCounts[(int)tech.Range] <= 1) techval += 30;

            if (minVal > techval) minVal = (int)techval;

            techweights[i] = techval;
            Logger.Debug(tech.Slot + " has a value of " + techval, Color.Orange);
        }

        for (var i = 0; i < techniques.Count; i++)
        {
            tech = techniques[i];
            var weight = 30 - (techweights[i] - minVal);
            for (var j = 0; j < weight; j++) weightedPool.Add(tech.Slot);
        }

        if (weightedPool.Count > 0)
        {
            var chosen = weightedPool[Random.Shared.Next() % weightedPool.Count];
            for (var i = 0; i < BreedInfo._techniques.Count; i++)
                if (BreedInfo._techniques[i]._id == chosen)
                    tech = BreedInfo._techniques[i];

            MonsterRemoveTechnique(tech);
            Logger.Warn(
                "Monster " + Name + " has unlearned " + tech + " they have " + techniques.Count + " | " +
                TechSlot + " now.", Color.Orange);
        }
    }

    public void MonsterAddTechnique(IMonsterTechnique tech)
    {
        // set the bit at this slot
        TechSlot |= (TechSlots)(1 << tech.Slot);
        if (!techniques.Contains(tech)) techniques.Add(tech);
    }

    public void MonsterRemoveTechnique(IMonsterTechnique tech)
    {
        // clear the bit at this slot
        TechSlot &= ~(TechSlots)(1 << tech.Slot);
        techniques.Remove(tech);
    }

    /// <summary>
    ///     Promotes a Monster to a specifc rank, learning specials at D and A ranks.
    ///     Relies on the order of the enum to work properly!
    /// </summary>
    public void PromoteToRank(EMonsterRanks rank)
    {
        if (rank >= Rank)
            Rank = rank;

        else
            while (Rank != rank)
            {
                Rank--;
                if (Rank == EMonsterRanks.A) LearnBattleSpecial();
                if (Rank == EMonsterRanks.D) LearnBattleSpecial();
            }
    }

    public void LearnBattleSpecial()
    {
        BattleSpecial |= (BattleSpecials)TournamentData.GrowthRNG.Next(13);
    }

    public byte[] ToSaveFile()
    {
        // ABD_TournamentMonster data will consist of 40 new bytes, followed by the standard 60 tracked by the game for Tournament Monsters.
        // 0-1, Lifetotal
        // 2-3, Current Remaining Lifespan
        // 4-5, Growth Rate Per Months
        // 6, Growth Group (Enum)
        // 7, Growth Intensity (Enum)
        // 8-9, Monster Rank (Enum)
        // 10-13, TournamentPools (Enums)
        // 14-19, Growth Weights (Generated from 6/7)
        // 20, Trainer Intelligence

        var data = new byte[40 + 60];

        data[0] = (byte)lifetotal;
        data[2] = (byte)lifespan;
        data[4] = (byte)_growthRate;
        data[6] = (byte)_growthGroup;
        data[7] = _growthIntensity;
        data[8] = (byte)Rank;

        data[10] = 0xFF;
        data[11] = 0xFF;
        data[12] = 0xFF;
        data[13] = 0xFF;
        for (var i = 0; i < pools.Count && i < 4; i++)
            data[10 + i] = (byte)pools[i].Pool;

        data[14] = _growthLif;
        data[15] = _growthPow;
        data[16] = _growthSki;
        data[17] = _growthSpd;
        data[18] = _growthDef;
        data[19] = _growthInt;

        data[20] = (byte)_trainerIntelligence;

        Serialize().CopyTo(data, 40);
        return data;
    }

    private enum GrowthGroups
    {
        Balanced,
        Power,
        Intel,
        Defend,
        Wither,
        Speedy
    }
}