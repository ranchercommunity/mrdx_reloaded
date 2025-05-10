using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MRDX.Base.Mod.Interfaces;

[Flags]
public enum StatFlags : ulong
{
    Age = 1 << 0,
    GenusMain = 1 << 1,
    GenusSub = 1 << 2,
    Life = 1 << 3,
    Power = 1 << 4,
    Intelligence = 1 << 5,
    Skill = 1 << 6,
    Speed = 1 << 7,
    Defense = 1 << 8,
    Lifespan = 1 << 9,
    InitalLifespan = 1 << 10,
    NatureRaw = 1 << 11,
    NatureBase = 1 << 12,
    Fatigue = 1 << 13,
    Fame = 1 << 14,
    Stress = 1 << 15,
    LoyalSpoil = 1 << 16,
    LoyalFear = 1 << 17,
    FormRaw = 1 << 18,

    GrowthRateLife = 1 << 19,
    GrowthRatePower = 1 << 20,
    GrowthRateIntelligence = 1 << 21,
    GrowthRateSkill = 1 << 22,
    GrowthRateSpeed = 1 << 23,
    GrowthRateDefense = 1 << 24,

    TrainBoost = 1 << 25,
    CupJellyCount = 1 << 26,
    UsedPeachGold = 1 << 27,
    UsedPeachSilver = 1 << 28,
    Playtime = 1 << 29,
    Drug = 1 << 30,
    DrugDuration = 1L << 31,
    ItemUsed = 1L << 32,
    ItemLike = 1L << 33,
    ItemDislike = 1L << 34,
    Rank = 1L << 35,
    LifeStage = 1L << 36,
    LifeType = 1L << 37,
    ArenaSpeed = 1L << 38,
    GutsRate = 1L << 39,

    Moves = 1L << 40,
    MoveUseCount = 1L << 41,

    MotivationDomino = 1L << 42,
    MotivationStudy = 1L << 43,
    MotivationRun = 1L << 44,
    MotivationShoot = 1L << 45,
    MotivationDodge = 1L << 46,
    MotivationEndure = 1L << 47,
    MotivationPull = 1L << 48,
    MotivationMeditate = 1L << 49,
    MotivationLeap = 1L << 50,
    MotivationSwim = 1L << 51,

    Name = 1L << 52,

    PrizeMoney = 1L << 53
}

[Flags]
public enum TechSlots : uint
{
    Melee0 = 1 << 0,
    Melee1 = 1 << 1,
    Melee2 = 1 << 2,
    Melee3 = 1 << 3,
    Melee4 = 1 << 4,
    Melee5 = 1 << 5,

    Short0 = 1 << 6,
    Short1 = 1 << 7,
    Short2 = 1 << 8,
    Short3 = 1 << 9,
    Short4 = 1 << 10,
    Short5 = 1 << 11,

    Medium0 = 1 << 12,
    Medium1 = 1 << 13,
    Medium2 = 1 << 14,
    Medium3 = 1 << 15,
    Medium4 = 1 << 16,
    Medium5 = 1 << 17,

    Long0 = 1 << 18,
    Long1 = 1 << 19,
    Long2 = 1 << 20,
    Long3 = 1 << 21,
    Long4 = 1 << 22,
    Long5 = 1 << 23
}

public static class MonsterHelper
{
    public static TEnum ToRangeEnum<TEnum, T>(T val) where T : IBinaryInteger<T> where TEnum : struct, Enum
    {
        foreach (var e in (T[])Enum.GetValuesAsUnderlyingType<TEnum>())
            if (val <= e)
                return (TEnum)Enum.ToObject(typeof(TEnum), e);
        return (TEnum)Enum.ToObject(typeof(TEnum), sbyte.MaxValue); // Error
    }

    public static T FromRangeEnum<TEnum, T>(TEnum val) where T : IBinaryInteger<T> where TEnum : struct, Enum
    {
        return (T)Convert.ChangeType(val, val.GetTypeCode());
    }

    public static short NatureRawToMod(short natureRaw)
    {
        return (short)Math.Truncate(Math.Sin(Math.PI * natureRaw / 2048) * 100);
    }

    public static short NatureModToRaw(short natureMod)
    {
        return (short)Math.Truncate(Math.Asin(natureMod / 100.0f) * 2048 / Math.PI);
    }

    public static void Deconstruct(this GenusInfo src, out MonsterGenus a0, out string a1, out string a2)
    {
        a0 = src.Id;
        a1 = src.Name;
        a2 = src.ShortName;
    }
}

