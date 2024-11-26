using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Game.Randomizer;

public class RandomizerConfig
{
    public bool ShuffleTechSlots { get; private set; }

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
    public static readonly Dictionary<string, string> ALL_MONSTERS = new()
    {
        { "Pixie", "kapi" },
        { "Dragon", "kbdr" },
        { "Centaur", "kckn" },
        { "ColorPandora", "kdro" },
        { "Beaclon", "kebe" },
        { "Henger", "kfhe" },
        { "Wracky", "khcy" },
        { "Golem", "kigo" },
        { "Zuum", "kkro" },
        { "Durahan", "klyo" },
        { "Arrow Head", "kmto" },
        { "Tiger", "marig" },
        { "Hopper", "mbhop" },
        { "Hare", "mcham" },
        { "Baku", "mdbak" },
        { "Gali", "megar" },
        { "Kato", "mfakr" },
        { "Zilla", "mggjr" },
        { "Bajarl", "mhlam" },
        { "Mew", "minya" },
        { "Phoenix", "mjfbd" },
        { "Ghost", "mkgho" },
        { "Metalner", "mlspm" },
        { "Suezo", "mmxsu" },
        { "Jill", "mnsnm" },
        { "Mocchi", "mochy" },
        { "Joker", "mpjok" },
        { "Gaboo", "mqnen" },
        { "Jell", "mrpru" },
        { "Undine", "msund" },
        { "Niton", "mtgai" },
        { "Mock", "muoku" },
        { "Ducken", "mvdak" },
        { "Plant", "mwpla" },
        { "Monol", "mxris" },
        { "Ape", "mylau" },
        { "Worm", "mzmus" },
        { "Naga", "naaga" }
    };

    public Monster(Span<byte> rawAtk, string displayName, string name)
    {
        // TODO: move file loading out of the constructor
        // load the file containing the vanilla data for the monster
        DisplayName = displayName;
        Name = name;
        Randomizer.Logger?.WriteLine($"[MRDX Randomizer] loading attacks for {displayName}");
        Attacks = new MonsterAttacks(rawAtk);
    }

    public string DisplayName { get; }

    public string Name { get; }

    public MonsterAttacks Attacks { get; }
}

public class MonsterAttacks
{
    public MonsterAttacks(Span<byte> raw)
    {
        for (var i = 0; i < Slots.GetLength(0); ++i)
        for (var j = 0; j < Slots.GetLength(1); ++j)
        {
            var tech = j * 4;
            var slot = i * 24;
            Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack tech {tech} slot {slot}");
            var offset = BitConverter.ToInt32(raw[(tech + slot) .. (tech + slot + 4)]);
            if (offset < 0)
            {
                Randomizer.Logger?.WriteLine($"[MRDX Randomizer] skipping negative offset {offset}");
                continue;
            }

            Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack offset {offset}");
            Slots[i, j] = new MonsterAttack(raw[offset .. (offset + 0x20)]);
        }
    }

    public MonsterAttack?[,] Slots { get; } = new MonsterAttack[4, 6];

    public byte[] Serialize()
    {
        var nooffset = new byte[] { 0xff, 0xff, 0xff, 0xff };
        var output = new byte[0x360];
        for (var i = 0; i < Slots.GetLength(0); ++i)
        for (var j = 0; j < Slots.GetLength(1); ++j)
        {
            var tech = j * 4;
            var slot = i * 6 * 4;
            if (Slots[i, j] == null)
            {
                Randomizer.Logger?.WriteLine($"[MRDX Randomizer] writing nooffset to i {i} j {j}");
                nooffset.CopyTo(output, tech + slot);
                continue;
            }

            Randomizer.Logger?.WriteLine("[MRDX Randomizer] bit converting");
            var offset = 0x60 + j * 0x20 + i * 0x20 * 6;
            Randomizer.Logger?.WriteLine($"[MRDX Randomizer] new attack offset {offset}");
            BitConverter.GetBytes(offset).CopyTo(output, tech + slot);
            Randomizer.Logger?.WriteLine($"[MRDX Randomizer] copying raw data to offset {offset}");
            Slots[i, j]!.Raw.CopyTo(output, offset);
        }

        return output;
    }
}

public class MonsterAttack
{
    public MonsterAttack(Span<byte> data)
    {
        Raw = data.ToArray();
    }

    public byte[] Raw { get; set; }
}

public class Randomizer
{
    public static readonly string RANDOMIZER_VERSION = "v1_0_0";

    public static ILogger? Logger;
    private readonly IRedirectorController _redirector;

    public Randomizer(ILogger logger, IRedirectorController redirector, string redirectPath, uint seed,
        RandomizerConfig config)
    {
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


    public async Task Load()
    {
        if (DataPath == null) return;
        Logger?.WriteLine("[MRDX Randomizer] writing new files");
        foreach (var (display, name) in Monster.ALL_MONSTERS)
        {
            var atkfilename = $"{name[..2]}_{name[..2]}_wz.bin";
            var atkpath = Path.Combine(DataPath, "mf2", "data", "mon", name, atkfilename);
            var data = await File.ReadAllBytesAsync(atkpath);
            Monsters[display] = new Monster(data, display, name);
        }
    }

    public async Task Shuffle()
    {
        if (DataPath == null) return;
        await Load();
        await Save();
    }

    public async Task Save()
    {
        if (DataPath == null) return;
        Logger?.WriteLine($"[MRDX Randomizer] creating directory {RedirectPath}");
        Directory.CreateDirectory(RedirectPath);
        foreach (var (display, monster) in Monsters)
        {
            var name = monster.Name;
            var atkfilename = $"{name[..2]}_{name[..2]}_wz.bin";
            var srcpath = Path.Combine(DataPath, "mf2", "data", "mon", name, atkfilename);
            var dstpath = Path.Combine(RedirectPath, atkfilename);

            Logger?.WriteLine($"[MRDX Randomizer] monster {display} creating file for attacks {dstpath}");
            await File.WriteAllBytesAsync(dstpath, monster.Attacks.Serialize());
            Logger?.WriteLine($"[MRDX Randomizer] redirecting {srcpath} to {dstpath}");
            _redirector.AddRedirect(srcpath, dstpath);
        }
    }

    public void DataExtractComplete(string? path)
    {
        DataPath ??= path;
        Shuffle().Wait();
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