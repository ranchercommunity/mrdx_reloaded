using System.Collections.ObjectModel;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public class TournamentMonster : BattleMonsterData
{
    private readonly GrowthGroups _growthGroup;
    private readonly byte _growthIntensity;
    private readonly ushort _growthRate;
    private readonly Config.TechInt _trainerIntelligence;
    public readonly ushort LifeTotal;
    private byte _growthDef;
    private byte _growthInt;

    private byte _growthLif;

    private List<byte> _growthOptions;
    private byte _growthPow;
    private byte _growthSki;
    private byte _growthSpd;

    public bool Alive = true;
    public MonsterBreed BreedInfo;
    public ushort Lifespan;

    public List<TournamentPool> Pools = [];

    public EMonsterRanks Rank;

    public TournamentMonster(Config config, IBattleMonsterData m) : base(m)
    {
        Logger.Info("Creating monster from game data.", Color.Lime);
        BreedInfo = MonsterBreed.GetBreed(m.GenusMain, m.GenusSub)!;

        var lifespanmin = config.LifespanMin;
        // Configuration File
        LifeTotal = (ushort)(lifespanmin +
                             TournamentData.LifespanRNG.Next(1 + config.LifespanMax - lifespanmin));

        Lifespan = LifeTotal;
        // Take an arbitrary amount of life off for starting stats.
        Lifespan -= (ushort)(4 * (StatTotal / 500));

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
            MonsterGenus.XX, MonsterGenus.XY, MonsterGenus.XZ,
            MonsterGenus.YX, MonsterGenus.YY, MonsterGenus.YZ
        ]);

        bonuses2.AddRange([
            MonsterGenus.Dragon, MonsterGenus.Centaur, MonsterGenus.Beaclon, MonsterGenus.Durahan, MonsterGenus.Zilla,
            MonsterGenus.Phoenix, MonsterGenus.Metalner, MonsterGenus.Joker, MonsterGenus.Undine,
            MonsterGenus.XX, MonsterGenus.XY, MonsterGenus.XZ,
            MonsterGenus.YX, MonsterGenus.YY, MonsterGenus.YZ
        ]);

        if (bonuses.Contains(GenusMain)) _growthRate += 3;
        if (bonuses2.Contains(GenusMain)) _growthRate += 3;
        if (bonuses.Contains(GenusSub)) _growthRate += 2;
        if (bonuses2.Contains(GenusSub)) _growthRate += 2;

        _growthRate = (ushort)(1 + _growthRate / 4); // Account for Prime Bonus, 4x
        Logger.Debug("Growth Post Prime" + _growthRate, Color.Lime);

        Logger.Info(
            "A new monster has entered the fray: " + m.Name + ", " + m.GenusMain + "/" + m.GenusSub + ", LIFE: " +
            Lifespan + ", GROWTH: " + _growthRate, Color.Lime);

        _growthGroup = (GrowthGroups)(TournamentData.GrowthRNG.Next() % 6);
        _growthIntensity = (byte)(TournamentData.GrowthRNG.Next() % 6);

        SetupGrowthOptions(config);

        _trainerIntelligence = config.TechIntelligence;
        if (Random.Shared.Next(10) == 0)
            _trainerIntelligence = (Config.TechInt)Random.Shared.Next(4);
    }

    public TournamentMonster(Dictionary<ETournamentPools, TournamentPool> pools, byte[] rawdtpmonster) : base(
        IBattleMonsterData.FromBytes(rawdtpmonster[40..100]))
    {
        Logger.Debug("Loading monster from DTP Save File.", Color.Lime);

        BreedInfo = MonsterBreed.GetBreed(GenusMain, GenusSub)!;

        LifeTotal = BitConverter.ToUInt16(rawdtpmonster, 0);
        Lifespan = BitConverter.ToUInt16(rawdtpmonster, 2);
        _growthRate = BitConverter.ToUInt16(rawdtpmonster, 4);
        _growthGroup = (GrowthGroups)rawdtpmonster[6];
        _growthIntensity = rawdtpmonster[7];

        Rank = (EMonsterRanks)rawdtpmonster[8];

        var rawpools = new byte[4];
        rawdtpmonster[10..14].CopyTo(rawpools, 0);
        foreach ( var pool in rawpools ) {
            if ( pool != 0xFF ) {
                Pools.Add( pools[ (ETournamentPools) pool ] );
            }
        }

        _growthLif = rawdtpmonster[14];
        _growthPow = rawdtpmonster[15];
        _growthSki = rawdtpmonster[16];
        _growthSpd = rawdtpmonster[17];
        _growthDef = rawdtpmonster[18];
        _growthInt = rawdtpmonster[19];

        _growthOptions = [];
        for (var i = 14; i < 19; i++)
        {
            if (rawdtpmonster[i] == 0) rawdtpmonster[i] = 1;
            for (var j = 0; j < rawdtpmonster[i]; j++) _growthOptions.Add((byte)(i - 14));
        }

        _trainerIntelligence = (Config.TechInt)rawdtpmonster[20];
        // DEBUG DEBUG DEBUG
        // for ( var i = 0; i < techniques.Count; i++ ) { TournamentData._mod.DebugLog( 2, monster.name + " has " + techniques[ i ], Color.Orange ); }
    }

    private ReadOnlyCollection<IMonsterTechnique> TechList =>
        BreedInfo.TechList.FindAll(t => TechSlot.HasFlag(t.Slot)).AsReadOnly();

    private void SetupGrowthOptions(Config config)
    {
        // Life, Pow, Skill, Speed, Def, Int

        _growthOptions = new List<byte>();
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
                gopts = [7, 4, 5, 4, 6, 4];
            else if (_growthIntensity == 2)
                gopts = [10, 8, 8, 5, 11, 2];
            else if (_growthIntensity == 3)
                gopts = [12, 2, 10, 6, 13, 10];
            else if (_growthIntensity == 4)
                gopts = [10, 2, 8, 10, 10, 8];
            else if (_growthIntensity == 5) gopts = [12, 10, 10, 13, 13, 2];


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
            if (Random.Shared.Next(6) == 0)
                gopts[i] = (byte)(gopts[i] - Random.Shared.Next(1, 6));
            if (Random.Shared.Next(config.Wildcard) == 0)
            {
                if (i is 0 or 2)
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
                _growthOptions.Add((byte)i);
    }

    public void AdvanceMonth()
    {
        var agegroup = 1;
        Lifespan--;
        if (Lifespan == 0) Alive = false;

        // Added an additonal prime grouping. This can result in a situation where a monster gets like, 2-3 months of optimal growth for ultra short life creatures but whatever, that's the price they pay. Longer life is better in this world objectively. Sorry! :(
        if (LifeTotal - Lifespan > 9) agegroup++;
        if (LifeTotal - Lifespan > 15) agegroup++;
        if (LifeTotal - Lifespan > 22) agegroup++;
        if (Lifespan < 16) agegroup--;
        if (Lifespan < 6) agegroup -= 2;

        agegroup *= _growthRate;

        Logger.Trace(
            "Monster " + Name + " Advancing Month: [STATS: " + StatTotal + ", GROWTH:" + _growthRate +
            " CGROW: " + agegroup + ", LIFE: " + Lifespan + "]", Color.Yellow);

        for (var i = 0; i < agegroup; i++)
        {
            var stat = _growthOptions[TournamentData.GrowthRNG.Next(_growthOptions.Count)];

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
            ", LIFE: " + Lifespan + ", ALIVE: " + Alive + "]", Color.Yellow);
    }

    public void LearnTechnique()
    {
        // TODO: Smarter Logic About which tech to get
        Logger.Trace(
            "Monster " + Name + " attempting to learn technique. They have " + TechList.Count + " | " +
            TechSlot + " now.", Color.Orange);

        var techint = _trainerIntelligence;
        var tech = BreedInfo.TechList[0];
        var techvariance = techint switch
        {
            Config.TechInt.Minimal => 30,
            Config.TechInt.Average => 25,
            Config.TechInt.Smart => 15,
            Config.TechInt.Genius => 10,
            _ => 25
        };

        var weightedLearnPool = new List<TechSlots>();

        var missingRanges = new List<TechRange> { TechRange.Melee, TechRange.Short, TechRange.Medium, TechRange.Long };
        foreach (var t in TechList)
            missingRanges.Remove(t.Range);

        foreach (var t in BreedInfo.TechList)
        {
            tech = t;

            if (TechList.Contains(tech)) continue;
            var techval = tech.TechValue + Random.Shared.Next() % techvariance;

            if (missingRanges.Contains(tech.Range)) techval += 20;
            if (techint != Config.TechInt.Minimal)
            {
                if (tech.Scaling == TechType.Power && Power < Intelligence)
                    techval = techval * 0.8 * ((float)Power / Intelligence);

                else if (tech.Scaling == TechType.Intelligence && Intelligence < Power)
                    techval = techval * 0.8 * ((float)Intelligence / Power);
            }

            if (tech.Type == ErrantryType.Basic && TechList.Count <= 4) techval *= 2;
            switch (_growthGroup)
            {
                case GrowthGroups.Power when tech.Type == ErrantryType.Heavy:
                case GrowthGroups.Intel when tech.Type == ErrantryType.Skill:
                case GrowthGroups.Wither when tech.Type == ErrantryType.Withering:
                case GrowthGroups.Speedy when tech.Type == ErrantryType.Sharp:
                    techval *= 2;
                    break;
            }

            if (tech.Type == ErrantryType.Special)
            {
                if (Rank is EMonsterRanks.S or EMonsterRanks.A or EMonsterRanks.B)
                    techval *= 2;
                else
                    techval *= 0.2;
            }

            if (techint is Config.TechInt.Smart or Config.TechInt.Genius)
                techval = (int)(techval * 1.1);

            Logger.Debug(techval + " TV: " + tech, Color.Beige);
            for (var j = 10; j < techval; j++)
                weightedLearnPool.Add(tech.Slot);
        }

        if (weightedLearnPool.Count > 0)
        {
            var chosen = weightedLearnPool[Random.Shared.Next() % weightedLearnPool.Count];
            foreach (var t in BreedInfo.TechList)
                if (t.Slot == chosen)
                    tech = t;

            MonsterAddTechnique(tech);
            Logger.Info(
                $"Monster {Name} learned {tech.Name} they have {TechList.Count} | {TechSlot} now.", Color.Orange);
        }

        if (techint == Config.TechInt.Genius && TechList.Count > 8 &&
            Random.Shared.Next() % 20 < TechList.Count) UnlearnTechnique();
    }

    private void UnlearnTechnique()
    {
        Logger.Info("Monster " + Name + " is attempting an unlearn a tech.", Color.Orange);

        var weightedPool = new List<TechSlots>();
        var minVal = 1000;
        var tech = TechList[0];

        var techweights = new double[TechList.Count];

        int[] rangeCounts = [0, 0, 0, 0];
        foreach (var t in TechList)
            rangeCounts[(int)t.Range] += 1;

        for (var i = 0; i < TechList.Count; i++)
        {
            tech = TechList[i];
            var techval = tech.TechValue;

            techval = tech.Scaling switch
            {
                TechType.Power when Power < Intelligence => techval * 0.8 * ((float)Power / Intelligence),
                TechType.Intelligence when Intelligence < Power => techval * 0.8 * ((float)Intelligence / Power),
                _ => techval
            };

            if (tech.Type == ErrantryType.Special)
                techval *= 1.25;
            else
                switch (_growthGroup)
                {
                    case GrowthGroups.Power when tech.Type == ErrantryType.Heavy:
                    case GrowthGroups.Intel when tech.Type == ErrantryType.Skill:
                    case GrowthGroups.Wither when tech.Type == ErrantryType.Withering:
                    case GrowthGroups.Speedy when tech.Type == ErrantryType.Sharp:
                        techval *= 1.25;
                        break;
                }

            if (rangeCounts[(int)tech.Range] <= 1) techval += 30;

            if (minVal > techval) minVal = (int)techval;

            techweights[i] = techval;
            Logger.Debug(tech.Slot + " has a value of " + techval, Color.Orange);
        }

        for (var i = 0; i < TechList.Count; i++)
        {
            tech = TechList[i];
            var weight = 30 - (techweights[i] - minVal);
            for (var j = 0; j < weight; j++)
                weightedPool.Add(tech.Slot);
        }

        if (weightedPool.Count <= 0) return;

        var chosen = weightedPool[Random.Shared.Next(weightedPool.Count)];
        foreach (var t in BreedInfo.TechList)
            if (t.Slot == chosen)
                tech = t;

        MonsterRemoveTechnique(tech);
        Logger.Info(
            "Monster " + Name + " has unlearned " + tech.Name + " they have " + TechList.Count + " | " +
            TechSlot + " now.", Color.Orange);
    }

    public void MonsterAddTechnique(IMonsterTechnique tech)
    {
        // set the bit at this slot
        TechSlot |= tech.Slot;
    }

    private void MonsterRemoveTechnique(IMonsterTechnique tech)
    {
        // clear the bit at this slot
        TechSlot &= ~tech.Slot;
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

    private void LearnBattleSpecial()
    {
        BattleSpecial |= (BattleSpecials)TournamentData.GrowthRNG.Next(13);
    }

    public byte[] ToSaveFile()
    {
        // TournamentMonster data will consist of 40 new bytes, followed by the standard 60 tracked by the game for Tournament Monsters.
        // 0-1, LifeTotal
        // 2-3, Current Remaining Lifespan
        // 4-5, Growth Rate Per Months
        // 6, Growth Group (Enum)
        // 7, Growth Intensity (Enum)
        // 8-9, Monster Rank (Enum)
        // 10-13, TournamentPools (Enums)
        // 14-19, Growth Weights (Generated from 6/7)
        // 20, Trainer Intelligence

        var data = new byte[40 + 60];
        BitConverter.GetBytes(LifeTotal).CopyTo(data, 0);
        BitConverter.GetBytes(Lifespan).CopyTo(data, 2);
        BitConverter.GetBytes(_growthRate).CopyTo(data, 4);
        data[6] = (byte)_growthGroup;
        data[7] = _growthIntensity;
        data[8] = (byte)Rank;

        data[10] = 0xFF;
        data[11] = 0xFF;
        data[12] = 0xFF;
        data[13] = 0xFF;
        for (var i = 0; i < Pools.Count && i < 4; i++)
            data[10 + i] = (byte)Pools[i].Pool;

        data[14] = _growthLif;
        data[15] = _growthPow;
        data[16] = _growthSki;
        data[17] = _growthSpd;
        data[18] = _growthDef;
        data[19] = _growthInt;

        data[20] = (byte)_trainerIntelligence;

        // Now copy the raw monster data to the last 60 bytes
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