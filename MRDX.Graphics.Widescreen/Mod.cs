using System.Diagnostics;
using System.Runtime.InteropServices;
using MRDX.Base.Mod.Interfaces;
using MRDX.Graphics.Widescreen.Template;
using Reloaded.Hooks.Definitions;
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


    private readonly WeakReference<IGameClient> _gameClient;
    // private IHook<SetUniform>? _setUniformHook;
    // private IHook<RenderFrameCall>? _renderFrameCallHook;

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
        const float originalWidth = 320.0f;
        var newWidth = CalculateNewWidth(originalWidth, ratio);
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