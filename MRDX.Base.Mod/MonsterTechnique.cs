using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.Mod;

public record MonsterTechnique : IMonsterTechnique
{
    public MonsterTechnique(string atkName, TechSlots slot, Span<byte> data)
    {
        var dat = data.ToArray();
        Name = atkName;
        Slot = slot;
        JpnName = dat[..16];
        Type = (ErrantryType)dat[16];
        Range = (TechRange)dat[17];
        Nature = (TechNature)dat[18];
        Scaling = (TechType)dat[19];
        Available = dat[20] == 1;
        HitPercent = (sbyte) dat[ 21 ];
        Force = dat[22];
        Withering = dat[23];
        Sharpness = dat[24];
        GutsCost = dat[25];
        GutsSteal = dat[26];
        LifeSteal = dat[27];
        LifeRecovery = dat[28];
        ForceMissSelf = dat[29];
        ForceHitSelf = dat[30];
    }

    // Slot is not a real value on the monster tech data, but rather the position in the attack
    // list where it belongs. This way we can build the actual tech list easier.
    public TechSlots Slot { get; set; }

    public byte[] JpnName { get; set; }
    public string Name { get; set; }
    public ErrantryType Type { get; set; }
    public TechRange Range { get; set; }
    public TechNature Nature { get; set; }
    public TechType Scaling { get; set; }
    public bool Available { get; set; }
    public sbyte HitPercent { get; set; }
    public byte Force { get; set; }
    public byte Withering { get; set; }
    public byte Sharpness { get; set; }
    public byte GutsCost { get; set; }
    public byte GutsSteal { get; set; }
    public byte LifeSteal { get; set; }
    public byte LifeRecovery { get; set; }
    public byte ForceMissSelf { get; set; }
    public byte ForceHitSelf { get; set; }
}