using System;

namespace MRDX.Base.Mod.Interfaces;

[Flags]
public enum BaseGame
{
    Mr1 = 1 << 0,
    Mr2 = 1 << 1
}

[Flags]
public enum Region
{
    Us = 1 << 0,
    Japan = 1 << 1
}

public enum OverlayDrawMode
{
    Unknown0 = 0,
    MainMode = 1,
    Unknown2 = 2,
    Unknown3 = 3,
    Unknown4 = 4,
    NoOverlay = 5
}

public interface IGameRenderRect
{
    float Width { get; set; }
    float Height { get; set; }
    float WidthScale { get; set; }
    float HeightScale { get; set; }
}

public interface IGameClient
{
    float SoundEffectsVolume { get; set; }
    float BackgroundVolume { get; set; }

    IGameRenderRect RenderBounds { get; set; }
    IGameRenderRect RenderScaleUniform { get; set; }

    bool FastForwardOption { get; set; }

    int TickDelay { get; set; }

    void SetVsyncEnable(bool enabled);

    void SetFastForward(bool value);
}