public interface IMonster
{
    public static readonly GenusInfo[] AllMonsters =
    [
        new() { Id = MonsterGenus.Pixie, Name = "Pixie", ShortName = "kapi" },
        new() { Id = MonsterGenus.Dragon, Name = "Dragon", ShortName = "kbdr" },
        new() { Id = MonsterGenus.Centaur, Name = "Centaur", ShortName = "kckn" },
        new() { Id = MonsterGenus.ColorPandora, Name = "ColorPandora", ShortName = "kdro" },
        new() { Id = MonsterGenus.Beaclon, Name = "Beaclon", ShortName = "kebe" },
        new() { Id = MonsterGenus.Henger, Name = "Henger", ShortName = "kfhe" },
        new() { Id = MonsterGenus.Wracky, Name = "Wracky", ShortName = "khcy" },
        new() { Id = MonsterGenus.Golem, Name = "Golem", ShortName = "kigo" },
        new() { Id = MonsterGenus.Zuum, Name = "Zuum", ShortName = "kkro" },
        new() { Id = MonsterGenus.Durahan, Name = "Durahan", ShortName = "klyo" },
        new() { Id = MonsterGenus.Arrowhead, Name = "Arrow Head", ShortName = "kmto" },
        new() { Id = MonsterGenus.Tiger, Name = "Tiger", ShortName = "marig" },
        new() { Id = MonsterGenus.Hopper, Name = "Hopper", ShortName = "mbhop" },
        new() { Id = MonsterGenus.Hare, Name = "Hare", ShortName = "mcham" },
        new() { Id = MonsterGenus.Baku, Name = "Baku", ShortName = "mdbak" },
        new() { Id = MonsterGenus.Gali, Name = "Gali", ShortName = "megar" },
        new() { Id = MonsterGenus.Kato, Name = "Kato", ShortName = "mfakr" },
        new() { Id = MonsterGenus.Zilla, Name = "Zilla", ShortName = "mggjr" },
        new() { Id = MonsterGenus.Bajarl, Name = "Bajarl", ShortName = "mhlam" },
        new() { Id = MonsterGenus.Mew, Name = "Mew", ShortName = "minya" },
        new() { Id = MonsterGenus.Phoenix, Name = "Phoenix", ShortName = "mjfbd" },
        new() { Id = MonsterGenus.Ghost, Name = "Ghost", ShortName = "mkgho" },
        new() { Id = MonsterGenus.Metalner, Name = "Metalner", ShortName = "mlspm" },
        new() { Id = MonsterGenus.Suezo, Name = "Suezo", ShortName = "mmxsu" },
        new() { Id = MonsterGenus.Jill, Name = "Jill", ShortName = "mnsnm" },
        new() { Id = MonsterGenus.Mocchi, Name = "Mocchi", ShortName = "mochy" },
        new() { Id = MonsterGenus.Joker, Name = "Joker", ShortName = "mpjok" },
        new() { Id = MonsterGenus.Gaboo, Name = "Gaboo", ShortName = "mqnen" },
        new() { Id = MonsterGenus.Jell, Name = "Jell", ShortName = "mrpru" },
        new() { Id = MonsterGenus.Undine, Name = "Undine", ShortName = "msund" },
        new() { Id = MonsterGenus.Niton, Name = "Niton", ShortName = "mtgai" },
        new() { Id = MonsterGenus.Mock, Name = "Mock", ShortName = "muoku" },
        new() { Id = MonsterGenus.Ducken, Name = "Ducken", ShortName = "mvdak" },
        new() { Id = MonsterGenus.Plant, Name = "Plant", ShortName = "mwpla" },
        new() { Id = MonsterGenus.Monol, Name = "Monol", ShortName = "mxris" },
        new() { Id = MonsterGenus.Ape, Name = "Ape", ShortName = "mylau" },
        new() { Id = MonsterGenus.Worm, Name = "Worm", ShortName = "mzmus" },
        new() { Id = MonsterGenus.Naga, Name = "Naga", ShortName = "naaga" },
        new() { Id = MonsterGenus.XX, Name = "Unknown", ShortName = "xx" },
        new() { Id = MonsterGenus.XY, Name = "Unknown", ShortName = "xy" },
        new() { Id = MonsterGenus.XZ, Name = "Unknown", ShortName = "xz" },
        new() { Id = MonsterGenus.YX, Name = "Unknown", ShortName = "yx" },
        new() { Id = MonsterGenus.YY, Name = "Unknown", ShortName = "yy" },
        new() { Id = MonsterGenus.YZ, Name = "Unknown", ShortName = "yz" }
    ];

    ushort Age { get; set; }

