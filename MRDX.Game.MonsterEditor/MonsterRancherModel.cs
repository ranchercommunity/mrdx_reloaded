using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Mod.Interfaces;

namespace MRDX.Game.MonsterEditor;

public class MonsterRancherModel
{
    private readonly WeakReference<IGame> _game;
    private readonly ILogger _logger;
    private WeakReference<IGameClient> _client;

    public MonsterModel Monster;

    public MonsterRancherModel(IModLoader modLoader)
    {
        _game = modLoader.GetController<IGame>();
        _client = modLoader.GetController<IGameClient>();
        _logger = (ILogger)modLoader.GetLogger();
        _game.TryGetTarget(out var g);
        if (g == null)
        {
            _logger.WriteLine("[TODO MonsterEditor] Could not get game target.", Color.Red);
            return;
        }

        g.OnMonsterChanged += MonsterChanged;
        Monster = new MonsterModel(g.Monster);
    }

    private void MonsterChanged(IMonsterChange mon)
    {
        Monster = new MonsterModel(mon.Current);
    }
}

public class MonsterModel : IMonster
{
    private readonly IMonster _mon;
    private ushort _age;
    private byte _arenaSpeed;
    private byte _cupJellyCount;
    private ushort _defense;
    private byte _drug;
    private byte _drugDuration;
    private byte _fame;
    private byte _fatigue;
    private sbyte _formRaw;
    private MonsterGenus _genusMain;
    private MonsterGenus _genusSub;
    private byte _growthRateDefense;
    private byte _growthRateIntelligence;
    private byte _growthRateLife;
    private byte _growthRatePower;
    private byte _growthRateSkill;
    private byte _growthRateSpeed;
    private byte _gutsRate;
    private ushort _initalLifespan;
    private ushort _intelligence;
    private Item _itemDislike;
    private Item _itemLike;
    private bool _itemUsed;
    private ushort _life;
    private ushort _lifespan;
    private LifeStage _lifeStage;
    private LifeType _lifeType;
    private byte _loyalFear;
    private byte _loyalSpoil;
    private byte _motivationDodge;
    private byte _motivationDomino;
    private byte _motivationEndure;
    private byte _motivationLeap;
    private byte _motivationMeditate;
    private byte _motivationPull;
    private byte _motivationRun;
    private byte _motivationShoot;
    private byte _motivationStudy;
    private byte _motivationSwim;
    private string _name = "";
    private sbyte _natureBase;
    private short _natureRaw;
    private PlaytimeType _playtime;
    private ushort _power;
    private byte _rank;
    private ushort _skill;
    private ushort _speed;
    private sbyte _stress;
    private ushort _trainBoost;
    private bool _usedPeachGold;
    private bool _usedPeachSilver;

    public MonsterModel(IMonster mon)
    {
        _mon = mon;
    }

    public ushort Age
    {
        get => _age;
        set => _mon.Age = value;
    }

    public MonsterGenus GenusMain
    {
        get => _genusMain;
        set => _mon.GenusMain = value;
    }

    public MonsterGenus GenusSub
    {
        get => _genusSub;
        set => _mon.GenusSub = value;
    }

    public ushort Life
    {
        get => _life;
        set => _mon.Life = value;
    }

    public ushort Power
    {
        get => _power;
        set => _mon.Power = value;
    }

    public ushort Intelligence
    {
        get => _intelligence;
        set => _mon.Intelligence = value;
    }

    public ushort Skill
    {
        get => _skill;
        set => _mon.Skill = value;
    }

    public ushort Speed
    {
        get => _speed;
        set => _mon.Speed = value;
    }

    public ushort Defense
    {
        get => _defense;
        set => _mon.Defense = value;
    }

    public ushort Lifespan
    {
        get => _lifespan;
        set => _mon.Lifespan = value;
    }

    public ushort InitalLifespan
    {
        get => _initalLifespan;
        set => _mon.InitalLifespan = value;
    }

    public short NatureRaw
    {
        get => _natureRaw;
        set => _mon.NatureRaw = value;
    }

    public sbyte NatureBase
    {
        get => _natureBase;
        set => _mon.NatureBase = value;
    }

    public byte Fatigue
    {
        get => _fatigue;
        set => _mon.Fatigue = value;
    }

    public byte Fame
    {
        get => _fame;
        set => _mon.Fame = value;
    }

    public sbyte Stress
    {
        get => _stress;
        set => _mon.Stress = value;
    }

    public byte LoyalSpoil
    {
        get => _loyalSpoil;
        set => _mon.LoyalSpoil = value;
    }

    public byte LoyalFear
    {
        get => _loyalFear;
        set => _mon.LoyalFear = value;
    }

    public sbyte FormRaw
    {
        get => _formRaw;
        set => _mon.FormRaw = value;
    }

    public byte GrowthRateLife
    {
        get => _growthRateLife;
        set => _mon.GrowthRateLife = value;
    }

    public byte GrowthRatePower
    {
        get => _growthRatePower;
        set => _mon.GrowthRatePower = value;
    }

    public byte GrowthRateIntelligence
    {
        get => _growthRateIntelligence;
        set => _mon.GrowthRateIntelligence = value;
    }

    public byte GrowthRateSkill
    {
        get => _growthRateSkill;
        set => _mon.GrowthRateSkill = value;
    }

    public byte GrowthRateSpeed
    {
        get => _growthRateSpeed;
        set => _mon.GrowthRateSpeed = value;
    }

    public byte GrowthRateDefense
    {
        get => _growthRateDefense;
        set => _mon.GrowthRateDefense = value;
    }

    public ushort TrainBoost
    {
        get => _trainBoost;
        set => _mon.TrainBoost = value;
    }

