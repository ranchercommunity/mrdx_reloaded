using System.Diagnostics;
using System.Runtime.InteropServices;
using MRDX.Base.Mod.Interfaces;
using MRDX.Graphics.Widescreen.Template;
using Reloaded.Hooks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace MRDX.Graphics.Widescreen;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    ///     Provides access to the Reloaded logger.
    /// </summary>
    private static ILogger _logger = null!;

    /// <summary>
    ///     Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

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

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    private IHook<CreateOverlay>? _createOverlayHook;

    private const float OriginalWidth = 320.0f;

    private readonly WeakReference<IGameClient> _gameClient;
    // private IHook<SetUniform>? _setUniformHook;
    // private IHook<RenderFrameCall>? _renderFrameCallHook;

    private nint _skyboxEndYPtr;
    private nint _skyboxStartXPtr;
    private nint _skyboxEndXPtr;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _gameClient = _modLoader.GetController<IGameClient>();

        _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks!.AddHook<CreateOverlay>(CreateOverlayHook)
            .ContinueWith(result => _createOverlayHook = result.Result?.Activate());
        UpdateWindowBounds(_configuration.AspectRatio);
        CalculateSkyboxCoords(_configuration.AspectRatio);
        var _startupScanner = _modLoader.GetController<IStartupScanner>();
        if (_startupScanner != null && _startupScanner.TryGetTarget(out var scanner))
        {
            InitSkyboxHooks(scanner);
        }
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private float CalculateNewWidth(float width, Config.AspectRatioEnum ratio)
    {
        var originalAspectRatio = ConvertAspectRatio(Config.AspectRatioEnum.Force_4_3);
        var newAspectRatio = ConvertAspectRatio(ratio);
        return width * newAspectRatio / originalAspectRatio;
    }

    private void UpdateWindowBounds(Config.AspectRatioEnum ratio)
    {
        var newWidth = CalculateNewWidth(OriginalWidth, ratio);
        _gameClient.TryGetTarget(out var gameClient);

        gameClient!.RenderBounds.Width = newWidth;
        gameClient.RenderScaleUniform.WidthScale = newWidth;
    }

    private static float ConvertAspectRatio(Config.AspectRatioEnum val)
    {
        switch (val)
        {
            default:
            case Config.AspectRatioEnum.Auto:
                // TODO adjust window size to match monitor aspect ratio as well?
                // return CalculateAspectRatio();
                return 16.0f / 9;
            case Config.AspectRatioEnum.Force_4_3:
                return 4.0f / 3;
            case Config.AspectRatioEnum.Force_16_9:
                return 16.0f / 9;
        }
    }

    private static float CalculateAspectRatio()
    {
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        GetWindowRect(hwnd, out var rect);
        return (float)(rect.Right - rect.Left) / (rect.Bottom - rect.Top);
    }

    private nint CreateOverlayHook(nint self, OverlayDrawMode drawMode)
    {
        _logger!.WriteLine($"[] original drawmode value: {(uint)drawMode}");
        return _createOverlayHook!.OriginalFunction(self, OverlayDrawMode.NoOverlay);
    }

    private void CalculateSkyboxCoords(Config.AspectRatioEnum ratio)
    {
        var newWidth = CalculateNewWidth(OriginalWidth, ratio);

        _skyboxEndYPtr = Marshal.AllocHGlobal(2);
        short newYVal = 0x78;
        Marshal.WriteInt16(_skyboxEndYPtr, newYVal);

        _skyboxStartXPtr = Marshal.AllocHGlobal(2);
        short newXVal = (short)(newWidth / 2 * -1);
        Marshal.WriteInt16(_skyboxStartXPtr, newXVal);

        _skyboxEndXPtr = Marshal.AllocHGlobal(2);
        short newXVal1 = (short)(newWidth / 2);
        Marshal.WriteInt16(_skyboxEndXPtr, newXVal1);
    }

    private void InitSkyboxHooks(IStartupScanner scanner)
    {
        // CFarBack skybox (farm & town)
        scanner.AddMainModuleScan("BB ?? ?? ?? ?? 57 8B F9 C6 46 03 08 C6 46 07 38", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov bx, [{_skyboxStartXPtr}]",
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.AddMainModuleScan("C7 46 08 60 FF 88 FF C7 46 10 A0 00 88 FF", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov ax, [{_skyboxStartXPtr}]",
                $"mov [esi+0x8], ax",
                $"mov ax, [{_skyboxEndXPtr}]",
                $"mov [esi+0x10], ax"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter, 14).Activate();
        });

        scanner.AddMainModuleScan("66 89 5E 18 B9 A0 00 00 00 0F B7 47 0A", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov cx, [{_skyboxEndXPtr}]"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.AddMainModuleScan("0F B7 47 0A 66 89 46 1A 66 89 4E 20 0F B7 47 0A", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov ax, [{_skyboxEndYPtr}]",
                $"mov [edi+0xA], eax"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        // CBackSky skybox (battles)
        scanner.AddMainModuleScan("B9 A0 00 00 00 25 FF FF FF 00 66 89 4C 24 28", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov cx, [{_skyboxEndXPtr}]",
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.AddMainModuleScan("BA 60 FF FF FF B8 01 00 00 00 66 89 54 24 30", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"mov dx, [{_skyboxStartXPtr}]"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.AddMainModuleScan("C7 44 24 1E AA 38 60 FF 0F B7 C0", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"push dx",
                $"mov dx, [{_skyboxStartXPtr}]",
                $"mov [esp+0x22], dx",
                $"pop dx"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.AddMainModuleScan("C7 44 24 3C A0 00 78 00 E8 ?? ?? ?? ??", result =>
        {
            string[] modifyCoords =
            {
                $"use32",
                $"push dx",
                $"mov dx, [{_skyboxEndXPtr}]",
                $"mov [esp+0x3E], dx",
                $"pop dx"
            };

            nuint addr = (nuint)(MRDX.Base.Mod.Base.ExeBaseAddress + result.Offset);
            new AsmHook(modifyCoords, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });
    }

    // TODO figure out how the game generates the HUD and background to move/rescale those independently
    // private static OpenTK.Graphics.ProgramHandle? _glProgram;
    // private static int _glUniformLocation = -1;

    // private void SetUniformHook(nint self)
    // {
    //     // First call the original uniform writing function
    //     _setUniformHook!.OriginalFunction(self);
    //     // Then use some GL commands to get the program and uniform location if we don't have them already
    //     if (_glProgram != null && _glUniformLocation != -1) return;
    //     
    //     var glProgramId = -1;
    //     OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.CurrentProgram, ref glProgramId);
    //     _glProgram = new OpenTK.Graphics.ProgramHandle(glProgramId);
    //     _glUniformLocation = OpenTK.Graphics.OpenGL.GL.GetUniformLocation(_glProgram.Value, "u_screen");
    // }
    //
    // private void RenderFrameCallHook(nint self)
    // {
    //     _renderFrameCallHook!.OriginalFunction(self);
    // }

    // TODO: Use this stuff to autodetect the player window size?
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect([In] nint hWnd, [Out] out RECT lpRect);

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        UpdateWindowBounds(configuration.AspectRatio);
        CalculateSkyboxCoords(configuration.AspectRatio);
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left; // x position of upper-left corner
        public int Top; // y position of upper-left corner
        public int Right; // x position of lower-right corner
        public int Bottom; // y position of lower-right corner
    }
}