    MonsterGenus GenusMain { get; set; }
    MonsterGenus GenusSub { get; set; }

    ushort Life { get; set; }
    ushort Power { get; set; }
    ushort Intelligence { get; set; }
    ushort Skill { get; set; }
    ushort Speed { get; set; }
    ushort Defense { get; set; }

    ushort Lifespan { get; set; }
    ushort InitalLifespan { get; set; }

    short NatureRaw { get; set; }
    sbyte NatureBase { get; set; }

    EffectiveNature NatureDisplay
    {
        get =>
            MonsterHelper.ToRangeEnum<EffectiveNature, sbyte>(
                (sbyte)(NatureBase + MonsterHelper.NatureRawToMod(NatureRaw)));
        set =>
            NatureRaw = MonsterHelper.NatureModToRaw(
                (short)(MonsterHelper.FromRangeEnum<EffectiveNature, sbyte>(value) - NatureBase));
    }

    sbyte Nature => (sbyte)(NatureBase + MonsterHelper.NatureRawToMod(NatureRaw));

    ushort AdjustedSpeed => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Speed * (sbyte)(NatureBase + MonsterHelper.NatureRawToMod(NatureRaw)) / 4 * 100) / 10000), 1, 999);

    ushort AdjustedDefense => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Defense * (sbyte)(NatureBase + MonsterHelper.NatureRawToMod(NatureRaw)) / 4 * 100) / 10000), 1, 999);

    byte Fatigue { get; set; }
    byte Fame { get; set; }
    sbyte Stress { get; set; }
    byte LoyalSpoil { get; set; }
    byte LoyalFear { get; set; }

    sbyte FormRaw { get; set; }

    Form Form
    {
        get => MonsterHelper.ToRangeEnum<Form, sbyte>(FormRaw);
        set => FormRaw = MonsterHelper.FromRangeEnum<Form, sbyte>(value);
    }

    byte GrowthRateLife { get; set; }
    byte GrowthRatePower { get; set; }
    byte GrowthRateIntelligence { get; set; }
    byte GrowthRateSkill { get; set; }
    byte GrowthRateSpeed { get; set; }
    byte GrowthRateDefense { get; set; }

    ushort TrainBoost { get; set; }
    byte CupJellyCount { get; set; }
    bool UsedPeachGold { get; set; }
    bool UsedPeachSilver { get; set; }
    PlaytimeType Playtime { get; set; }
    byte Drug { get; set; }
    byte DrugDuration { get; set; }
    bool ItemUsed { get; set; }
    Item ItemLike { get; set; }
    Item ItemDislike { get; set; }
    byte Rank { get; set; }
    LifeStage LifeStage { get; set; }
    LifeType LifeType { get; set; }
    byte ArenaSpeed { get; set; }
    byte GutsRate { get; set; }

    IList<IMonsterTechnique> Moves { get; }
    IList<byte> MoveUseCount { get; }

    byte MotivationDomino { get; set; }
    byte MotivationStudy { get; set; }
    byte MotivationRun { get; set; }
    byte MotivationShoot { get; set; }
    byte MotivationDodge { get; set; }
    byte MotivationEndure { get; set; }
    byte MotivationPull { get; set; }
    byte MotivationMeditate { get; set; }
    byte MotivationLeap { get; set; }
    byte MotivationSwim { get; set; }

    string Name { get; set; }

    uint PrizeMoney { get; set; }
}

public interface IBattleMonsterData
{
    string Name { get; set; }

    MonsterGenus GenusMain { get; set; }
    MonsterGenus GenusSub { get; set; }

    ushort Life { get; set; }
    ushort Power { get; set; }
    ushort Defense { get; set; }
    ushort Skill { get; set; }
    ushort Speed { get; set; }
    ushort Intelligence { get; set; }

