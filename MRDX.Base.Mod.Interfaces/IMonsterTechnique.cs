using System;
using System.Collections.Generic;
using System.Linq;

namespace MRDX.Base.Mod.Interfaces;

public interface IMonsterTechnique
{
    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);

    // Slot is not a real value on the monster tech data, but rather the position in the attack
    // list where it belongs. This way we can build the actual tech list easier.
    // int Slot { get; set; }
    //
    // int Id
    // {
    //     get => (int)Range * 6 + Slot;
    //     set => Slot = value / 6; Range = value % 6;
    // }

    TechSlots Slot { get; set; }

    byte[] JpnName { get; set; }
    string Name { get; set; }
    ErrantryType Type { get; set; }
    TechRange Range { get; set; }
    TechNature Nature { get; set; }
    TechType Scaling { get; set; }

    bool Available { get; set; }

    sbyte HitPercent { get; set; }
    byte Force { get; set; }
    byte Withering { get; set; }
    byte Sharpness { get; set; }
    byte GutsCost { get; set; }

    byte GutsSteal { get; set; }
    byte LifeSteal { get; set; }
    byte LifeRecovery { get; set; }
    byte ForceMissSelf { get; set; }
    byte ForceHitSelf { get; set; }

    /// <summary>
    ///     Estimated value for how good this tech is based on a simply formula
    /// </summary>
    double TechValue
    {
        get
        {
            var v = 30 + (HitPercent * 1.3 + Force * 1.5 + Withering * 1.0 + Sharpness / 1.25) - GutsCost;
            v *= GutsSteal > 0 || LifeSteal > 0 || LifeRecovery > 0 ? 1.3 : 1.0;
            v *= ForceHitSelf > 0 || ForceMissSelf > 0 ? 0.85 : 1.0;
            return v;
        }
    }

    public byte[] SerializeName()
    {
        return Name.AsMr2().AsBytes();
    }

    public byte[] Serialize()
    {
        var o = new byte[32];
        JpnName.CopyTo(o, 0);
        o[16] = (byte)Type;
        o[17] = (byte)Range;
        o[18] = (byte)Nature;
        o[19] = (byte)Scaling;
        o[20] = (byte)(Available ? 1 : 0);
        o[21] = (byte) HitPercent;
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

    public static byte[] SerializeAttackFileData(List<IMonsterTechnique> allTechs)
    {
        // var nooffset = new byte[] { 0xff, 0xff, 0xff, 0xff };
        var output = new byte[0x360];
        var filledSlot = 0x60; // start from the first offset after the header
        for (var i = 0; i < (int)TechRange.Count; ++i)
        {
            var range = (TechRange)i;
            var techs = allTechs.FindAll(t => t.Range == range).OrderBy(t => t.Slot).ToArray();
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
                tech.Serialize().CopyTo(output, offset);
            }
        }

        return output;
    }
}

public interface IBattleAttack
{
    byte SelectedTechSlot1 { get; set; }
    byte SelectedTechSlot2 { get; set; }
    byte SelectedTechSlot3 { get; set; }
    byte SelectedTechSlot4 { get; set; }
}