using System;

namespace MRDX.Base.Mod.Interfaces;

public enum LifeType : byte
{
    Normal = 0,
    Precocious = 1,
    LateBloom = 2,
    Sustainable = 3,
    Prodigy = 4
}

public enum LifeStage : byte
{
    Baby = 0,
    Child = 1,
    Adolescent = 2,
    LateAdolescent = 3,
    Prime = 4,
    SubPrime = 5,
    Elder = 6,
    LateElder = 7,
    OldAge = 8,
    Twilight = 9
}

public enum PlaytimeType : byte
{
    MudFight = 0,
    SumoBattle = 1,
    Sparring = 2
}

[Flags]
public enum BattleSpecials
{
    None = 0,
    Power = 1 << 0,
    Anger = 1 << 1,
    Grit = 1 << 2,
    Will = 1 << 3,
    Fight = 1 << 4,
    Fury = 1 << 5,
    Guard = 1 << 6,
    Ease = 1 << 7,
    Hurry = 1 << 8,
    Vigor = 1 << 9,
    Real = 1 << 10,
    Drunk = 1 << 11,
    Unity = 1 << 12,
    Hax1 = 1 << 13,
    Hax2 = 1 << 14,
    Hax3 = 1 << 15
}

public enum Item : byte
{
    Potato = 0,
    Fish = 1,
    Meat = 2,
    Milk = 3,
    CupJelly = 4,
    Tablet = 5,
    Sculpture = 6,
    GeminisPot = 7,
    LumpofIce = 8,
    FireStone = 9,
    PureGold = 10,
    PureGoldSuezoSpecial = 11,
    PureSilver = 12,
    PurePlatina = 13,
    Mango = 14,
    Candy = 15,
    SmokedSnake = 16,
    AppleCake = 17,
    MintLeaf = 18,
    Powder = 19,
    SweetJelly = 20,
    SourJelly = 21,
    CrabsClaw = 22,
    NutsOil = 23,
    NutsOilHad = 24,
    StarPrune = 25,
    GoldPeach = 26,
    SilverPeach = 27,
    MagicBanana = 30,
    HalfEaten = 31,
    Irritater = 32,
    Griever = 33,
    Teromeann = 34,
    Manseitan = 35,
    Larox = 36,
    Kasseitan = 37,
    Troron = 38,
    Nageel = 39,
    TorokachinFx = 40,
    Paradoxine = 41,
    DiscChips = 57,
    BayShrimp = 58,
    Incense = 59,
    Shoes = 60,
    RiceCracker = 61,
    Tobacco = 62,
    OliveOil = 63,
    Kaleidoscope = 64,
    TorlesWater = 65,
    BrokenPipe = 66,
    Perfume = 67,
    StickRed = 68,
    Bone = 69,
    PerfumeOil = 70,
    WoolBall = 71,
    CedarLog = 72,
    PileOfMeat = 73,
    Soil = 74,
    RockCandy = 75,
    TrainingDummy = 76,
    IceOfPapas = 77,
    Grease = 78,
    DnaCapsule = 85,
    GodsSlate = 86,
    HeroBadge = 87,
    HeelBadge = 88,
    QuackDoll = 89,
    DragonTusk = 90,
    OldSheath = 91,
    DoubleEdged = 92,
    MagicPot = 93,
    Mask = 94,
    BigFootstep = 95,
    BigBoots = 96,
    FireFeather = 97,
    TaurusHorn = 98,
    DinosTail = 99,
    ZillaBeard = 100,
    FunCan = 101,
    StrongGlue = 102,
    QuackDoll2 = 103,
    MysticSeed = 104,
    ParepareTea = 105,
    Match = 106,
    ToothPick = 107,
    Playmate = 108,
    WhetStone = 109,
    Polish = 110,
    SilkCloth = 111,
    DiscDish = 112,
    Gramophone = 113,
    ShinyStone = 114,
    Meteorite = 115,
    SteamedBun = 116,
    RazorBlade = 117,
    IceCandy = 118,
    FishBone = 119,
    SunLamp = 120,
    SilkHat = 121,
    HalfCake = 122,
    ShavedIce = 123,
    SweetPotato = 124,
    Medal = 125,
    GoldMedal = 126,
    SilverMedal = 127,
    MusicBox = 128,
    Medallion = 129,
    Nothing = 130,
    Battle = 131,
    Rest = 132,
    Play = 133,
    Mirror = 134,
    ColartTea = 135,
    GaloeNut = 136,
    StickOfIce = 137,
    OceanStone = 138,
    Seaweed = 139,
    ClayDoll = 142,
    MockNut = 143,
    ColtsCake = 144,
    Flower = 145,
    DiscChips2 = 167,
    GaliMask = 168,
    Crystal = 169,
    UndineSlate = 170,
    Money = 172,
    StickGreen = 173,
    CupJellyD = 174,
    Spear = 175,
    WrackyDoll = 176,
    QuackDoll3 = 177
}

public enum MonsterGenus : byte
{
    Pixie = 0,
    Dragon = 1,
    Centaur = 2,
    ColorPandora = 3,
    Beaclon = 4,
    Henger = 5,
    Wracky = 6,
    Golem = 7,
    Zuum = 8,
    Durahan = 9,
    Arrowhead = 10,
    Tiger = 11,
    Hopper = 12,
    Hare = 13,
    Baku = 14,
    Gali = 15,
    Kato = 16,
    Zilla = 17,
    Bajarl = 18,
    Mew = 19,
    Phoenix = 20,
    Ghost = 21,
    Metalner = 22,
    Suezo = 23,
    Jill = 24,
    Mocchi = 25,
    Joker = 26,
    Gaboo = 27,
    Jell = 28,
    Undine = 29,
    Niton = 30,
    Mock = 31,
    Ducken = 32,
    Plant = 33,
    Monol = 34,
    Ape = 35,
    Worm = 36,
    Naga = 37,
    Count = 38,
    Unknown1 = 38,
    Unknown2 = 39,
    Unknown3 = 40,
    Unknown4 = 41,
    Unknown5 = 42,
    Unknown6 = 43
}

public enum Form : sbyte
{
    ErrorLow = -101,
    Skinny = -60,
    Slim = -20,
    Normal = 19,
    Fat = 59,
    Plump = 100,
    ErrorHigh = sbyte.MaxValue
}

public enum EffectiveNature : sbyte
{
    ErrorLow = -101,
    Worst = -60,
    Bad = -20,
    Neutral = 19,
    Good = 59,
    Best = 100,
    ErrorHigh = sbyte.MaxValue
}

public enum SpecialTech : byte
{
    Recovery,
    HpDrain,
    GutsDrain,
    GutsHpDrain,
    SelfDamage,
    SelfDamageMiss
}

public enum TechType : byte
{
    Power,
    Intelligence
}

public enum ErrantryType : byte
{
    Basic = 0,
    Skill = 1,
    Heavy = 2,
    Withering = 3,
    Sharp = 4,
    Special = 5
}

public enum TechRange : byte
{
    Melee = 0,
    Short = 1,
    Medium = 2,
    Long = 3,
    Count
}

public enum TechNature : byte
{
    Neutral = 0,
    Good = 1,
    Evil = 2
}

public record MonsterInfo
{
    public MonsterGenus Id { get; init; } = 0;
    public string Name { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
}

public record MonsterBreed
{
    public MonsterGenus Main { get; init; } = 0;
    public MonsterGenus Sub { get; init; } = 0;
    public string Name { get; init; } = string.Empty;
}