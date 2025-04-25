using MRDX.Base.Mod;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.Randomizer;

public class Monster
{
    // 4 bytes per offset, 24 attacks


    public Monster(Dictionary<MonsterGenus, string[,]> monAtks, byte[] atkData, GenusInfo genusInfo)
    {
        Info = genusInfo;
        CreateTechs(monAtks[genusInfo.Id], atkData);
    }

    public GenusInfo Info { get; }
    public List<MonsterTechnique> Techs { get; } = new();

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
                Techs.Add(new MonsterTechnique(atkNames[i, j], (TechSlots)(1 << (i * 6 + j)),
                    rawStats[offset .. (offset + 0x20)]));
            }
        }
    }
}