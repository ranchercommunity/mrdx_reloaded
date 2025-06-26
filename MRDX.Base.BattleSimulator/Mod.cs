using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using MRDX.Base.BattleSimulator.Configuration;
using MRDX.Base.BattleSimulator.Template;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.BattleSimulator;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private const string ControlPipeName = "mrdx_reloaded_battle_simulator";

    private readonly string _exepath;

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

    private readonly bool IsServer;

    private Process? _child;

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
        Logger.SetLogLevel(Logger.LogLevel.Trace);
        // Debugger.Launch();

        // Check if the named pipe exists already
        IsServer = !Directory.GetFiles(@"\\.\pipe\").ToList().Contains($@"\\.\pipe\{ControlPipeName}");

        // Try hiding the windows on the client side
        if (!IsServer)
        {
            Logger.Debug("Hiding Console");
            // FreeConsole();
            var console = GetConsoleWindow();
            ShowWindow(console, 0);
        }

        var mainModule = Process.GetCurrentProcess().MainModule;
        if (mainModule == null)
        {
            Logger.Error("Cannot get main module process");
            return;
        }

        _exepath = Path.GetDirectoryName(mainModule.FileName);

        var maybeGame = _modLoader.GetController<IGame>();
        if (maybeGame != null && maybeGame.TryGetTarget(out var game))
            game.OnMonsterBreedsLoaded.Subscribe(_ =>
                Task.Run(StartMod));
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // public static nint GetMainWindowHandle()
    // {
    //     var processes = Process.GetProcesses();
    //     foreach (var process in processes)
    //         if (!string.IsNullOrEmpty(process.MainWindowTitle))
    //             return process.MainWindowHandle;
    //
    //     return IntPtr.Zero;
    // }

    private async Task StartMod()
    {
        if (!IsServer)
        {
            Logger.Debug("Hiding Window");
            Process.GetCurrentProcess().Refresh();
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            var attempts = 0; // keep trying to hide the window for 2 seconds
            while (handle == nint.Zero && attempts++ < 20)
            {
                handle = Process.GetCurrentProcess().MainWindowHandle;
                await Task.Delay(100);
            }

            // ShowWindow(handle, 0); // Hide window
        }

        if (IsServer)
            await SetupServer();
        else
            await SetupClient();
    }

    private async Task SetupServer()
    {
        // Spawn the reloaded process a second time
        // var config = IConfig<LoaderConfig>.FromPathOrDefault(Paths.LoaderConfigPath);
        // var exe = Path.Combine(config.LoaderPath32, "Reloaded-II.exe");
        // TODO don't hardcode this winky face
        Logger.Debug("Gonna relaunch!");

        var exe = @"C:\Programs\Reloaded-II\Reloaded-II.exe";
        var procinfo = new ProcessStartInfo(exe, ["--launch", Path.Combine(_exepath, "MF2.exe")])
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exe),
            CreateNoWindow = true
        };
        _child = Process.Start(procinfo);
        ChildProcessTracker.AddProcess(_child);
        _child.EnableRaisingEvents = true;
        _child.Exited += (sender, args) => { Logger.Debug("Simulator child process has perished. sad face"); };
        var server = new NamedPipeServerStream(ControlPipeName, PipeDirection.InOut);
        Logger.Debug("Waiting for simulation client to connect!");
        await server.WaitForConnectionAsync();
        Logger.Debug("Got connection from client!");
    }

    private async Task SetupClient()
    {
        var client = new NamedPipeClientStream(ControlPipeName);
        Logger.Debug("Connecting to server");
        await client.ConnectAsync();
        Logger.Debug("Got connection to server!");
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion
}