using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Game.Randomizer;

public class RandomizerConfig
{
    public bool ShuffleTechSlots { get; private set; } = true;

    public static RandomizerConfig? FromFlags(string flags)
    {
        var data = Convert.FromBase64String(flags);
        var cbor = new CborReader(data);
        var cfg = new RandomizerConfig();
        try
        {
            cfg.ShuffleTechSlots = cbor.ReadBoolean();
        }
        catch (Exception e)
        {
            // todo better exception handling
            Randomizer.Logger?.WriteLine($"[MRDX Randomizer] could not parse flags {e}");
            return null;
        }

        return cfg;
    }

    public string ToFlags()
    {
        var cbor = new CborWriter();
        cbor.WriteBoolean(ShuffleTechSlots);
        return Convert.ToBase64String(cbor.Encode());
    }
}

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

public class Randomizer
{
    public const string RANDOMIZER_VERSION = "v1_0_0";

    private const nuint ATK_HEADER_OFFSET = 0x340870;
    private const nuint ATK_NAME_OFFSET = 0x3416B0;

    public static ILogger? Logger;

    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);

    private readonly Memory _memory;
    private readonly IRedirectorController _redirector;


    public Randomizer(ILogger logger, IRedirectorController redirector, string redirectPath, uint seed,
        RandomizerConfig config)
    {
        _memory = Memory.Instance;
        Rng = new Random((int)seed);
        Config = config;
        Logger = logger;
        _redirector = redirector;
        RedirectPath = Path.Combine(redirectPath, $"{seed}_{config.ToFlags()}");
    }

    public string? DataPath { get; set; }
    public string RedirectPath { get; set; }

    public Dictionary<string, Monster> Monsters { get; } = new();

    public Random Rng { get; set; }

    public RandomizerConfig Config { get; set; }


    public void ShuffleTechSlots()
    {
        // Debugger.Launch();
        foreach (var (_, monster) in Monsters)
        {
            // var slots = new MonsterAttack[4, 6];
            // Array.Copy(monster.Attacks.Slots, slots, 4 * 6);
            var i = monster.Techs.Count;
            while (i > 1)
            {
                i--;
                var j = Rng.Next(i + 1);
                var slot1 = monster.Techs[i];
                var slot2 = monster.Techs[j];
                if (monster.Info.Id == MonsterGenus.Centaur)
                    Logger?.WriteLine($"[MRDX Randomizer] switching {slot1.Name} with {slot2.Name}");
                (slot1.Range, slot2.Range) = (slot2.Range, slot1.Range);
                (slot1.Slot, slot2.Slot) = (slot2.Slot, slot1.Slot);
            }
        }
    }

    public async Task Load()
    {
        if (DataPath == null) return;
        Logger?.WriteLine("[MRDX Randomizer] reading monster atk strings from exe");
        var atkNameTable = LoadAtkNames();
        Logger?.WriteLine("[MRDX Randomizer] loading monster data from files");
        for (var i = 0; i < (int)MonsterGenus.Count; i++)
        {
            var mon = IMonster.AllMonsters[i];
            var (_, display, name) = mon;
            var atkfilename = $"{name[..2]}_{name[..2]}_wz.bin";
            var atkpath = Path.Combine(DataPath, "mf2", "data", "mon", name, atkfilename);
            var data = await File.ReadAllBytesAsync(atkpath);
            Monsters[display] = new Monster(atkNameTable, data, mon);
        }
    }

    private Dictionary<MonsterGenus, string[,]> LoadAtkNames()
    {
        const int byteCountForAtkName = 34;
        const int byteCountForHeader = 4;
        const int numberOfAttacks = 24;
        const int allAtkNameSize = (int)MonsterGenus.Count * byteCountForAtkName * numberOfAttacks;
        const int headerSize = (int)MonsterGenus.Count * byteCountForHeader * numberOfAttacks;
        var startAddr = (nuint)(Base.Mod.Base.ExeBaseAddress + ATK_HEADER_OFFSET.ToUInt32());
        var endAddr = (nuint)(startAddr.ToUInt64() + allAtkNameSize + headerSize);
        Logger?.WriteLine(
            $"[MRDX.Randomizer] loading monster atk strings from EXE from addr [{startAddr:x}, {endAddr:x})");
        _memory.ReadRaw(startAddr, out var bytes, (int)(endAddr - startAddr));

        var atkList = new Dictionary<MonsterGenus, string[,]>();
        for (var mon = 0; mon < (int)MonsterGenus.Count; mon++)
        {
            var atkSlots = new string[4, 6];
            for (var i = 0; i < atkSlots.GetLength(0); ++i)
            for (var j = 0; j < atkSlots.GetLength(1); ++j)
            {
                var header = (mon * numberOfAttacks + i * 6 + j) * byteCountForHeader;
                var offset = BitConverter.ToInt32(bytes, header);
                var name = bytes[offset..(offset + byteCountForAtkName)];
                atkSlots[i, j] = name.AsShorts().AsString();
            }

            atkList[(MonsterGenus)mon] = atkSlots;
        }

        return atkList;
    }

    public async Task Save()
    {
        const int byteCountForAtkName = 34;
        const int byteCountForHeader = 4;
        if (DataPath == null) return;
        Logger?.WriteLine($"[MRDX Randomizer] creating directory {RedirectPath}");
        Directory.CreateDirectory(RedirectPath);

        var atkData =
            new byte[Monster.HeaderSize * (int)MonsterGenus.Count + Monster.NameSize * (int)MonsterGenus.Count * 24];

        for (var i = 0; i < (int)MonsterGenus.Count; i++)
        {
            var mon = IMonster.AllMonsters[i];
            var (_, display, name) = mon;
            var headerOffset = Monster.HeaderSize * i;
            var atkNameOffset = Monster.NameSize * i + Monster.HeaderSize * (int)MonsterGenus.Count;

            // Write the attack name and header to the temp array so we can write it out later
            var monster = Monsters[display];
            // Build a header for the attacks 
            for (var j = 0; j < 6 * 4; j++)
            {
                var tech = monster.Techs.ElementAtOrDefault(j);
                if (tech?.Available ?? false)
                {
                    BitConverter.GetBytes(atkNameOffset).CopyTo(atkData, headerOffset);
                    tech.Name.AsMr2().AsBytes().CopyTo(atkData, atkNameOffset);
                    atkNameOffset += byteCountForAtkName;
                }
                else
                {
                    NO_OFFSET.CopyTo(atkData, headerOffset);
                }

                headerOffset += byteCountForHeader;
            }

            // Write the attack data out to a file to redirect.
            var atkfilename = $"{name[..2]}_{name[..2]}_wz.bin";
            var srcpath = Path.Combine(DataPath, "mf2", "data", "mon", name, atkfilename);
            var dstpath = Path.Combine(RedirectPath, atkfilename);

            Logger?.WriteLine($"[MRDX Randomizer] monster {display} creating file for attacks {dstpath}");
            await File.WriteAllBytesAsync(dstpath, monster.SerializeAttackFileData());
            Logger?.WriteLine($"[MRDX Randomizer] redirecting {srcpath} to {dstpath}");
            _redirector.AddRedirect(srcpath, dstpath);
        }

        // Now write the attack data back to the exe
        _memory.Write((nuint)(Base.Mod.Base.ExeBaseAddress + ATK_HEADER_OFFSET.ToUInt32()), atkData);
        Logger?.WriteLine("[MRDX Randomizer] Finished Saving atk data");
    }

    public void DataExtractComplete(string? path)
    {
        DataPath ??= path;
        var process = Randomize();
        process.Wait();
    }

    private async Task Randomize()
    {
        try
        {
            await Load();
            if (Config.ShuffleTechSlots) ShuffleTechSlots();
            await Save();
        }
        catch (Exception e)
        {
            Logger?.WriteLine($"[MRDX Randomizer] Exception thrown while running randomizer {e}");
        }
    }

    public static Randomizer? Create(ILogger logger, IRedirectorController redirector, string modpath, string raw)
    {
        var arr = raw.Split(";");
        if (arr.Length != 3)
        {
            logger.WriteLine(
                $"[MRDX Randomizer] Invalid flag string! Does not have all required parts <seed>;<flags>;<version> : {raw}");
            return null;
        }

        var (seed, flags, version) = arr;
        if (version != RANDOMIZER_VERSION)
        {
            logger.WriteLine(
                $"[MRDX Randomizer] Invalid flag string! Version ({version}) does not match current randomizer version ({RANDOMIZER_VERSION})");
            return null;
        }

        var config = RandomizerConfig.FromFlags(flags);
        if (config == null)
        {
            logger.WriteLine(
                $"[MRDX Randomizer] Invalid flag string! Version ({version}) does not match current randomizer version ({RANDOMIZER_VERSION})");
            return null;
        }

        return new Randomizer(logger, redirector, modpath, CalculateHashMd5(seed), config);
    }

    private static uint CalculateHashMd5(string read)
    {
        var data = Encoding.UTF8.GetBytes(read);
        var blah = MD5.HashData(data);
        return BitConverter.ToUInt32(blah);
    }
}