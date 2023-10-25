using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;

namespace MRDX.Base.Mod.Interfaces;

// Called once per frame to update the current controller state
// Instead of hooking this, prefer using the IController to setup callback events for input
[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 10 53 56 57 8D 4D F0")]
[Function(CallingConventions.Stdcall)]
public delegate void RegisterUserInput();

// Called during training to see if the training animation is complete. Return true to exit training early.
[HookDef(BaseGame.Mr2, Region.Us, "33 C0 39 41 ?? 0F 95 C0")]
[Function(CallingConventions.Fastcall)]
public delegate bool IsTrainingDone(nint self);

// Called from a lot of places. Seems to push the object to the next state.
[HookDef(BaseGame.Mr2, Region.Us, "8A 41 ?? FE 41 ??")]
[Function(CallingConventions.Fastcall)]
public delegate void UpdateGenericState(nint self);

// Called when checking if the monster grabbed the item or not
// This is called by the RuinsTurningPoint state handler for the CModeIsekiMap2D
[HookDef(BaseGame.Mr2, Region.Us, "C6 41 56 00 8D 41 64 C7 41 ?? ?? ?? ??")]
[Function(CallingConventions.Fastcall)]
public delegate void CheckIfMonsterGrabbedAnItem(nint self);

// Called before the game creates the custom client overlay. Passing in 5 to the original function skips drawing the overlay
[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 8B 45 ?? 53 8B D9 89 83 ?? ?? ?? ??")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate nint CreateOverlay(nint self, OverlayDrawMode unkEnum);

// Sets the uniforms and also seems to copy the pointer to the vertex data to another location
[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 83 EC 2C A1 ?? ?? ?? ?? 33 C5 89 45 ?? 56 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 8B F1")]
[Function(CallingConventions.Fastcall)]
public delegate void SetUniform(nint self);

// Takes all the incoming render data, and does the actual OpenGL draw calls for it.
[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 E4 F8 83 EC 64")]
[Function(CallingConventions.Fastcall)]
public delegate void RenderFrameCall(nint self);

// Function that will start playing the FMV that we want to hook to set the volume before it starts playing.
[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 14 8B 45 ?? 56")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate int PlayFmv(nint self, nint unk);

// Function that is called when FMV playback is about to end.
[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 ?? ?? ?? ?? 50 56 A1 ?? ?? ?? ?? 33 C5 50 8D 45 ?? 64 A3 ?? ?? ?? ?? 8B F1 C7 45 ?? 00 00 00 00 C7 06 ?? ?? ?? ??")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate nint StopFmv(nint self, byte shouldDestroy);

/**
 * Called once a frame. TODO add an event in the base mod for OnFrameStart and OnFrameEnd
 */
[HookDef(BaseGame.Mr2, Region.Us, "51 83 3D ?? ?? ?? ?? 00 75 ??")]
[Function(CallingConventions.Stdcall)]
public delegate int FrameStart();

/**
 * The following functions are called when certain boxes are drawn to the screen
 */
[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 8B 89 ?? ?? ?? ?? 56 57 C7 84 24 ?? ?? ?? ?? ?? ?? ?? ?? 8A 41 2A")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate int DrawMonsterCardHitChanceValue(nint self);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 8A 45 08")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate int DrawMonsterCardForceValue(nint self, sbyte force, int unk);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 8B 89 ?? ?? ?? ?? 56 57 C7 84 24 ?? ?? ?? ?? ?? ?? ?? ?? 8A 41 34")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate int DrawMonsterCardSharpnessValue(nint self);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 E4 F8 81 EC ?? ?? ?? ?? A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 8B 89 ?? ?? ?? ?? 56 57 C7 84 24 ?? ?? ?? ?? ?? ?? ?? ?? 8A 41 2B")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate int DrawMonsterCardWitheringValue(nint self);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 E4 F8 81 EC C0 08 00 00 A1 ?? ?? ?? ?? 33 C4 89 84 24 ?? ?? ?? ?? 56 57 8B F2 8B F9 8B 4D ?? 8D 94 24 ?? ?? ?? ?? E8 ?? ?? ?? ?? 33 C0")]
[Function(CallingConventions.Fastcall)]
public delegate int DrawIntWithHorizontalSpacing(short x, short y, int number);

/**
 * Called when setting up the battle controls. CCtrlBattle seems to store things like the battle timer among other things.
 * Its heap allocated so we can't just use a fixed memory address.
 */
[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 83 EC 14 53 56 8B F1 57 6A 01")]
[Function(CallingConventions.Fastcall)]
public delegate void SetupCCtrlBattle(nuint battle);

/**
 * Method call on CCtrlBattle that decrements the battle timer.
 */
[HookDef(BaseGame.Mr2, Region.Us, "8B 41 ?? 85 C0 7E ??")]
[Function(CallingConventions.Fastcall)]
public delegate void DecrementBattleTimer(nint self);

public interface IHooks
{
    Task<IHook<T>> AddHook<T>(T func);

    Task<T> CreateWrapper<T>() where T : Delegate;
}

[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Event, AllowMultiple = true)]
public class HookDefAttribute : Attribute
{
    public HookDefAttribute(BaseGame game, Region region, string signature)
    {
        Game = game;
        Region = region;
        Signature = signature;
    }

    public BaseGame Game { get; }
    public Region Region { get; }
    public string Signature { get; }
}

public static class Utils
{
    /// <summary>
    ///     Use reflection to find the appropriate signature for this memory location
    /// </summary>
    /// <param name="game"></param>
    /// <param name="region"></param>
    /// <typeparam name="T">Delegate or the type that you want to get the hookdef from</typeparam>
    /// <returns>Signature string for this location</returns>
    /// <exception cref="Exception">If no hookdef attribute can be found.</exception>
    public static string GetSignature<T>(BaseGame game, Region region)
    {
        foreach (var attr in typeof(T).GetCustomAttributes().OfType<HookDefAttribute>())
            if ((attr.Game & game) != 0 && (attr.Region & region) != 0)
                return attr.Signature;
        throw new Exception(
            $"Unable to find signature for Hook func {typeof(T)} with Game {game} and Region {region}\n   Make sure you define a hook signature!");
    }
}