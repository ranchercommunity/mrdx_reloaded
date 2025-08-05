using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using MRDX.Base.BattleSimulator.Configuration;
using MRDX.Base.BattleSimulator.Template;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.BattleSimulator;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private const string ControlPipeName = "mrdx_reloaded_battle_simulator";

    private readonly string _exepath;

    private readonly WeakReference<IGameClient> _gameClient;

    private readonly Lock _gameLock = new();

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

    private IHook<SetupCCtrlBattle>? _battleHook;

    private Process? _child;

    private ChildProcessManager _childManager = new();

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    private IHook<LoadDemoMonsterData>? _loadDataHook;

    private BattleSimRequest? _request;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        // Logger.SetLogLevel(Logger.LogLevel.Trace);
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

            _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
            if (hooks == null)
            {
                Logger.Error("Could not get hook controller.");
                return;
            }

            hooks.AddHook<SetupCCtrlBattle>(ClientDemoBattleHook)
                .ContinueWith(result => _battleHook = result.Result);
            hooks.AddHook<LoadDemoMonsterData>(ClientDemoMonsterDataHook)
                .ContinueWith(result => _loadDataHook = result.Result);
        }

        var mainModule = Process.GetCurrentProcess().MainModule;
        if (mainModule == null)
        {
            Logger.Error("Cannot get main module process");
            return;
        }

        _exepath = Path.GetDirectoryName(mainModule.FileName);

        _gameClient = _modLoader.GetController<IGameClient>();
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
        Logger.Debug("Gonna relaunch!");
        // TODO don't hardcode this winky face
        const string exe = @"C:\Programs\Reloaded-II\Reloaded-II.exe";
        var procinfo = new ProcessStartInfo(exe, ["--launch", Path.Combine(_exepath, "MF2.exe")])
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exe),
            CreateNoWindow = true
        };

        _childManager = new ChildProcessManager();
        _child = Process.Start(procinfo)!;
        _childManager.AddProcess(_child);
        _child.EnableRaisingEvents = true;
        // _child.Exited += (sender, args) => { Logger.Debug("Simulator child process has perished. sad face"); };
        var server = new NamedPipeServerStream(ControlPipeName, PipeDirection.InOut);
        Logger.Debug("Waiting for simulation client to connect!");
        await server.WaitForConnectionAsync();
        Logger.Debug("Got connection from client!");

        var reader = new StreamReader(server);
        var writer = new StreamWriter(server);
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
            {
                await Task.Delay(100);
                continue;
            }

            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }
    }

    private async Task SetupClient()
    {
        if (!_gameClient.TryGetTarget(out var game))
        {
            Logger.Error("Could not get game client controller.");
            return;
        }

        game.TickDelay = 0;
        game.SetVsyncEnable(false);
        game.SetFastForward(true);

        var client = new NamedPipeClientStream(ControlPipeName);

        Logger.Debug("Connecting to server");
        await client.ConnectAsync();
        Logger.Debug("Got connection to server!");

        var reader = new StreamReader(client);
        var writer = new StreamWriter(client);

        while (true)
        {
            if (!await ClientWaitForSimRequest(reader))
                continue;

            // and then wait for the simulation result.

            var inCombat = true;
            while (inCombat)
            {
            }
        }
    }

    private async Task<bool> ClientWaitForSimRequest(StreamReader reader)
    {
        // Wait for a sim message from the server
        var line = await reader.ReadLineAsync();
        if (line == null)
        {
            await Task.Delay(100);
            return false;
        }

        // We have a sim message
        var message = line.Split(",");
        var cmd = message[0];
        if (cmd != "SIM")
        {
            Logger.Warn($"Unknown command {cmd}. Ignored.");
            return false;
        }

        var request = new BattleSimRequest
        {
            left = IBattleMonsterData.FromBytes(Convert.FromBase64String(message[1])),
            right = IBattleMonsterData.FromBytes(Convert.FromBase64String(message[2]))
        };

        // Now tell the game thread to start running
        lock (_gameLock)
        {
            _request = request;
        }

        return true;
    }

    private void ClientDemoBattleHook(nuint self)
    {
        _battleHook!.OriginalFunction(self);
        Memory.Instance.Read(nuint.Add(self, 0x34), out short gameMode);
        if (gameMode != 6) Logger.Warn($"Game mode {gameMode} is invalid. Somehow not in the demo battle?");

        var newTimer = 60;
        // The current timer that ticks down is in offset 0x10
        Memory.Instance.Write(nuint.Add(self, 0x10), newTimer);
        // but the original timer value is in offset 0x14
        Memory.Instance.Write(nuint.Add(self, 0x14), newTimer);
    }

    private void ClientDemoMonsterDataHook(nuint self)
    {
        // Run the original code to load the demo monsters
        _loadDataHook!.OriginalFunction(self);

        var maybeGame = _modLoader.GetController<IGame>();
        if (maybeGame == null || !maybeGame.TryGetTarget(out var game)) return;

        // Now we stall until its time to start the demo battle for real.
        IBattleMonsterData left;
        IBattleMonsterData right;
        while (true)
        {
            lock (_gameLock)
            {
                if (_request != null)
                {
                    // Copy the monster data into the battle monsters
                    left = _request.left;
                    right = _request.right;

                    // Clear out the monster to say I'm ready for the next battle
                    _request = null;
                    // Time to start the battle!
                    break;
                }
            }

            // Nothing to do so sleep the game thread and prevent it from continuing
            Thread.Sleep(100);
        }

        // Now actually write the data into the battle monster
        Memory.Instance.Read(nuint.Add(self, 0x58), out nuint leftAddr);
        var leftMon = game.MonsterFromPointer(leftAddr + 8);
        CopyMonsterData(left, leftMon);

        Memory.Instance.Read(nuint.Add(self, 0x68), out nuint rightAddr);
        var rightMon = game.MonsterFromPointer(rightAddr + 8);
        CopyMonsterData(right, rightMon);

        // battle start!
    }

    private void CopyMonsterData(IBattleMonsterData src, IMonster dst)
    {
        dst.Name = src.Name;
        dst.GenusMain = src.GenusMain;
        dst.GenusSub = src.GenusSub;
        dst.Life = src.Life;
        dst.Power = src.Power;
        dst.Intelligence = src.Intelligence;
        dst.Skill = src.Skill;
        dst.Speed = src.Speed;
        dst.Defense = src.Defense;
        dst.ArenaSpeed = src.ArenaSpeed;
        dst.GutsRate = src.GutsRate;
        dst.NatureBase = src.Nature;
        // dst.Moves = src.Techs;
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

internal record BattleSimRequest
{
    public IBattleMonsterData left;
    public IBattleMonsterData right;
}