    ushort AdjustedSpeed => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Speed * Nature / 4 * 100) / 10000), 1, 999);

    ushort AdjustedDefense => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Defense * Nature / 4 * 100) / 10000), 1, 999);

    sbyte Nature { get; set; }

    byte Spoil { get; set; }
    byte Fear { get; set; }

    byte[] Techs { get; set; }

    TechSlots TechSlot
    {
        get => (TechSlots)BitConverter.ToUInt32(Techs, 0);
        set => BitConverter.GetBytes((uint)value)[..2].CopyTo(Techs, 0);
    }

    byte ArenaSpeed { get; set; }
    byte GutsRate { get; set; }

    BattleSpecials BattleSpecial { get; set; }

    ushort StatTotal => (ushort)(Life + Power + Defense + Skill + Speed + Intelligence);

    byte[] Serialize()
    {
        return GetSerializedData();
    }

    public byte[] GetSerializedData()
    {
        var o = Enumerable.Repeat((byte)0xff, 60).ToArray();
        Name.AsMr2().AsBytes().CopyTo(o, 0);
        o[26] = (byte)GenusMain;
        o[27] = (byte)GenusSub;
        BitConverter.GetBytes(Life).CopyTo(o, 28);
        BitConverter.GetBytes(Power).CopyTo(o, 30);
        BitConverter.GetBytes(Defense).CopyTo(o, 32);
        BitConverter.GetBytes(Skill).CopyTo(o, 34);
        BitConverter.GetBytes(Speed).CopyTo(o, 36);
        BitConverter.GetBytes(Intelligence).CopyTo(o, 38);
        o[40] = (byte)Nature;
        o[41] = Fear;
        o[42] = Spoil;
        Techs.CopyTo(o, 43);
        o[48] = ArenaSpeed;
        o[49] = GutsRate;
        BitConverter.GetBytes((ushort)BattleSpecial).CopyTo(o, 52);
        return o;
    }

    public static IBattleMonsterData FromBytes(byte[] o)
    {
        return new BattleMonsterData
        {
            Name = o[..26].AsShorts().AsString(),
            GenusMain = (MonsterGenus)o[26],
            GenusSub = (MonsterGenus)o[27],
            Life = BitConverter.ToUInt16(o, 28),
            Power = BitConverter.ToUInt16(o, 30),
            Defense = BitConverter.ToUInt16(o, 32),
            Skill = BitConverter.ToUInt16(o, 34),
            Speed = BitConverter.ToUInt16(o, 36),
            Intelligence = BitConverter.ToUInt16(o, 38),
            Nature = (sbyte)o[40],
            Fear = o[41],
            Spoil = o[42],
            Techs = o[43..47],
            ArenaSpeed = o[48],
            GutsRate = o[49],
            BattleSpecial = (BattleSpecials)BitConverter.ToUInt16(o, 52)
        };
    }
}

public class BattleMonsterData : IBattleMonsterData
{
    public BattleMonsterData()
    {
    }

    public BattleMonsterData(IBattleMonsterData b)
    {
        // Copy all of the fields from the battlemonsterdata into "us" 
        Name = b.Name;
        GenusMain = b.GenusMain;
        GenusSub = b.GenusSub;
        Life = b.Life;
        Power = b.Power;
        Defense = b.Defense;
        Skill = b.Skill;
        Speed = b.Speed;
        Intelligence = b.Intelligence;
        Nature = b.Nature;
        Fear = b.Fear;
        Spoil = b.Spoil;
        Techs = b.Techs;
        ArenaSpeed = b.ArenaSpeed;
        GutsRate = b.GutsRate;
        BattleSpecial = b.BattleSpecial;
    }

    public ushort StatTotal => (ushort)(Life + Power + Defense + Skill + Speed + Intelligence);

    public ushort AdjustedSpeed => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Speed * Nature / 4 * 100) / 10000), 1, 999);

    public ushort AdjustedDefense => (ushort)Math.Clamp(Math.Truncate(Math.Truncate(
        (double)Defense * Nature / 4 * 100) / 10000), 1, 999);

    public TechSlots TechSlot
    {
        get => (TechSlots)BitConverter.ToUInt32(Techs, 0);
        set => BitConverter.GetBytes((uint)value).CopyTo(Techs, 0);
    }

    public string Name { get; set; }
    public MonsterGenus GenusMain { get; set; }
    public MonsterGenus GenusSub { get; set; }
    public ushort Life { get; set; }
    public ushort Power { get; set; }
    public ushort Defense { get; set; }
    public ushort Skill { get; set; }
    public ushort Speed { get; set; }
    public ushort Intelligence { get; set; }
    public sbyte Nature { get; set; }
    public byte Spoil { get; set; }
    public byte Fear { get; set; }
    public byte[] Techs { get; set; } = new byte[4];
    public byte ArenaSpeed { get; set; }
    public byte GutsRate { get; set; }
    public BattleSpecials BattleSpecial { get; set; }

    public byte[] Serialize()
    {
        return (this as IBattleMonsterData).GetSerializedData();
    }
}

public interface IBattleMonster : IBattleMonsterData
{
    ushort Hp { get; set; }

    sbyte Stress { get; set; }

    byte Fame { get; set; }

    BattleSpecials ActiveBattleSpecial { get; set; }

    BattleSpecials InactiveBattleSpecial { get; set; }

    byte Guts { get; set; }

