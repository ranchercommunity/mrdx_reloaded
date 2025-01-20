using System.Formats.Cbor;

namespace MRDX.Game.Randomizer;

public enum ShuffleMode
{
    Vanilla = 0,
    FullRandom,
    BalancedRandom,
    ShuffledRandom
}

public class RandomizerConfig
{
    public const int Version = 1;

    // Tech randomization group
    // UNUSED until I can figure out where and how battle animations are setup
    public bool TechSlots { get; private set; }

    public ShuffleMode TechStats { get; private set; } = ShuffleMode.FullRandom;

    public bool TechType { get; private set; } = true;

    private static bool? ReadNullableBool(CborReader reader)
    {
        var value = reader.ReadSimpleValue();
        return value switch
        {
            CborSimpleValue.False => false,
            CborSimpleValue.True => true,
            CborSimpleValue.Null => null,
            _ => throw new FormatException($"Unexpected CBOR value: {value}")
        };
    }

    private static void WriteNullableBool(CborWriter writer, bool? value)
    {
        switch (value)
        {
            case null: writer.WriteNull(); break;
            default: writer.WriteBoolean(value.Value); break;
        }
    }

    public static RandomizerConfig? FromFlags(string flags)
    {
        var data = Convert.FromBase64String(flags);
        var reader = new CborReader(data, allowMultipleRootLevelValues: true);
        var cfg = new RandomizerConfig();
        try
        {
            var version = reader.ReadInt32();
            if (version != Version)
            {
                Randomizer.Logger?.WriteLine(
                    $"[MRDX Randomizer] Flags version validation failed! Expected version {Version}, got {version}");
                return null;
            }

            cfg.TechSlots = reader.ReadBoolean();
            cfg.TechStats = (ShuffleMode)reader.ReadUInt32();
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
        var writer = new CborWriter(allowMultipleRootLevelValues: true);
        writer.WriteUInt32(Version);
        writer.WriteBoolean(TechSlots);
        writer.WriteUInt32((uint)TechStats);
        return Convert.ToBase64String(writer.Encode());
    }
}