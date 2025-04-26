using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class Hooks : IHooks
{
    private readonly IReloadedHooks? _hooks;
    private readonly ILogger _logger;
    private readonly IModConfig _modConfig;
    private readonly WeakReference<IStartupScanner>? _startupScanner;

    public Hooks(ModContext context)
    {
        _modConfig = context.ModConfig;
        _logger = context.Logger;
        if (context.Hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Reloaded Hooks is null.", Color.Red);
            return;
        }

        _hooks = context.Hooks;
        _startupScanner = context.ModLoader.GetController<IStartupScanner>();
    }

    public Task<IHook<T>> AddHook<T>(T hookFunc)
    {
        var promise = new TaskCompletionSource<IHook<T>>();
        if (_startupScanner != null && _startupScanner.TryGetTarget(out var scanner))
        {
            var signature = Utils.GetSignature<T>(Base.Game, Base.Region);
            scanner.AddMainModuleScan(signature, result =>
            {
                Logger.Debug($"Created hook for {hookFunc} @ {Base.ExeBaseAddress + result.Offset}");
                promise.SetResult(_hooks.CreateHook(hookFunc, Base.ExeBaseAddress + result.Offset));
            });
        }
        else
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Error: Failed to get scanner!");
            promise.SetCanceled();
        }

        return promise.Task;
    }

    public Task<T> CreateWrapper<T>() where T : Delegate
    {
        var promise = new TaskCompletionSource<T>();
        if (_hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Reloaded Hooks is null.");
            promise.SetCanceled();
            return promise.Task;
        }

        var hook = _hooks!;
        if (_startupScanner != null && _startupScanner.TryGetTarget(out var scanner))
        {
            var signature = Utils.GetSignature<T>(Base.Game, Base.Region);
            scanner.AddMainModuleScan(signature, result =>
            {
                _logger.WriteLine(
                    $"[{_modConfig.ModId}] Created wrapper for {typeof(T)} @ {Base.ExeBaseAddress + result.Offset}");
                promise.SetResult(hook.CreateWrapper<T>(Base.ExeBaseAddress + result.Offset, out _));
            });
        }
        else
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Error: Failed to get scanner!");
            promise.SetCanceled();
        }

        return promise.Task;
    }
}