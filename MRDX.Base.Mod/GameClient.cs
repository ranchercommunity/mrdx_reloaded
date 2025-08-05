using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace MRDX.Base.Mod;

public partial class GameClient : BaseObject<GameClient>, IGameClient
{
    private readonly Lock _lock = new();
    private readonly nint _tickDelayPtr;
    private IHook<RenderElement>? _hook;

    private int _tickDelay;

    private IAsmHook _tickDelayHook;
    private bool _vsync;
    private bool _vsyncRequested;
    private bool _vsyncTempEnable;

    public GameClient(ModContext context)
    {
        var modLoader = context.ModLoader;
        _tickDelayPtr = Marshal.AllocHGlobal(4);
        _tickDelay = FastForwardOption ? 32000 : 16000; // vanilla value for the tick delay
        Marshal.WriteInt32(_tickDelayPtr, _tickDelay);
        var maybeHooks = modLoader.GetController<IHooks>();
        if (maybeHooks == null || !maybeHooks.TryGetTarget(out var hooks))
        {
            Logger.Error("Unable to load startup scanner! Cant configure fastforward");
            return;
        }

        var startupScanner = modLoader.GetController<IStartupScanner>();
        if (startupScanner == null || !startupScanner.TryGetTarget(out var scanner))
        {
            Logger.Error("Unable to load startup scanner! Cant configure fastforward");
            return;
        }

        scanner.AddMainModuleScan("BF 00 7D 00 00 BA 80 3E 00 00", result =>
        {
            var addr = (nuint)(Base.ExeBaseAddress + result.Offset);
            string[] modifyTickDelay =
            [
                "use32",
                $"mov edx, [{_tickDelayPtr}]"
            ];
            _tickDelayHook =
                new AsmHook(modifyTickDelay, addr, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        hooks.AddHook<RenderElement>(RenderElementImpl).ContinueWith(result => _hook = result.Result);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1677FB)] // offset used when loading the volume
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1677DF)] // offset used for running audio
    public float SoundEffectsVolume
    {
        get => Read<float>();
        set => SafeWriteAll(value);
    }

    [BaseOffset(BaseGame.Mr1, Region.Us, 0xE4C428)]
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x166A60)] // offset used when loading the volume
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1D481D0)] // offset used for running audio
    public float BackgroundVolume
    {
        get => Read<float>();
        set => SafeWriteAll(value);
    }

    public IGameRenderRect RenderBounds { get; set; } = new RenderBounds();
    public IGameRenderRect RenderScaleUniform { get; set; } = new RenderScaleUniform();

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1D01EA1)]
    public bool FastForwardOption
    {
        get => Read<bool>();
        set => Write(value);
    }

    public int TickDelay
    {
        get => _tickDelay;
        set
        {
            _tickDelay = value;
            Marshal.WriteInt32(_tickDelayPtr, value);
        }
    }

    public void SetVsyncEnable(bool enabled)
    {
        lock (_lock)
        {
            _vsyncRequested = true;
            _vsync = enabled;
        }
    }

    public void SetFastForward(bool ff)
    {
        FastForwardOption = ff;

        var director = GetDirectorInstance();
        if (ff)
        {
            SetAnimationInterval(director, 1.0f / 9999);
            if (_vsync) _vsyncRequested = true;
        }
        else
        {
            SetAnimationInterval(director, 1.0f / 60);
            _vsyncRequested = true;
            _vsyncTempEnable = true;
        }
    }

    [LibraryImport("libcocos2d.dll", EntryPoint = "?getInstance@Director@cocos2d@@SAPAV12@XZ")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetDirectorInstance();

    [LibraryImport("libcocos2d.dll", EntryPoint = "?setAnimationInterval@Director@cocos2d@@QAEXM@Z")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvThiscall)])]
    private static partial nint SetAnimationInterval(nint director, float interval);

    private void RenderElementImpl(nint self)
    {
        _hook?.OriginalFunction(self);

        var enable = false;
        lock (_lock)
        {
            if (!_vsyncRequested)
                return;
            enable = _vsync;
            _vsyncTempEnable = false;
            _vsyncRequested = false;
        }

        Wgl.SwapIntervalEXT(_vsyncTempEnable || enable ? 1 : 0);
    }
}

[BaseOffset(BaseGame.Mr2, Region.Us, 0x1684E5)]
internal class RenderScaleUniform : BaseObject<RenderScaleUniform>, IGameRenderRect
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 8)]
    public float Width
    {
        get => Read<float>() * -2;
        set => SafeWrite(value / -2.0f);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0)]
    public float Height
    {
        get => Read<float>() * -2;
        set => SafeWrite(value / -2.0f);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 23)]
    public float WidthScale
    {
        get => 2.0f / Read<float>();
        set => SafeWrite(2.0f / value);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 16)]
    public float HeightScale
    {
        get => 2.0f / Read<float>();
        set => SafeWrite(2.0f / value);
    }
}

[BaseOffset(BaseGame.Mr2, Region.Us, 0x165C93)]
internal class RenderBounds : BaseObject<RenderBounds>, IGameRenderRect
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 7)]
    public float Width
    {
        get => Read<float>();
        set => SafeWrite(value);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0)]
    public float Height
    {
        get => Read<float>();
        set => SafeWrite(value);
    }

    public float WidthScale
    {
        get => throw new NotSupportedException("Cannot Get WidthScale on RenderBounds");
        set => throw new NotSupportedException("Cannot Set WidthScale on RenderBounds");
    }

    public float HeightScale
    {
        get => throw new NotSupportedException("Cannot Get HeightScale on RenderBounds");
        set => throw new NotSupportedException("Cannot Set HeightScale on RenderBounds");
    }
}