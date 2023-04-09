using System;
using System.Collections.Generic;
using System.Numerics;

namespace MRDX.Base.Mod.Interfaces;

public interface IMonster
{
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
            ToRangeEnum<EffectiveNature, sbyte>((sbyte)(NatureBase + NatureRawToMod(NatureRaw)));
        set =>
            NatureRaw = NatureModToRaw((short)(FromRangeEnum<EffectiveNature, sbyte>(value) - NatureBase));
    }

    byte Fatigue { get; set; }
    byte Fame { get; set; }
    sbyte Stress { get; set; }
    byte LoyalSpoil { get; set; }
    byte LoyalFear { get; set; }

    sbyte FormRaw { get; set; }

    Form Form
    {
        get => ToRangeEnum<Form, sbyte>(FormRaw);
        set => FormRaw = FromRangeEnum<Form, sbyte>(value);
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

    IList<IMonsterAttack> Moves { get; set; }
    IList<byte> MoveUseCount { get; set; }

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

    protected static TEnum ToRangeEnum<TEnum, T>(T val) where T : IBinaryInteger<T> where TEnum : struct, Enum
    {
        foreach (var e in (T[])Enum.GetValuesAsUnderlyingType<TEnum>())
            if (val <= e)
                return (TEnum)Enum.ToObject(typeof(TEnum), e);
        return (TEnum)Enum.ToObject(typeof(TEnum), sbyte.MaxValue); // Error
    }

    protected static T FromRangeEnum<TEnum, T>(TEnum val) where T : IBinaryInteger<T> where TEnum : struct, Enum
    {
        return (T)Convert.ChangeType(val, val.GetTypeCode());
    }

    protected static short NatureRawToMod(short natureRaw)
    {
        return (short)Math.Truncate(Math.Sin(Math.PI * natureRaw / 2048) * 100);
    }

    protected static short NatureModToRaw(short natureMod)
    {
        return (short)Math.Truncate(Math.Asin(natureMod / 100.0f) * 2048 / Math.PI);
    }
}