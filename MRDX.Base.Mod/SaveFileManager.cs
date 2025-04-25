using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Base.Mod;

public record struct SaveFileEntry(string Filename, string Slot, bool IsAutoSave) : ISaveFileEntry;

public class SaveFileManager : ISaveFile
{
    private string _filename = "";
    private bool _saveDataGameLoaded;
    private byte _saveDataReadCount;
    private string _saveDataSlot = "";

    private IHook<UpdateGenericState>? _updateHook;

    public SaveFileManager(IModLoader loader)
    {
        var redirector = loader.GetController<IRedirectorController>();
        if (redirector != null && redirector.TryGetTarget(out var re))
            re.Loading += SaveDataMonitor;
        else
            Logger.Error("Failed to get redirection controller to watch save files.");

        loader.GetController<IHooks>().TryGetTarget(out var hooks);
        if (hooks != null)
            hooks.AddHook<UpdateGenericState>(CheckForSaveLoad)
                .ContinueWith(result => _updateHook = result.Result.Activate());
        else
            Logger.Error("Failed to get hooks to watch for loading files.");
    }

    public event ISaveFile.Load? OnLoad;
    public event ISaveFile.Save? OnSave;

    /// <summary>
    ///     Tracks the files read by the game. There is a file load/hook signature we can use until we find the save
    ///     function/load functions to duplicate this effect.
    ///     3+ Access followed by a psdata access is for saving.
    ///     3+ Access followed by a GameEventHook is for loading.
    /// </summary>
    private void SaveDataMonitor(string filename)
    {
        _filename = filename;
        // && (!filename.Contains("614") || SaveDataAutosavesEnabled)
        if (filename.Contains("BISLPS-"))
        {
            var newSlot = filename[^2..];

            if (_saveDataSlot != newSlot)
            {
                _saveDataSlot = newSlot;
                _saveDataReadCount = 0;
            }

            _saveDataReadCount++;

            if (_saveDataReadCount >= 4)
                _saveDataGameLoaded = true;
        }
        else if (filename.Contains("psdata001.bin"))
        {
            if (_saveDataReadCount <= 3) return;
            OnSave?.Invoke(new SaveFileEntry(filename, _saveDataSlot, filename.Contains("614")));
            _saveDataGameLoaded = false;
        }
        else
        {
            _saveDataReadCount = 0;
            _saveDataGameLoaded = false;
        }
    }

    private void CheckForSaveLoad(nint parent)
    {
        _updateHook!.OriginalFunction(parent);
        if (!_saveDataGameLoaded) return;
        OnLoad?.Invoke(new SaveFileEntry(_filename, _saveDataSlot, _saveDataSlot.Contains("614")));
        _saveDataReadCount = 0;
        _saveDataGameLoaded = false;
    }
}