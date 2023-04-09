using System.Reflection;
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
    private readonly WeakReference<IStartupScanner>? _startupScanner;

    public Hooks(ModContext context)
    {
        _hooks = context.Hooks;
        _logger = context.Logger;
        _startupScanner = context.ModLoader.GetController<IStartupScanner>();
    }

    public Task<IHook<T>?> AddHook<T>(T func)
    {
        var promise = new TaskCompletionSource<IHook<T>?>();
        if (_startupScanner != null && _startupScanner.TryGetTarget(out var scanner))
        {
            var signature = GetSignature(func);
            scanner.AddMainModuleScan(signature, result =>
            {
                _logger.WriteLine($"[Hooks] Created hook for {func} @ {Base.ExeBaseAddress + result.Offset}");
                promise.SetResult(_hooks?.CreateHook(func, Base.ExeBaseAddress + result.Offset));
            });
        }
        else
        {
            _logger.WriteLine("[Hooks] Error: Failed to get scanner!");
            promise.SetCanceled();
        }

        return promise.Task;
    }

    private string GetSignature<T>(T func)
    {
        foreach (var attr in func!.GetType().GetCustomAttributes().OfType<HookDefAttribute>())
            if (attr.Game == Base.Game && attr.Region == Base.Region)
                return attr.Signature;
        throw new Exception(
            $"Unable to find signature for Hook func {func} with Game {Base.Game} and Region {Base.Region}n \n   Make sure you define a hook signature!");
    }
}