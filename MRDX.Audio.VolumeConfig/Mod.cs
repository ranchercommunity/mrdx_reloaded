using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using CoreAudio;
using MRDX.Audio.VolumeConfig.Template;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Audio.VolumeConfig;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private readonly WeakReference<IGameClient>? _gameClient;

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

    private readonly float _originalVolume = -1.0f;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    // private static float _currentFmvVolume;
    private bool _isPlayingFmv;

    // private readonly nint _musicVolumeAddress;
    // private readonly nint _sfxVolumeAddress;
    // private readonly nint _sfxVolumeMr2Address;
    // // private static nint _mediaAddress;
    // private readonly bool _isMr2Dx;

    private IHook<PlayFmv>? _playFmvHook;
    private IHook<StopFmv>? _stopFmvHook;
    private SimpleAudioVolume? _volumeControl;

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

        if (hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get hook controller.", Color.Red);
            return;
        }

        hooks.AddHook<PlayFmv>(PlayFmvHook).ContinueWith(result => _playFmvHook = result.Result);
        hooks.AddHook<StopFmv>(StopFmvHook).ContinueWith(result => _stopFmvHook = result.Result);

        var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());
        var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _originalVolume = device.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 1.0f;

        WriteVolumeToMemory(_configuration);
        _logger.WriteLine($"[{_modConfig.ModId}] done building mod");
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        WriteVolumeToMemory(configuration);
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    #region Custom Code for writing the volume to memory

    private void WriteVolumeToMemory(Config configuration)
    {
        if (_gameClient == null || !_gameClient.TryGetTarget(out var client)) return;
        client.SoundEffectsVolume = ClampVolume(configuration.SfxVolume);
        client.BackgroundVolume = ClampVolume(configuration.MusicVolume);

        if (!_isPlayingFmv || _volumeControl == null) return;
        _volumeControl.MasterVolume = ClampVolume(configuration.FmvVolume);
    }

    private static float ClampVolume(int volume)
    {
        return Math.Max(0, Math.Min(100, volume)) / 100.0f;
    }

    private int PlayFmvHook([In] nint self, [In] nint unk)
    {
        _isPlayingFmv = true;
        if (_volumeControl == null)
        {
            // We have to delay finding the current audio stream until after the client starts up the Media Foundation session
            var device = GetDefaultAudioDevice();
            foreach (var session in device.AudioSessionManager2!.Sessions!)
            {
                var proc = Process.GetProcessById((int)session.ProcessID);
                if (!proc.ProcessName.Equals("mf2", StringComparison.CurrentCultureIgnoreCase)) continue;

                _volumeControl = session.SimpleAudioVolume;
                break;
            }
        }

        if (_volumeControl != null)
            _volumeControl.MasterVolume = ClampVolume(_configuration.FmvVolume);
        return _playFmvHook!.OriginalFunction(self, unk);
    }

    private nint StopFmvHook([In] nint self, [In] byte shouldDestroy)
    {
        _isPlayingFmv = false;
        if (_volumeControl != null)
            _volumeControl.MasterVolume = _originalVolume;
        return _stopFmvHook!.OriginalFunction(self, shouldDestroy);
    }

    private static readonly Guid GUID = Guid.NewGuid();

    private static MMDevice GetDefaultAudioDevice()
    {
        var deviceEnumerator = new MMDeviceEnumerator(GUID);
        return deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    #endregion
}