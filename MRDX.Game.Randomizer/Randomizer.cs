using System.Security.Cryptography;
using System.Text;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Game.Randomizer;

public class Randomizer
{
    // public const string RANDOMIZER_VERSION = "v1_0_0";

    public static ILogger? Logger;

    private static readonly byte[] NO_OFFSET = BitConverter.GetBytes(0xffffffff);

    private readonly Memory _memory;
    private readonly WeakReference<IRedirectorController> _redirector;


    public Randomizer(ILogger logger, WeakReference<IRedirectorController> redirector, string redirectPath, uint seed,
        RandomizerConfig config)
    {
        _memory = Memory.Instance;
        Rng = new Random((int)seed);
        TourneyRng = new Random(Rng.Next());
        Config = config;
        Logger = logger;
        _redirector = redirector;
        RedirectPath = Path.Combine(redirectPath, $"{seed}_{config.ToFlags()}");
    }

    public string? DataPath { get; set; }
    public string RedirectPath { get; set; }

    public Dictionary<string, byte[]> MonsterFlkFile { get; } = new();

    public Random Rng { get; set; }

    // Will be used when generating new tourney monsters with rotating by tourney
    public Random TourneyRng { get; set; }

    public RandomizerConfig Config { get; set; }

    // public void ShuffleTechSlots()
    // {
    //     foreach (var (_, monster) in Monsters)
    //     {
    //         // var slots = new MonsterAttack[4, 6];
    //         // Array.Copy(monster.Attacks.Slots, slots, 4 * 6);
    //         var i = monster.Techs.Count;
    //         while (i > 1)
    //         {
    //             i--;
    //             var j = Rng.Next(i + 1);
    //             var slot1 = monster.Techs[i];
    //             var slot2 = monster.Techs[j];
    //             if (monster.Info.Id == MonsterGenus.Centaur)
    //                 Logger?.WriteLine($"[MRDX Randomizer] switching {slot1.Name} with {slot2.Name}");
    //             (slot1.Range, slot2.Range) = (slot2.Range, slot1.Range);
    //             (slot1.Slot, slot2.Slot) = (slot2.Slot, slot1.Slot);
    //         }
    //     }
    // }

    public async Task Save()
    {
        if (DataPath == null) return;
        Logger?.WriteLine($"[MRDX Randomizer] creating directory {RedirectPath}");
        Directory.CreateDirectory(RedirectPath);

        // await SaveAttacks();
    }

    public void DataExtractComplete(string? path)
    {
        DataPath ??= path;
        var process = Randomize();
        process.Wait();
    }

    private void ShuffleTechStats(ShuffleMode mode)
    {
        // Todo: Consider allowing users to add/remove stats from the shuffle
        var order = new[]
        {
            StatShuffleOrder.Force,
            StatShuffleOrder.Hit,
            StatShuffleOrder.Sharp,
            StatShuffleOrder.Wither
        };

        Action<IMonsterTechnique> shuffleStrategy = mode switch
        {
            ShuffleMode.BalancedRandom => atk =>
            {
                var total = atk.Force + atk.HitPercent + atk.Sharpness * .5 + atk.Withering;
                // Create a variance of +/- 20% to the stats
                total += total * Rng.Next(-20, 21) / 100.0;
                // and then reapply stats in a random order
                Utils.Shuffle(Rng, order);
                foreach (var stat in order)
                {
                    var val = 0;
                    switch (stat)
                    {
                        case StatShuffleOrder.Force:
                            // Set the range from 1, 60
                            val = Rng.Next(1, (byte)Math.Max(2, Math.Round(Math.Min(total, 60))));
                            atk.Force = (byte)val;
                            break;
                        case StatShuffleOrder.Hit:
                            // NOTE: This range is different, its -16, 34
                            val = Rng.Next(0, (byte)Math.Max(1, Math.Round(Math.Min(total, 50)))) - 16;
                            atk.HitPercent = (byte)val;
                            break;
                        case StatShuffleOrder.Sharp:
                            // Set the range 0, 50 (times 2)
                            // NOTE: we double the effectiveness of sharpness to make it more useful
                            val = Rng.Next(0, (byte)Math.Max(1, Math.Round(Math.Min(total, 50))));
                            atk.Sharpness = (byte)(val * 2);
                            break;
                        case StatShuffleOrder.Wither:
                            // Set the range 0, 50
                            val = Rng.Next(0, (byte)Math.Max(1, Math.Round(Math.Min(total, 50))));
                            atk.Withering = (byte)val;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    total -= val;
                }

                // And calculate a new guts cost based on the final stats with a +/- 20% variance
                var gutsCost = atk.Force + atk.HitPercent + atk.Sharpness * .5 + atk.Withering;
                atk.GutsCost = (byte)(Math.Max(0, gutsCost + gutsCost * Rng.Next(-20, 21) / 100.0) + 8);
            },
            ShuffleMode.ShuffledRandom => atk =>
            {
                // atk.Force
            },
            ShuffleMode.FullRandom => atk =>
            {
                // TODO: adjustable biases?
                // Bias towards lower values to prevent too many crazy overpowered moves 
                atk.Force = (byte)Math.Min(Rng.Next(1, 60), Rng.Next(1, 60));
                atk.HitPercent = (byte)Math.Min(Rng.Next(-16, 35), Rng.Next(-16, 35));
                atk.GutsCost = (byte)Math.Min(Rng.Next(10, 55), Rng.Next(10, 55));
                atk.Withering = (byte)Math.Min(Rng.Next(0, 55), Rng.Next(0, 55));
                atk.Sharpness = (byte)Math.Min(Rng.Next(0, 55), Rng.Next(0, 55));
            },
            _ => throw new NotImplementedException()
        };

        // foreach (var (_, mon) in Monsters)
        // foreach (var tech in mon.Techs)
        //     shuffleStrategy(tech);
    }

    private void ShuffleTechType()
    {
        // foreach (var (_, mon) in Monsters)
        // foreach (var tech in mon.Techs)
        //     tech.Scaling = (TechType)Rng.Next(0, 1);
    }

    private async Task Randomize()
    {
        try
        {
            // await Load();
            // if (Config.TechSlots) TechSlots();
            if (Config.TechStats != ShuffleMode.Vanilla) ShuffleTechStats(Config.TechStats);
            if (Config.TechType) ShuffleTechType();
            await Save();
        }
        catch (Exception e)
        {
            Logger?.WriteLine($"[MRDX Randomizer] Exception thrown while running randomizer {e}");
        }
    }

    public static Randomizer? Create(ILogger logger, WeakReference<IRedirectorController> redirector, string modpath,
        string raw)
    {
        var arr = raw.Split("_");
        if (arr.Length != 2)
        {
            logger.WriteLine(
                $"[MRDX Randomizer] Invalid flag string! Does not have all required parts <seed>_<flags> : {raw}");
            return null;
        }

        var (seed, flags) = arr;

        var config = RandomizerConfig.FromFlags(flags);
        if (config == null)
        {
            logger.WriteLine(
                "[MRDX Randomizer] Invalid flag string!");
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

    private enum StatShuffleOrder
    {
        Force,
        Hit,
        Sharp,
        Wither
    }

    private enum TechShuffleType
    {
        Force,
        Hit,
        Sharp,
        Withering
    }
}