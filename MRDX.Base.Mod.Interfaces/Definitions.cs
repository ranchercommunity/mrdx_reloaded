using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;

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

[Flags]
public enum TrainingBoosts {
    None = 0,
    Domino = 1 << 0,
    Study = 1 << 1,
    Run = 1 << 2,
    Shoot = 1 << 3,
    Dodge = 1 << 4,
    Endure = 1 << 5,
    Pull = 1 << 6,
    Meditate = 1 << 7,
    Leap = 1 << 8,
    Swim = 1 << 9,
    TorbleSea = 1 << 10,
    PapasMountain = 1 << 11,
    MandyDesert = 1 << 12,
    ParepareJungle = 1 << 13,
    KawreaVolcano = 1 << 14,
    Hax1 = 1 << 15
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
    DiscChipsTiger = 42,
    DiscChipsHopper = 43,
    DiscChipsHare = 44,
    DiscChipsKato = 45,
    DiscChipsSuezo = 46,
    DiscChipsNaga = 47,
    DiscChipsGaboo = 48,
    DiscChipsJell = 49,
    DiscChipsNiton = 50,
    DiscChipsPlant = 51,
    DiscChipsMew = 52,
    DiscChipsMocchi = 53,
    DiscChipsZuum = 54,
    DiscChipsArrowhead = 55,
    DiscChipsApe = 56,
    DiscChipsMonol = 57,
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
    DnaCapsuleRed = 79,
    DnaCapsuleYellow = 80,
    DnaCapsulePink = 81,
    DnaCapsuleGray = 82,
    DnaCapsuleWhite = 83,
    DnaCapsuleGreen = 84,
    DnaCapsuleBlack = 85,
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
    DiscChipsPixie = 146,
    DiscChipsDragon = 147,
    DiscChipsCentaur = 148,
    DiscChipsColorP = 149,
    DiscChipsBeaclon = 150,
    DiscChipsHenger = 151,
    DiscChipsWracky = 152,
    DiscChipsGolem = 153,
    DiscChipsDurahan = 154,
    DiscChipsBaku = 155,
    DiscChipsGali = 156,
    DiscChipsZilla = 157,
    DiscChipsBajarl = 158,
    DiscChipsPhoenix = 159,
    DiscChipsGhost = 160,
    DiscChipsMetalner = 161,
    DiscChipsJill = 162,
    DiscChipsJoker = 163,
    DiscChipsUndine = 164,
    DiscChipsMock = 165,
    DiscChipsDucken = 166,
    DiscChipsWorm = 167,
    GaliMask = 168,
    Crystal = 169,
    UndineSlate = 170,
    Money = 172,
    StickGreen = 173,
    CupJellyD = 174,
    Spear = 175,
    WrackyDoll = 176,
    QuackDoll3 = 177,
    NoneEmpty = 178,
    NoneInvalid = 255
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
    XX = 38, // Unknown 1
    XY = 39, // Unknown 2
    XZ = 40, // Unknown 3
    YX = 41, // Unknown 4
    YY = 42, // Unknown 5
    YZ = 43, // Unknown 6,
    Garbage = 0xff
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

public enum ErrantryLocation : byte {
    TorbleSea = 0,
    PapasMountain = 1,
    MandyDesert = 2,
    ParepareJungle = 3,
    KawreaVolcano = 4,
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

public record struct Range<T>(T Min, T Max)
{
    public static implicit operator ValueTuple<T, T>(Range<T> record)
    {
        return (record.Min, record.Max);
    }

    public static implicit operator Range<T>(ValueTuple<T, T> record)
    {
        return new Range<T> { Min = record.Item1, Max = record.Item2 };
    }
}

/// <summary>
///     Container for information used to load monster data from disk.
///     Some of the files in data.bin reference the Name while others use
///     the ShortName field.
/// </summary>
public record GenusInfo
{
    public MonsterGenus Id { get; init; } = 0;
    public string Name { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
}

/// <summary>
///     Represents an actual monster breed (combination of two Genus) that is used in game.
/// </summary>
public record MonsterBreed
{
    public static List<MonsterBreed> AllBreeds = [];
    public MonsterGenus GenusMain { get; init; } = 0;
    public MonsterGenus GenusSub { get; init; } = 0;

    public string Name { get; init; } = string.Empty;
    public string BreedIdentifier { get; init; } = string.Empty;

