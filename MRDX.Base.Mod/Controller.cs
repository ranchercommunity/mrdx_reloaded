using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class Controller : IController
{
    private ButtonFlags _currentCache = 0;

    private IHook<RegisterUserInput>? _hook;

    private IInput _input = new Input();
    private ILogger _logger;
    private ButtonFlags _previousCache = 0;

    public Controller(ModContext context)
    {
        _logger = context.Logger;
        context.ModLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks?.AddHook<RegisterUserInput>(ProcessControllerEvents)
            .ContinueWith(result => _hook = result.Result?.Activate());
    }

    public IInput Current => _input;
    public event IController.InputEvent? SetInput;
    public event IController.InputEvent? InputChanged;
    public event IController.InputEvent? PostProcessInput;
    public event IController.OnInputEvent? OnInput;
    public event IController.OnInputEvent? OnInputChanged;

    private void ProcessControllerEvents()
    {
        _hook?.OriginalFunction();

        _currentCache = Current.Buttons;
        var read = new ReadonlyInput(_currentCache);

        if (_currentCache != _previousCache) InputChanged?.Invoke(ref _input);

        SetInput?.Invoke(ref _input);
        PostProcessInput?.Invoke(ref _input);

        OnInput?.Invoke(read);
        if (_currentCache != _previousCache) OnInputChanged?.Invoke(read);

        _previousCache = _currentCache;
    }
}

internal class Input : BaseObject<Input>, IInput
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x3723B0)] // offset for current buttons
    public ButtonFlags Buttons
    {
        get => (ButtonFlags)Read<ushort>();
        set => Write((ushort)value);
    }
}

internal class ReadonlyInput : IInput
{
    public ReadonlyInput(ButtonFlags buttons)
    {
        Buttons = buttons;
    }

    public ButtonFlags Buttons { get; set; }
}