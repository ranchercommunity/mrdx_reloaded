using System;
using System.Collections.Generic;
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

    public static void Deconstruct(this MonsterInfo src, out MonsterGenus a0, out string a1, out string a2)
    {
        a0 = src.Id;
        a1 = src.Name;
        a2 = src.ShortName;
    }
}

public interface IMonster
{
    static readonly MonsterInfo[] AllMonsters =
    {
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
        new() { Id = MonsterGenus.Unknown1, Name = "Unknown1", ShortName = "unk1" },
        new() { Id = MonsterGenus.Unknown2, Name = "Unknown2", ShortName = "unk2" },
        new() { Id = MonsterGenus.Unknown3, Name = "Unknown3", ShortName = "unk3" },
        new() { Id = MonsterGenus.Unknown4, Name = "Unknown4", ShortName = "unk4" },
        new() { Id = MonsterGenus.Unknown5, Name = "Unknown5", ShortName = "unk5" },
        new() { Id = MonsterGenus.Unknown6, Name = "Unknown6", ShortName = "unk6" }
    };

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

    EffectiveNature Nature
    {
        get =>
            MonsterHelper.ToRangeEnum<EffectiveNature, sbyte>(
                (sbyte)(NatureBase + MonsterHelper.NatureRawToMod(NatureRaw)));
        set =>
            NatureRaw = MonsterHelper.NatureModToRaw(
                (short)(MonsterHelper.FromRangeEnum<EffectiveNature, sbyte>(value) - NatureBase));
    }

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

    IList<IMonsterAttack> Moves { get; }
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

public interface IBattleMonster
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

    sbyte Nature { get; set; }

    byte Spoil { get; set; }
    byte Fear { get; set; }

    byte[] Attacks { get; set; }

    byte ArenaSpeed { get; set; }
    byte GutsRate { get; set; }

    BattleSpecials BattleSpecial { get; set; }
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