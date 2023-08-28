using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Game.HardMode.Configuration;
using MRDX.Game.HardMode.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Game.HardMode;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
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

    private readonly IRedirectorController _redirector;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    private string? _dataBinPath;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _modLoader.GetController<IExtractDataBin>().TryGetTarget(out var extract);
        _modLoader.GetController<IRedirectorController>().TryGetTarget(out var redirect);
        _redirector = redirect!;
        _modPath = _modLoader.GetDirectoryForModId(_modConfig.ModId);

        if (extract!.ExtractedPath == null)
        {
            extract.ExtractComplete += path =>
            {
                // TODO: not sure if this works right. The goal is if the extraction hasn't finished before
                // loading the mod then we want to setup the redirection later.
                _dataBinPath = path;
                _redirector.AddRedirectFolder(path,
                    _modPath + @"\Redirector\data");
                SetupRedirectToLifespan();
            };
        }
        else
        {
            _dataBinPath = extract.ExtractedPath;
            _logger.WriteLine(@$"[MRDX.HardMode] path to csv {_dataBinPath}\SDATA_MONSTER.csv");
            SetupRedirectToLifespan();
        }
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private void SetupRedirectToLifespan()
    {
        var path = _configuration.UseOriginalLifespan ? "PS1Lifespan" : "DXLifespan";

        _redirector.RemoveRedirect(_dataBinPath + @"\SDATA_MONSTER.csv");
        _redirector.RemoveRedirect(_dataBinPath + @"\mf2\data\monbase\base.obj");
        _redirector.AddRedirect(_dataBinPath + @"\SDATA_MONSTER.csv",
            _modPath + @$"\ManualRedirected\{path}\SDATA_MONSTER.csv");
        _redirector.AddRedirect(_dataBinPath + @"\mf2\data\monbase\base.obj",
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