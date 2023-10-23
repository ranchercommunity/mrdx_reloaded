using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase, IExports // <= Do not Remove.
{
    private static readonly Dictionary<string, ushort[]> _pluralNames = new()
    {
        { "Pixies", "Pixie".AsMr2() },
        { "Dragons", "Dragon".AsMr2() },
        { "Centaurs", "Centaur".AsMr2() },
        { "ColorPandoras", "ColorPandora".AsMr2() },
        { "Beaclons", "Beaclon".AsMr2() },
        { "Hengers", "Henger".AsMr2() },
        { "Wrackys", "Wracky".AsMr2() },
        { "Golems", "Golem".AsMr2() },
        { "Zuums", "Zuum".AsMr2() },
        { "Durahans", "Durahan".AsMr2() },
        { "Arrow Heads", "Arrow Head".AsMr2() },
        { "Tigers", "Tiger".AsMr2() },
        { "Hoppers", "Hopper".AsMr2() },
        { "Hares", "Hare".AsMr2() },
        { "Bakus", "Baku".AsMr2() },
        { "Galis", "Gali".AsMr2() },
        { "Katos", "Kato".AsMr2() },
        { "Zillas", "Zilla".AsMr2() },
        { "Bajarls", "Bajarl".AsMr2() },
        { "Mews", "Mew".AsMr2() },
        { "Phoenixes", "Phoenix".AsMr2() },
        { "Ghosts", "Ghost".AsMr2() },
        { "Metalners", "Metalner".AsMr2() },
        { "Suezos", "Suezo".AsMr2() },
        { "Jills", "Jill".AsMr2() },
        { "Mocchis", "Mocchi".AsMr2() },
        { "Jokers", "Joker".AsMr2() },
        { "Gaboos", "Gaboo".AsMr2() },
        { "Jells", "Jell".AsMr2() },
        { "Undines", "Undine".AsMr2() },
        { "Nitons", "Niton".AsMr2() },
        { "Mocks", "Mock".AsMr2() },
        { "Duckens", "Ducken".AsMr2() },
        { "Plants", "Plant".AsMr2() },
        { "Monols", "Monol".AsMr2() },
        { "Apes", "Ape".AsMr2() },
        { "Worms", "Worm".AsMr2() },
        { "Nagas", "Naga".AsMr2() }
    };

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private readonly Config _configuration;

    /// <summary>
    ///     Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    ///     Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    /// <summary>
    ///     Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    private readonly IStartupScanner? _startupScanner;

    private readonly Memory _memory;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _configuration = context.Configuration;

        _modLoader.AddOrReplaceController<IHooks>(_owner, new Hooks(context));
        _modLoader.AddOrReplaceController<IController>(_owner, new Controller(context));
        _modLoader.AddOrReplaceController<IGame>(_owner, new Game(context));
        _modLoader.AddOrReplaceController<IGameClient>(_owner, new GameClient());

        var maybeScanner = _modLoader.GetController<IStartupScanner>();
        if (maybeScanner != null && maybeScanner.TryGetTarget(out var scanner)) _startupScanner = scanner;

        _memory = Memory.Instance;

        if (_configuration.FixMonsterBreedPluralization && _startupScanner != null)
            // Sigscanning can only find code not data, so we use it to find a pointer to the plural block and patch them
            // directly instead of hooking the text rendering functions
            _startupScanner.AddMainModuleScan("05 ?? ?? ?? ?? 50 E8 ?? ?? ?? ?? 6B C6 1B",
                result => FixMonsterBreedPluralization(result, 1));
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    public Type[] GetTypes()
    {
        return new[] { typeof(IController) };
    }

    private void FixMonsterBreedPluralization(PatternScanResult result, int addrOffset)
    {
        if (!result.Found)
        {
            _logger.WriteLine("[MRDX.Base] Unable to find pointer to pluralized text!");
            return;
        }

        var pointer = (nuint)(Base.ExeBaseAddress + result.Offset + addrOffset);
        _memory.Read(pointer, out nuint addr);
        _logger.WriteLine($"[MRDX.Base] Patching Pluralization at {addr:x}");
        // each name is a fixed chunk of 13 letters (ushort) + 1 bytes (0xff)
        for (var i = 0; i < _pluralNames.Count; ++i)
        {
            const int len = 13 * 2;
            var straddr = nuint.Add(addr, i * (len + 1));
            _memory.ReadRaw(straddr, out var bytes, len);
            var str = bytes.AsShorts().AsString();
            var unpluralized = _pluralNames[str].AsBytes();

            _memory.SafeWrite(straddr, unpluralized);
        }

        _logger.WriteLine($"[MRDX.Base] Patching Pluralization at {addr:x} Complete");
    }
}