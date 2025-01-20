using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.Randomizer;

public class Monster
{
    // 4 bytes per offset, 24 attacks
    public const int HeaderSize = 4 * 24;
    public const int NameSize = 34;
    public const int AtkNameDataOffset = HeaderSize * (int)MonsterGenus.Count;

    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);

    public Monster(Dictionary<MonsterGenus, string[,]> monAtks, byte[] atkData, MonsterInfo monsterInfo)
    {
        Info = monsterInfo;
        CreateTechs(monAtks[monsterInfo.Id], atkData);
    }

    public MonsterInfo Info { get; }
    public List<MonsterAttack> Techs { get; } = new();

    private void CreateTechs(string[,] atkNames, Span<byte> rawStats)
    {
        for (var i = 0; i < (int)TechRange.Count; ++i)
        {
            var tech = (TechRange)i;
            for (var j = 0; j < 6; ++j)
            {
                var slot = (i * 6 + j) * 4;
                // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack tech {tech} slot {slot}");
                var offset = BitConverter.ToInt32(rawStats[slot .. (slot + 4)]);
                if (offset < 0)
                    // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] skipping negative offset {offset}");
                    continue;

                // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack offset {offset}");
                Techs.Add(new MonsterAttack(atkNames[i, j], j, rawStats[offset .. (offset + 0x20)]));
            }
        }
    }

    public byte[] SerializeAttackFileData()
    {
        // var nooffset = new byte[] { 0xff, 0xff, 0xff, 0xff };
        var output = new byte[0x360];
        var filledSlot = 0x60; // start from the first offset after the header
        for (var i = 0; i < (int)TechRange.Count; ++i)
        {
            var range = (TechRange)i;
            var techs = Techs.FindAll(t => t.Range == range).OrderBy(t => t.Slot).ToArray();
            for (var j = 0; j < 6; ++j)
            {
                var headerOffset = (i * 6 + j) * 4;
                if (j >= techs.Length)
                {
                    // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] writing nooffset to i {i} j {j}");
                    // NOTE: This is essentially a break because once the game sees a 0xffffffff it will also skip to the next
                    // column, so thats why we sort by the Slot value, but then ignore it when filling attacks into the file
                    NO_OFFSET.CopyTo(output, headerOffset);
                    continue;
                }

                var tech = techs[j];

                // Randomizer.Logger?.WriteLine("[MRDX Randomizer] bit converting");
                var offset = filledSlot;
                filledSlot += 0x20;
                // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack offset {offset}");
                BitConverter.GetBytes(offset).CopyTo(output, headerOffset);
                // Randomizer.Logger?.WriteLine($"[MRDX Randomizer] copying raw data to offset {offset}");
                tech.SerializeData().CopyTo(output, offset);
            }
        }

        return output;
    }
}

public record MonsterAttack
{
    public MonsterAttack(string atkName, int slot, Span<byte> data)
    {
        var dat = data.ToArray();
        Name = atkName;
        Slot = slot;
        JpnName = dat[..16];
        Type = (ErrantyType)dat[16];
        Range = (TechRange)dat[17];
        Nature = (TechNature)dat[18];
        Scaling = (TechType)dat[19];
        Available = dat[20] == 1;
        HitPercent = dat[21];
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
    public int Slot { get; set; }

    public byte[] JpnName { get; set; }
    public string Name { get; set; }
    public ErrantyType Type { get; set; }
    public TechRange Range { get; set; }
    public TechNature Nature { get; set; }
    public TechType Scaling { get; set; }
    public bool Available { get; set; }
    public byte HitPercent { get; set; }
    public byte Force { get; set; }
    public byte Withering { get; set; }
    public byte Sharpness { get; set; }
    public byte GutsCost { get; set; }
    public byte GutsSteal { get; set; }
    public byte LifeSteal { get; set; }
    public byte LifeRecovery { get; set; }
    public byte ForceMissSelf { get; set; }
    public byte ForceHitSelf { get; set; }

    public byte[] SerializeName()
    {
        return Name.AsMr2().AsBytes();
    }

    public byte[] SerializeData()
    {
        var o = new byte[32];
        JpnName.CopyTo(o, 0);
        o[16] = (byte)Type;
        o[17] = (byte)Range;
        o[18] = (byte)Nature;
        o[19] = (byte)Scaling;
        o[20] = (byte)(Available ? 1 : 0);
        o[21] = HitPercent;
        o[22] = Force;
        o[23] = Withering;
        o[24] = Sharpness;
        o[25] = GutsCost;
        o[26] = GutsSteal;
        o[27] = LifeSteal;
        o[28] = LifeRecovery;
        o[29] = ForceMissSelf;
        o[30] = ForceHitSelf;
        return o;
    }
}