    public byte CupJellyCount
    {
        get => _cupJellyCount;
        set => _mon.CupJellyCount = value;
    }

    public bool UsedPeachGold
    {
        get => _usedPeachGold;
        set => _mon.UsedPeachGold = value;
    }

    public bool UsedPeachSilver
    {
        get => _usedPeachSilver;
        set => _mon.UsedPeachSilver = value;
    }

    public PlaytimeType Playtime
    {
        get => _playtime;
        set => _mon.Playtime = value;
    }

    public byte Drug
    {
        get => _drug;
        set => _mon.Drug = value;
    }

    public byte DrugDuration
    {
        get => _drugDuration;
        set => _mon.DrugDuration = value;
    }

    public bool ItemUsed
    {
        get => _itemUsed;
        set => _mon.ItemUsed = value;
    }

    public Item ItemLike
    {
        get => _itemLike;
        set => _mon.ItemLike = value;
    }

    public Item ItemDislike
    {
        get => _itemDislike;
        set => _mon.ItemDislike = value;
    }

    public byte Rank
    {
        get => _rank;
        set => _mon.Rank = value;
    }

    public LifeStage LifeStage
    {
        get => _lifeStage;
        set => _mon.LifeStage = value;
    }

    public LifeType LifeType
    {
        get => _lifeType;
        set => _mon.LifeType = value;
    }

    public byte ArenaSpeed
    {
        get => _arenaSpeed;
        set => _mon.ArenaSpeed = value;
    }

    public byte GutsRate
    {
        get => _gutsRate;
        set => _mon.GutsRate = value;
    }

    public byte MotivationDomino
    {
        get => _motivationDomino;
        set => _mon.MotivationDomino = value;
    }

    public byte MotivationStudy
    {
        get => _motivationStudy;
        set => _mon.MotivationStudy = value;
    }

    public byte MotivationRun
    {
        get => _motivationRun;
        set => _mon.MotivationRun = value;
    }

    public byte MotivationShoot
    {
        get => _motivationShoot;
        set => _mon.MotivationShoot = value;
    }

    public byte MotivationDodge
    {
        get => _motivationDodge;
        set => _mon.MotivationDodge = value;
    }

    public byte MotivationEndure
    {
        get => _motivationEndure;
        set => _mon.MotivationEndure = value;
    }

    public byte MotivationPull
    {
        get => _motivationPull;
        set => _mon.MotivationPull = value;
    }

    public byte MotivationMeditate
    {
        get => _motivationMeditate;
        set => _mon.MotivationMeditate = value;
    }

    public byte MotivationLeap
    {
        get => _motivationLeap;
        set => _mon.MotivationLeap = value;
    }

    public byte MotivationSwim
    {
        get => _motivationSwim;
        set => _mon.MotivationSwim = value;
    }

    public IList<IMonsterTechnique> Moves { get; } = new List<IMonsterTechnique>();
    public IList<byte> MoveUseCount { get; } = new List<byte>();

    public string Name
    {
        get => _name;
        set => _mon.Name = value;
    }

    public uint PrizeMoney { get; set; }

    public void Set(IMonster mon)
    {
        _age = mon.Age;
        _genusMain = mon.GenusMain;
        _genusSub = mon.GenusSub;
        _life = mon.Life;
        _power = mon.Power;
        _intelligence = mon.Intelligence;
        _skill = mon.Skill;
        _speed = mon.Speed;
        _defense = mon.Defense;
        _lifespan = mon.Lifespan;
        _initalLifespan = mon.InitalLifespan;
        _natureRaw = mon.NatureRaw;
        _natureBase = mon.NatureBase;
        _fatigue = mon.Fatigue;
        _fame = mon.Fame;
        _stress = mon.Stress;
        _loyalSpoil = mon.LoyalSpoil;
        _loyalFear = mon.LoyalFear;
        _formRaw = mon.FormRaw;
        _growthRateLife = mon.GrowthRateLife;
        _growthRatePower = mon.GrowthRatePower;
        _growthRateIntelligence = mon.GrowthRateIntelligence;
        _growthRateSkill = mon.GrowthRateSkill;
        _growthRateSpeed = mon.GrowthRateSpeed;
        _growthRateDefense = mon.GrowthRateDefense;
        _trainBoost = mon.TrainBoost;
        _cupJellyCount = mon.CupJellyCount;
        _usedPeachGold = mon.UsedPeachGold;
        _usedPeachSilver = mon.UsedPeachSilver;
        _playtime = mon.Playtime;
        _drug = mon.Drug;
        _drugDuration = mon.DrugDuration;
        _itemUsed = mon.ItemUsed;
        _itemLike = mon.ItemLike;
        _itemDislike = mon.ItemDislike;
        _rank = mon.Rank;
        _lifeStage = mon.LifeStage;
        _lifeType = mon.LifeType;
        _arenaSpeed = mon.ArenaSpeed;
        _gutsRate = mon.GutsRate;
        //Moves. = mon.Moves;
        //MoveUseCount = mon.MoveUseCount;
        _motivationDomino = mon.MotivationDomino;
        _motivationStudy = mon.MotivationStudy;
        _motivationRun = mon.MotivationRun;
        _motivationShoot = mon.MotivationShoot;
        _motivationDodge = mon.MotivationDodge;
        _motivationEndure = mon.MotivationEndure;
        _motivationPull = mon.MotivationPull;
        _motivationMeditate = mon.MotivationMeditate;
        _motivationLeap = mon.MotivationLeap;
        _motivationSwim = mon.MotivationSwim;
        _name = mon.Name;
        PrizeMoney = mon.PrizeMoney;
    }
}