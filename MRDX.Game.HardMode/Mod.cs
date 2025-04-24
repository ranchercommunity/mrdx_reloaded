using System.Drawing;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.HardMode.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;
using Config = MRDX.Game.HardMode.Configuration.Config;

namespace MRDX.Game.HardMode;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private static readonly Dictionary<int, EnemyMonsterData> Enemies = new()
    {
        {
            0x319140 - 0x319140, new EnemyMonsterData
            {
                Name = "Blue Phoenix",
                Life = 826,
                Power = 683,
                Defense = 882,
                Skill = 936,
                Speed = 751,
                Intelligence = 999,
                Nature = -90,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0xff, 0x03, 0x00 },
                ArenaSpeed = 3,
                GutsRate = 11,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Grit |
                                BattleSpecials.Fury | BattleSpecials.Ease
            }
        },
        {
            0x3191b8 - 0x319140, new EnemyMonsterData
            {
                Name = "Bloody Eye"
            }
        },
        {
            0x319320 - 0x319140, new EnemyMonsterData
            {
                Name = "Mad Gaboo"
            }
        },
        {
            0x31935c - 0x319140, new EnemyMonsterData
            {
                Name = "Punisher",
                Life = 668,
                Power = 798,
                Defense = 666,
                Skill = 994,
                Speed = 671,
                Intelligence = 582,
                Nature = -80,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0xff, 0x0b, 0x00 },
                ArenaSpeed = 3,
                GutsRate = 8,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Grit
            }
        },
        {
            0x319398 - 0x319140, new EnemyMonsterData
            {
                Name = "Magma Heart",
                Life = 954,
                Power = 839,
                Defense = 612,
                Skill = 779,
                Speed = 657,
                Intelligence = 791,
                Nature = -55,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0xff, 0xff, 0x06 },
                ArenaSpeed = 2,
                GutsRate = 12,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Fury | BattleSpecials.Real
            }
        },
        {
            0x31962c - 0x319140, new EnemyMonsterData
            {
                Name = "Scaley Grass"
            }
        },
        {
            0x3196a4 - 0x319140, new EnemyMonsterData
            {
                // Replaces Terry with Giegue
                Name = "Giegue",
                GenusMain = MonsterGenus.Metalner,
                GenusSub = (MonsterGenus)0x26,
                Life = 913,
                Power = 832,
                Defense = 869,
                Skill = 968,
                Speed = 544,
                Intelligence = 101,
                Nature = -99,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0xeb, 0x11, 0x00 },
                ArenaSpeed = 4,
                GutsRate = 6,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Grit | BattleSpecials.Hurry
            }
        },
        {
            0x3196e0 - 0x319140, new EnemyMonsterData
            {
                // Replaces Blue Sludge with Queen In Red
                Name = "Queen In Red",
                GenusMain = MonsterGenus.Joker,
                GenusSub = MonsterGenus.Pixie,
                Life = 512,
                Power = 874,
                Defense = 51,
                Skill = 812,
                Speed = 544,
                Intelligence = 23,
                Nature = -99,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0x27, 0x00, 0x00 },
                ArenaSpeed = 3,
                GutsRate = 11,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Real
            }
        },
        {
            0x31971c - 0x319140, new EnemyMonsterData
            {
                // Replaces Blue Sludge with King In Red
                Name = "King In Red",
                GenusMain = MonsterGenus.Gali,
                GenusSub = MonsterGenus.Pixie,
                Life = 513,
                Power = 23,
                Defense = 51,
                Skill = 813,
                Speed = 544,
                Intelligence = 874,
                Nature = -99,
                Spoil = 100,
                Fear = 100,
                Attacks = new byte[] { 0x20, 0x31, 0x00 },
                ArenaSpeed = 3,
                GutsRate = 12,
                BattleSpecial = BattleSpecials.Power | BattleSpecials.Anger | BattleSpecials.Fury
            }
        },
        {
            0x319d70 - 0x319140, new EnemyMonsterData
            {
                Name = "Spring Leg"
            }
        }
    };

    private readonly string? _dataBinPath;

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

    private readonly string _modPath;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    private readonly WeakReference<IRedirectorController> _redirector;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _modPath = _modLoader.GetDirectoryForModId(_modConfig.ModId);

        _redirector = _modLoader.GetController<IRedirectorController>();
        _modLoader.GetController<IExtractDataBin>().TryGetTarget(out var extract);
        if (extract == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get extract data bin controller.", Color.Red);
            return;
        }

        _redirector.TryGetTarget(out var redirect);
        if (redirect == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red);
            return;
        }

        _dataBinPath = extract.ExtractedPath;
        SetupRedirectToLifespan();
        UpdateErrantryMonsterStats();
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private void UpdateErrantryMonsterStats()
    {
        // Debugger.Launch();
        foreach (var (offset, enemy) in Enemies)
        {
            var p = new ErrantryEnemyMonster(0x31b540 + offset);
            if (enemy.Name != null) p.Name = enemy.Name;
            if (enemy.GenusMain != null) p.GenusMain = enemy.GenusMain.GetValueOrDefault();
            if (enemy.GenusSub != null) p.GenusSub = enemy.GenusSub.GetValueOrDefault();
            if (enemy.Life != null) p.Life = enemy.Life.GetValueOrDefault();
            if (enemy.Power != null) p.Power = enemy.Power.GetValueOrDefault();
            if (enemy.Defense != null) p.Defense = enemy.Defense.GetValueOrDefault();
            if (enemy.Skill != null) p.Skill = enemy.Skill.GetValueOrDefault();
            if (enemy.Speed != null) p.Speed = enemy.Speed.GetValueOrDefault();
            if (enemy.Intelligence != null) p.Intelligence = enemy.Intelligence.GetValueOrDefault();
            if (enemy.Nature != null) p.Nature = enemy.Nature.GetValueOrDefault();
            if (enemy.Spoil != null) p.Spoil = enemy.Spoil.GetValueOrDefault();
            if (enemy.Fear != null) p.Fear = enemy.Fear.GetValueOrDefault();
            if (enemy.Attacks != null) p.Techs = enemy.Attacks;
            if (enemy.ArenaSpeed != null) p.ArenaSpeed = enemy.ArenaSpeed.GetValueOrDefault();
            if (enemy.GutsRate != null) p.GutsRate = enemy.GutsRate.GetValueOrDefault();
            if (enemy.BattleSpecial != null) p.BattleSpecial = enemy.BattleSpecial.GetValueOrDefault();
        }
    }

    private void SetupRedirectToLifespan()
    {
        var path = _configuration.UseOriginalLifespan ? "PS1Lifespan" : "DXLifespan";

        _redirector.TryGetTarget(out var redirect);
        if (redirect == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to get redirection controller.", Color.Red);
            return;
        }

        redirect.RemoveRedirect(_dataBinPath + @"\SDATA_MONSTER.csv");
        redirect.RemoveRedirect(_dataBinPath + @"\mf2\data\monbase\base.obj");
        redirect.AddRedirect(_dataBinPath + @"\SDATA_MONSTER.csv",
            _modPath + @$"\ManualRedirected\{path}\SDATA_MONSTER.csv");
        redirect.AddRedirect(_dataBinPath + @"\mf2\data\monbase\base.obj",
            _modPath + @$"\ManualRedirected\{path}\base.obj");
    }

    #region Standard Overrides

    /// <summary>
    ///     Returns true if the suspend functionality is supported, else false.
    /// </summary>
    public override bool CanSuspend()
    {
        return true;
    }

    /// <summary>
    ///     Returns true if the unload functionality is supported, else false.
    /// </summary>
    public override bool CanUnload()
    {
        return true;
    }

    /// <summary>
    ///     Suspends your mod, i.e. mod stops performing its functionality but is not unloaded.
    /// </summary>
    public override void Suspend()
    {
        var redirector = _modLoader.GetController<IRedirectorController>();
        if (redirector != null && redirector.TryGetTarget(out var re)) re.Disable();
    }

    /// <summary>
    ///     Unloads your mod, i.e. mod stops performing its functionality but is not unloaded.
    /// </summary>
    /// <remarks>In most cases, calling suspend here is sufficient.</remarks>
    public override void Unload()
    {
        Suspend();
    }

    /// <summary>
    ///     Automatically called by the mod loader when the mod is about to be unloaded.
    /// </summary>
    public override void Resume()
    {
        var redirector = _modLoader.GetController<IRedirectorController>();
        if (redirector != null && redirector.TryGetTarget(out var re)) re.Enable();
    }

    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        SetupRedirectToLifespan();
    }

    #endregion
}

internal struct EnemyMonsterData
{
    public string? Name;
    public MonsterGenus? GenusMain;
    public MonsterGenus? GenusSub;
    public ushort? Life;
    public ushort? Power;
    public ushort? Defense;
    public ushort? Skill;
    public ushort? Speed;
    public ushort? Intelligence;

    public sbyte? Nature;

    public byte? Spoil;
    public byte? Fear;

    public byte[]? Attacks;

    public byte? ArenaSpeed;
    public byte? GutsRate;

    public BattleSpecials? BattleSpecial;
}