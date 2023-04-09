using System;

namespace MRDX.Base.Mod.Interfaces;

[Flags]
public enum ButtonFlags : ushort
{
    None = 0,
    LTrigger = 1 << 0,
    RTrigger = 1 << 1,

    Circle = 1 << 4,
    Cross = 1 << 5,
    Triangle = 1 << 6,
    Square = 1 << 7,

    Start = 1 << 8,

    DpadUp = 1 << 12,
    DpadRight = 1 << 13,
    DpadDown = 1 << 14,
    DpadLeft = 1 << 15
}

public interface IInput
{
    /// <summary>
    ///     Contains the currently pressed buttons at any point.
    /// </summary>
    ButtonFlags Buttons { get; set; }
}

public interface IController
{
    /// <summary>
    ///     Event used for manipulating the inputs sent to the game.
    /// </summary>
    /// <param name="inputs">The inputs structure to be sent to the game.</param>
    public delegate void InputEvent(ref IInput inputs);

    /// <summary>
    ///     Event used for reading the inputs sent to the game right before they are sent to the game.
    ///     Modifying the inputs structure has no effect on the game.
    /// </summary>
    /// <param name="inputs">The inputs structure to be sent to the game.</param>
    public delegate void OnInputEvent(IInput inputs);

    IInput Current { get; }

    /// <summary>
    ///     This event allows you to send inputs to be registered by the game.
    ///     Simply edit the passed by reference Inputs structure.
    /// </summary>
    event InputEvent SetInput;

    /// <summary>
    ///     This event is only invoked when inputs have changed since last frame.
    ///     Editing the input will update the game input.
    /// </summary>
    event InputEvent InputChanged;

    /// <summary>
    ///     Same as <see cref="SetInputs" /> but executed after <see cref="SetInputs" />.
    ///     Intended use is to provide post processing of inputs supplied by other implementations.
    ///     e.g. Flip axis, etc.
    /// </summary>
    event InputEvent PostProcessInput;

    /// <summary>
    ///     This event allows you to receive a copy of the inputs before they are sent to the game.
    /// </summary>
    event OnInputEvent OnInput;

    /// <summary>
    ///     This event is only called when inputs have changed since the last frame.
    ///     Editing the input field will NOT affect the game input.
    /// </summary>
    event OnInputEvent OnInputChanged;
}