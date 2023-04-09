using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class Monster : BaseObject<Monster>, IMonster
{
    public Monster(int offset) : base(offset)
    {
    }

    // TODO fill in the offsets for all these fields

    [BaseOffset(BaseGame.Mr2, Region.Us, 0)]
    public ushort Age
    {
        get => Read<ushort>("Age");
        set => Write("Age", value);
    }

    public MonsterGenus GenusMain { get; set; }
    public MonsterGenus GenusSub { get; set; }
    public ushort Life { get; set; }
    public ushort Power { get; set; }
    public ushort Intelligence { get; set; }
    public ushort Skill { get; set; }
    public ushort Speed { get; set; }
    public ushort Defense { get; set; }
    public ushort Lifespan { get; set; }
    public ushort InitalLifespan { get; set; }
    public short NatureRaw { get; set; }
    public sbyte NatureBase { get; set; }
    public byte Fatigue { get; set; }
    public byte Fame { get; set; }
    public sbyte Stress { get; set; }
    public byte LoyalSpoil { get; set; }
    public byte LoyalFear { get; set; }
    public sbyte FormRaw { get; set; }
    public byte GrowthRateLife { get; set; }
    public byte GrowthRatePower { get; set; }
    public byte GrowthRateIntelligence { get; set; }
    public byte GrowthRateSkill { get; set; }
    public byte GrowthRateSpeed { get; set; }
    public byte GrowthRateDefense { get; set; }
    public ushort TrainBoost { get; set; }
    public byte CupJellyCount { get; set; }
    public bool UsedPeachGold { get; set; }
    public bool UsedPeachSilver { get; set; }
    public PlaytimeType Playtime { get; set; }
    public byte Drug { get; set; }
    public byte DrugDuration { get; set; }
    public bool ItemUsed { get; set; }
    public Item ItemLike { get; set; }
    public Item ItemDislike { get; set; }
    public byte Rank { get; set; }
    public LifeStage LifeStage { get; set; }
    public LifeType LifeType { get; set; }
    public byte ArenaSpeed { get; set; }
    public byte GutsRate { get; set; }

    public IList<IMonsterAttack> Moves { get; set; } = new List<IMonsterAttack>();
    public IList<byte> MoveUseCount { get; set; } = new List<byte>();

    public byte MotivationDomino { get; set; }
    public byte MotivationStudy { get; set; }
    public byte MotivationRun { get; set; }
    public byte MotivationShoot { get; set; }
    public byte MotivationDodge { get; set; }
    public byte MotivationEndure { get; set; }
    public byte MotivationPull { get; set; }
    public byte MotivationMeditate { get; set; }
    public byte MotivationLeap { get; set; }
    public byte MotivationSwim { get; set; }

    public string Name
    {
        get => ReadStr("Name", 12);
        set => WriteStr("Name", value);
    }

    public uint PrizeMoney { get; set; }
}