    IList<IMonsterTechnique> TechData { get; }
}

public static class StatFlagUtil
{
    public static readonly Dictionary<string, StatFlags> LookUp = new()
    {
        { nameof(StatFlags.Age), StatFlags.Age },
        { nameof(StatFlags.GenusMain), StatFlags.GenusMain },
        { nameof(StatFlags.GenusSub), StatFlags.GenusSub },
        { nameof(StatFlags.Life), StatFlags.Life },
        { nameof(StatFlags.Power), StatFlags.Power },
        { nameof(StatFlags.Intelligence), StatFlags.Intelligence },
        { nameof(StatFlags.Skill), StatFlags.Skill },
        { nameof(StatFlags.Speed), StatFlags.Speed },
        { nameof(StatFlags.Defense), StatFlags.Defense },
        { nameof(StatFlags.Lifespan), StatFlags.Lifespan },
        { nameof(StatFlags.InitalLifespan), StatFlags.InitalLifespan },
        { nameof(StatFlags.NatureRaw), StatFlags.NatureRaw },
        { nameof(StatFlags.NatureBase), StatFlags.NatureBase },
        { nameof(StatFlags.Fatigue), StatFlags.Fatigue },
        { nameof(StatFlags.Fame), StatFlags.Fame },
        { nameof(StatFlags.Stress), StatFlags.Stress },
        { nameof(StatFlags.LoyalSpoil), StatFlags.LoyalSpoil },
        { nameof(StatFlags.LoyalFear), StatFlags.LoyalFear },
        { nameof(StatFlags.FormRaw), StatFlags.FormRaw },
        { nameof(StatFlags.GrowthRateLife), StatFlags.GrowthRateLife },
        { nameof(StatFlags.GrowthRatePower), StatFlags.GrowthRatePower },
        { nameof(StatFlags.GrowthRateIntelligence), StatFlags.GrowthRateIntelligence },
        { nameof(StatFlags.GrowthRateSkill), StatFlags.GrowthRateSkill },
        { nameof(StatFlags.GrowthRateSpeed), StatFlags.GrowthRateSpeed },
        { nameof(StatFlags.GrowthRateDefense), StatFlags.GrowthRateDefense },
        { nameof(StatFlags.TrainBoost), StatFlags.TrainBoost },
        { nameof(StatFlags.CupJellyCount), StatFlags.CupJellyCount },
        { nameof(StatFlags.UsedPeachGold), StatFlags.UsedPeachGold },
        { nameof(StatFlags.UsedPeachSilver), StatFlags.UsedPeachSilver },
        { nameof(StatFlags.Playtime), StatFlags.Playtime },
        { nameof(StatFlags.Drug), StatFlags.Drug },
        { nameof(StatFlags.DrugDuration), StatFlags.DrugDuration },
        { nameof(StatFlags.ItemUsed), StatFlags.ItemUsed },
        { nameof(StatFlags.ItemLike), StatFlags.ItemLike },
        { nameof(StatFlags.ItemDislike), StatFlags.ItemDislike },
        { nameof(StatFlags.Rank), StatFlags.Rank },
        { nameof(StatFlags.LifeStage), StatFlags.LifeStage },
        { nameof(StatFlags.LifeType), StatFlags.LifeType },
        { nameof(StatFlags.ArenaSpeed), StatFlags.ArenaSpeed },
        { nameof(StatFlags.GutsRate), StatFlags.GutsRate },
        { nameof(StatFlags.Moves), StatFlags.Moves },
        { nameof(StatFlags.MoveUseCount), StatFlags.MoveUseCount },
        { nameof(StatFlags.MotivationDomino), StatFlags.MotivationDomino },
        { nameof(StatFlags.MotivationStudy), StatFlags.MotivationStudy },
        { nameof(StatFlags.MotivationRun), StatFlags.MotivationRun },
        { nameof(StatFlags.MotivationShoot), StatFlags.MotivationShoot },
        { nameof(StatFlags.MotivationDodge), StatFlags.MotivationDodge },
        { nameof(StatFlags.MotivationEndure), StatFlags.MotivationEndure },
        { nameof(StatFlags.MotivationPull), StatFlags.MotivationPull },
        { nameof(StatFlags.MotivationMeditate), StatFlags.MotivationMeditate },
        { nameof(StatFlags.MotivationLeap), StatFlags.MotivationLeap },
        { nameof(StatFlags.MotivationSwim), StatFlags.MotivationSwim },
        { nameof(StatFlags.Name), StatFlags.Name },
        { nameof(StatFlags.PrizeMoney), StatFlags.PrizeMoney }
    };
}