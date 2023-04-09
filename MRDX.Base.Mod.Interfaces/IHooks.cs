using System;
using System.Threading.Tasks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace MRDX.Base.Mod.Interfaces;

// Called once per frame to update the current controller state
// Instead of hooking this, prefer using the IController to setup callback events for input
[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 10 53 56 57 8D 4D F0")]
[Function(CallingConventions.Stdcall)]
public delegate void RegisterUserInput();

// Called during training to see if the training animation is complete. Return true to exit training early.
[HookDef(BaseGame.Mr2, Region.Us, "33 C0 39 41 ?? 0F 95 C0")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate bool IsTrainingDone(nint self);

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

public interface IHooks
{
    Task<IHook<T>?> AddHook<T>(T func);
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

    public BaseGame Game { get; private set; }
    public Region Region { get; private set; }
    public string Signature { get; private set; }
}