    public string[] SDATAValues { get; init; } = [];

    public ushort Lifespan { get; init; } = 300;
    public sbyte NatureBase { get; init; } = 0;
    public LifeType LifeType { get; init; } = LifeType.Normal;
    public ushort Life { get; init; } = 100;
    public ushort Power { get; init; } = 100;
    public ushort Intelligence { get; init; } = 100;
    public ushort Skill { get; init; } = 100;
    public ushort Speed { get; init; } = 100;
    public ushort Defense { get; init; } = 100;
    public byte GrowthRateLife { get; init; } = 2;
    public byte GrowthRatePower { get; init; } = 2;
    public byte GrowthRateIntelligence { get; init; } = 2;
    public byte GrowthRateSkill { get; init; } = 2;
    public byte GrowthRateSpeed { get; init; } = 2;
    public byte GrowthRateDefense { get; init; } = 2;
    public byte ArenaSpeed { get; init; } = 2;
    public byte GutsRate { get; init; } = 10;
    public ushort BattleSpecialsRaw { get; init; } = 3;
    public byte[] TechniquesRaw { get; set; } 
    public ushort TrainBoost { get; init; } = 0; // Slots 23 and 24 are unknown.
    public List<IMonsterTechnique> TechList { get; init; } = [];
    public List<IMonsterTechnique> TechsKnown { get; init; } = [];

    public static MonsterBreed NewBreed( MonsterGenus main, MonsterGenus sub, string name, string breedidentifier,
        List<IMonsterTechnique> techlist, string[] SDATAvalues ) {
        MonsterBreed newBreed = new MonsterBreed {
            GenusMain = main,
            GenusSub = sub,
            Name = name,
            BreedIdentifier = breedidentifier,
            TechList = techlist,
            SDATAValues = SDATAvalues,
            Lifespan = UInt16.Parse( SDATAvalues[ 4 ] ),
            NatureBase = SByte.Parse( SDATAvalues[ 5 ] ),
            LifeType = (LifeType) Byte.Parse( SDATAvalues[ 6 ] ),
            Life = UInt16.Parse( SDATAvalues[ 7 ] ),
            Power = UInt16.Parse( SDATAvalues[ 8 ] ),
            Intelligence = UInt16.Parse( SDATAvalues[ 9 ] ),
            Skill = UInt16.Parse( SDATAvalues[ 10 ] ),
            Speed = UInt16.Parse( SDATAvalues[ 11 ] ),
            Defense = UInt16.Parse( SDATAvalues[ 12 ] ),
            GrowthRateLife = Byte.Parse( SDATAvalues[ 13 ] ),
            GrowthRatePower = Byte.Parse( SDATAvalues[ 14 ] ),
            GrowthRateIntelligence = Byte.Parse( SDATAvalues[ 15 ] ),
            GrowthRateSkill = Byte.Parse( SDATAvalues[ 16 ] ),
            GrowthRateSpeed = Byte.Parse( SDATAvalues[ 17 ] ),
            GrowthRateDefense = Byte.Parse( SDATAvalues[ 18 ] ),
            ArenaSpeed = Byte.Parse( SDATAvalues[ 19 ] ),
            GutsRate = Byte.Parse( SDATAvalues[ 20 ] ),
            BattleSpecialsRaw = UInt16.Parse( SDATAvalues[ 21 ] ),
            TechniquesRaw = new byte[48],
            TrainBoost = UInt16.Parse( SDATAvalues[ 25 ] ), // Slots 23 and 24 are unknown.
        };

        var techArray = SDATAvalues[22].ToCharArray();

        for ( var i = techArray.Length - 1; i >= 0; i-- ) {
            if ( techArray[ i ] == '1' ) {
                foreach ( var technique in techlist ) {
                    if ( technique.Id == ( techArray.Length - ( i + 1 )  ) ) {
                        newBreed.TechsKnown.Add( technique );
                        newBreed.TechniquesRaw[ technique.Id * 2 ] = 1;
                    }
                }
            }
        }
        return newBreed;
    }
 
    public static MonsterBreed? GetBreed(MonsterGenus main, MonsterGenus sub)
    {
        Logger.Trace($"Get Breed: {main}, sub: {sub}");
        return AllBreeds.Find(m => m.GenusMain == main && m.GenusSub == sub);
    }
}