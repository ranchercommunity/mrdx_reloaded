using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public enum EMonsterRanks
{
    L,
    M,
    S,
    A,
    B,
    C,
    D,
    E
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ETournamentPools
{
    L,
    M,
    S,
    A,
    B,
    C,
    D,
    E,
    A_Phoenix,
    A_DEdge,
    B_Dragon,
    F_Hero,
    F_Heel,
    F_Elder,
    S_FIMBA,
    A_FIMBA,
    B_FIMBA,
    C_FIMBA,
    D_FIMBA,
    S_FIMBA2,
    A_FIMBA2,
    B_FIMBA2,
    C_FIMBA2,
    D_FIMBA2,
    X_MOO
}

public class TournamentData
{
    public static readonly string[] RandomNameList =
    [
        "Cimasio", "Kyrades", "Ambroros", "Teodeus", "Lazan", "Pegetus", "Perseos", "Asandrou", "Agametrios",
        "Lazion", "Morphosyne", "Gelantinos", "Narkelous", "Taloclus", "Baltsalus", "Hypnaeon", "Atrol", "Alexede",
        "Baccinos", "Idastos", "Ophyroe", "Larissa", "Asperata",
        "Alnifolia", "Dentala", "Celsa", "Hempera", "Laurel", "Haldiphe", "Saffronea", "Quinn",
        "Poplarbush", "Snowdrop", "Funnyfluff", "Firo", "Limespice", "Herb", "Twinklespa", "Spring", "Shinyglade",
        "Almond", "Foggytree", "Pecan", "Jesterfeet", "Skylark", "Rainbow", "Snow", "Oakswamp", "Liri", "Briarpuff",
        "Extos", "Grimes", "Talis", "Anemia", "Tinder", "Neige", "Lec", "Chinook", "Graund", "Greidax", "Pigatt",
        "Kuanezz", "Nidar", "Danuzz", "Razodrug", "Krorodurr", "Galae",
        "Tepua", "Uvle", "Ujay", "Surlul", "Razsa", "Dezunu", "Urabu", "Sholgokoh", "Abedin", "Yetsi", "Jaedrey",
        "Leadeth", "Baudr", "Araldyng", "Gilparymr", "Sawah", "Mazraeh",
        "Aayuh", "Grimstriker", "Twistmight", "Pyregaze", "Heatmarch", "Omega", "Dalton", "Alpha", "Beta", "Gamma",
        "Zeta", "Phi", "Jaeger", "Dimbranch", "Raindust", "Hillbrace", "Storm", "Sohish", "Sunhol", "Ehtae", "Lilsof",
        "Ghostsign", "Snowlock", "Mystic", "Nevi", "Azahz", "Owen", "Denzel", "Robinson",
        "Blossom", "Yarn", "Skitter", "Mercy", "Firj", "Blubber", "Dribble", "Angel", "Tank", "Dottie", "Taugh",
        "Liberi", "Thespia", "Pirene", "Isonei", "Harrow", "Bakano", "Polo", "Okal", "Ochen", "Mhina", "Siaka", "Tamba",
        "Savane", "Boukary", "Traore", "Yaya", "Dia", "Dio", "Aaron", "Prizo", "Dimitri", "Dashaco",
        "Mathis", "Calamity", "Buve", "Hosho", "Zimba", "Tsun", "Mawere", "Rufaro", "Emoger", "Fida", "Thorns",
        "Saffron", "Teddago", "Skelyte", "Chilleni", "Slowhawk", "Jagola",
        "Zhar", "Dim", "Cleoz", "Rav", "Membut", "Dazam", "Groznur", "Aqrat", "Azzac", "Tergu", "Kirchon", "Nilla",
        "Ricryll", "Imnor", "Lanceruil", "Ballion",
        "Quosa", "Yesnorin", "Caina", "Holyn", "Athena", "Nandra", "Beratha", "Libgalyn", "Galin", "Heleto", "Faerona",
        "Fairest", "Chuckles", "Darkness",
        "Shiner", "Monkeytime", "Noper", "Willow", "Grassfall", "Misty", "Mantle", "Painscribe", "Plainwood", "Orbgold",
        "Ragespear", "Dawnward", "Clair", "Caffal", "Ronch", "Pola", "Moux", "Ranteau", "Nothier", "Peseul", "Astellon",
        "Glide", "Roughkiss", "Mosswisp", "Shadow", "Autumn",
        "Voidbane", "Voidsicle", "Kingsmith", "Kingly", "Peasant", "Pleasant", "Swellow", "Alexa", "Luitgard", "Ede",
        "Medou", "Branka", "Devotee", "Aura",
        "Outlaw", "Jade", "Nocturne", "Jarvis", "Beeps", "Faint", "Perkless", "Yill", "Quona", "Washerguard", "Eda",
        "Rosery", "Tapper", "Undergrow", "Ova",
        "Adamant", "Silverlock", "Dobby", "Finx", "Gar", "Hope", "Jewel", "Kattery", "Languish", "Zephyr", "Xilla",
        "Cedar", "Villa", "Branx", "Naught", "Midas",
        "Atronaph", "Argus", "Aideen", "Alias", "Adonay", "Anno", "Apollo", "Aydin", "Asakoa", "Aviri", "Adelynn",
        "Arsonwheel", "Angerstomp",
        "Bajor", "Beekler", "Bobbles", "Buu", "Brainstorm", "Bracer", "Basselt", "Boggycreek", "Boggart", "Bahamut",
        "Baretree", "Birchbellow",
        "Calaphyx", "Cawcaws", "Cix", "Cerrusio", "Creator", "Clipse", "Conjus", "Chanceux", "Ciorliath", "Clearwish",
        "David", "Dingus", "Dakadaka", "Dill", "Doodle", "Daydream", "Dreameater", "Diablo", "Diabolos", "Dunker",
        "Dragonfly", "Eater", "Eo", "Endofall", "Exuberance", "Etresse", "Elan", "Entun", "Earthtender",
        "Fargus", "Fillero", "Ferrus", "Faunus", "Feathers", "Fuzzball", "Foolcaller",
        "Gronkula", "Gimmles", "Golox", "Gargamel", "Gutterman", "Gale", "Gemlashes", "Gotusloop", "Goldenboy",
        "Hardness", "Herman", "Hillox", "Hundredyear", "Hurlante", "Hazel", "Hanzel", "Hatemonger",
        "Io", "Iodine", "Iaz", "Illomens", "Incarnate", "Isaias", "Islecrusher", "Iaull", "Itong", "Iilos",
        "Jax", "Jack", "Jillian", "Jellyjam", "Julius", "Jasmine", "Jazlynn",
        "Kawkaws", "Ki", "Kallus", "Keith", "Kevin", "Kitten", "Kedijah", "Keah", "Kammi",
        "Larrius", "Lengeru", "Ludwiz", "Longboy", "Llij", "Liyong", "Laylah",
        "Moparscape", "Mardok", "Mueller", "Mastodon", "Morphius", "Murph", "McNasty", "Mehret", "Mordheim", "Mors",
        "Magician", "Morefather",
        "Nilus", "Neo", "Nevarine", "Nix", "Nemo", "Nangara", "Neta", "Nontoun",
        "Ox", "Otherwilds", "Outerspace", "Oiler", "Officer", "Omexx", "Ocus", "Oddball",
        "Parrix", "Pickle", "Piledriver", "Puffpuff", "Peachclaw", "Peargrinder", "Paraema",
        "Quizler", "Qix", "Quark", "Qidus", "Quid",
        "Roachest", "Rizz", "Rue", "Ramman", "Rammuh", "Ragdoll", "Rose",
        "Savi", "Serenity", "Silverhand", "Sammy", "Soothsayer", "Swissmiss", "Sully", "Semere", "Spiris", "Splinter",
        "Sandytwist", "Seatsaidh",
        "Tav", "Traveller", "Taximon", "Truegold", "Tuskies", "Tinkertot", "Terminus", "Tamil",
        "Underway", "Uco", "Unibrow",
        "Valiant", "Violence", "Vorton", "Voodoo", "Veuve", "Vexee", "Volance",
        "Wabberjack", "Warbler", "Werebaby", "Wodyeith",
        "Xilla", "Xerces",
        "Yoyo", "Yanger", "Yucca", "Yew", "Yewflower",
        "Zoro", "Zewdi", "Zaben", "Zookeeper"
    ];

    public static readonly Random GrowthRNG = new(Random.Shared.Next());
    public static readonly Random LifespanRNG = new(Random.Shared.Next());

    private readonly Config _config;
    private readonly string _gamePath;

    private readonly Dictionary<ETournamentPools, TournamentPool> _tournamentPools = [];
    public readonly List<TournamentMonster> Monsters = [];

    private uint _currentWeek;
    private bool _firstweek;

    private bool _initialized;
    private List<MonsterGenus> _unlockedTournamentBreeds = [];

    public TournamentData(string gamePath, Config config)
    {
        _config = config;
        _gamePath = gamePath;
        foreach (var pool in Enum.GetValues<ETournamentPools>())
            _tournamentPools.Add(pool, new TournamentPool(this, config, pool));

        SetupTournamentParticipantsFromTaikai();
    }

    private void AddExistingMonster(IBattleMonsterData m, int id)
    {
        var pool = TournamentPool.PoolFromId(id);
        var abdm = new TournamentMonster(_config, m)
        {
            Rank = _tournamentPools[pool].Info.Rank
        };

        Monsters.Add(abdm);
        // _tournamentPools[pool].MonsterAdd(abdm);
    }

    private void AddExistingMonster(TournamentMonster abdm)
    {
        Monsters.Add(abdm);
        // foreach (var pool in tournamentPools.Values)
        // foreach (var monpool in abdm.pools)
        //     if ((ETournamentPools)abdm._rawpools[i] == pool.Pool)
        //         pool.MonsterAdd(abdm);
    }

    public void AdvanceWeek(uint currentWeek, List<MonsterGenus> unlockedmonsters)
    {
        _unlockedTournamentBreeds = unlockedmonsters;

        if (!_initialized)
        {
            _currentWeek = currentWeek;
            return;
        }

        if (_firstweek && currentWeek != 0)
        {
            _currentWeek = currentWeek - 1;
        }
        else if (currentWeek > int.MaxValue - 4)
        {
            _currentWeek = 0;
            currentWeek = 0;
        }

        Logger.Debug("Advancing Weeks in TD: " + _currentWeek + " trying to get to " + currentWeek);
        while (_currentWeek < currentWeek)
        {
            _currentWeek++;

            if (_currentWeek % 4 == 0) AdvanceMonth();

            if (_currentWeek % 12 == 0) AdvanceTournamentPromotions();
        }

        Logger.Debug("Finished Advancing Weeks, Checking Pools", Color.Yellow);
        foreach (var pool in _tournamentPools.Values)
            while (Monsters.Count(m => m.Pools.Contains(pool)) < pool.Info.Size)
                Monsters.Add(pool.GenerateNewValidMonster(_unlockedTournamentBreeds));
        // Shuffle Monsters - TODO: This should happen weekly.
        // var ml = pool.Monsters.ToArray();
        // Random.Shared.Shuffle(ml);
        _firstweek = false;
    }

    public void LoadSavedTournamentData(List<byte[]> monstersRaw)
    {
        var monsters = new List<TournamentMonster>();
        foreach (var raw in monstersRaw) monsters.Add(new TournamentMonster(_tournamentPools, raw));
        // if (!_saveFileManager.SaveDataGameLoaded) return;
        // Logger.Info("Game Load Detected", Color.Orange);
        // var monsters = _saveFileManager.LoadTournamentData();
        if (monsters.Count == 0)
        {
            Logger.Trace("No custom tournament data found. Loading taikai_en.", Color.Orange);
            SetupTournamentParticipantsFromTaikai();
        }
        else
        {
            Logger.Trace("Found Data for " + monsters.Count + " monsters.", Color.Orange);
            ClearAllData();
            foreach (var abdm in monsters)
                AddExistingMonster(abdm);
        }

        _initialized = true;
        _firstweek = true;
        Logger.Info("Initialization Complete", Color.Orange);
    }

    /// <summary>
    ///     Loads the taikai_en.flk file and generates the TournamentData from it. Is loaded at startup and when a new save
    ///     without save data is loaded.
    /// </summary>
    private void SetupTournamentParticipantsFromTaikai()
    {
        ClearAllData();

        var tournamentMonsterFile = _gamePath + @"\mf2\data\taikai\taikai_en.flk";
        var rawmonster = new byte[60];

        using var fs = new FileStream(tournamentMonsterFile, FileMode.Open);
        fs.Position = 0xA8C + 60; // This relies upon nothing earlier in the file being appended. 
        for (var i = 1; i < 120; i++)
        {
            // 0 = Dummy Monster so skip. 119 in the standard file.
            fs.ReadExactly(rawmonster, 0, 60);
            TournamentMonster tm = new(_tournamentPools, rawmonster);
            AddExistingMonster(tm, i);

            // var bytes = "";
            // for (var z = 0; z < 60; z++) bytes += rawmonster[z] + ",";
            Logger.Trace("Monster " + i + " Parsed: " + tm, Color.Lime);
        }

        _initialized = true;
    }

    private void AdvanceMonth()
    {
        Logger.Debug("Advancing month from TournamentData", Color.Blue);
        for (var i = Monsters.Count - 1; i >= 0; i--)
        {
            var m = Monsters[i];

            m.AdvanceMonth();
            if (!m.Alive)
            {
                Logger.Info(m.Name + " has died. Rest in peace.", Color.Blue);
                // for (var j = m.pools.Count() - 1; j >= 0; j--) m.pools[j].MonsterRemove(m);
                Monsters.Remove(m);
            }

            // TODO CONFIG TECHNIQUE RATE
            if (Random.Shared.Next(25) == 0) m.LearnTechnique();
        }
    }

    private void AdvanceTournamentPromotions()
    {
        _tournamentPools[ETournamentPools.M].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.L]);
        _tournamentPools[ETournamentPools.S].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.M]);
        _tournamentPools[ETournamentPools.A].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.S]);
        _tournamentPools[ETournamentPools.B].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.A]);
        _tournamentPools[ETournamentPools.C].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.B]);
        _tournamentPools[ETournamentPools.D].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.C]);
        _tournamentPools[ETournamentPools.E].MonstersPromoteToNewPool(_tournamentPools[ETournamentPools.D]);
    }

    public List<TournamentMonster> GetTournamentMembers(int start, int end)
    {
        var participants = new List<TournamentMonster>();

        foreach (var pool in _tournamentPools.Values)
        {
            if (pool.Pool == ETournamentPools.X_MOO)
                continue;
            var poolMonsters = Monsters.FindAll(m => m.Pools.Contains(pool));
            participants.AddRange(poolMonsters[..pool.Info.Size]);
        }

        // Edge Case to Handle the ONE SLOT that is skipped For Moo.
        participants.Add(Monsters.First(m => m.Pools.Exists(p => p.Pool == ETournamentPools.S)));
        return participants;
    }

    public void ClearAllData()
    {
        _initialized = false;

        Monsters.Clear();
        // foreach (var pool in _tournamentPools.Values) pool.Monsters.Clear();
    }
}