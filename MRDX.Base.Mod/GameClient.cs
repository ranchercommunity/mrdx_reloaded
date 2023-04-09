using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.Mod;

public class GameClient : BaseObject<GameClient>, IGameClient
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1677FB)] // offset used when loading the volume
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1677DF)] // offset used for running audio
    public float SoundEffectsVolume
    {
        get => Read<float>("SoundEffectsVolume");
        set => SafeWriteAll("SoundEffectsVolume", value);
    }

    [BaseOffset(BaseGame.Mr1, Region.Us, 0xE4C428)]
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x166A60)] // offset used when loading the volume
    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1D481D0)] // offset used for running audio
    public float BackgroundVolume
    {
        get => Read<float>("BackgroundVolume");
        set => SafeWriteAll("BackgroundVolume", value);
    }

    public IGameRenderRect RenderBounds { get; set; } = new RenderBounds();
    public IGameRenderRect RenderScaleUniform { get; set; } = new RenderScaleUniform();

    [BaseOffset(BaseGame.Mr2, Region.Us, 0x1D01EA1)]
    public bool FastForwardOption
    {
        get => Read<bool>("FastForwardOption");
        set => Write("FastForwardOption", value);
    }
}

[BaseOffset(BaseGame.Mr2, Region.Us, 0x1684E5)]
internal class RenderScaleUniform : BaseObject<RenderScaleUniform>, IGameRenderRect
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 8)]
    public float Width
    {
        get => Read<float>("Width") * -2;
        set => SafeWrite("Width", value / -2.0f);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0)]
    public float Height
    {
        get => Read<float>("Height") * -2;
        set => SafeWrite("Height", value / -2.0f);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 23)]
    public float WidthScale
    {
        get => 2.0f / Read<float>("WidthScale");
        set => SafeWrite("WidthScale", 2.0f / value);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 16)]
    public float HeightScale
    {
        get => 2.0f / Read<float>("HeightScale");
        set => SafeWrite("HeightScale", 2.0f / value);
    }
}

[BaseOffset(BaseGame.Mr2, Region.Us, 0x165C93)]
internal class RenderBounds : BaseObject<RenderBounds>, IGameRenderRect
{
    [BaseOffset(BaseGame.Mr2, Region.Us, 7)]
    public float Width
    {
        get => Read<float>("Width");
        set => SafeWrite("Width", value);
    }

    [BaseOffset(BaseGame.Mr2, Region.Us, 0)]
    public float Height
    {
        get => Read<float>("Height");
        set => SafeWrite("Height", value);
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